using System.IO;
using System.Text;
using SporeMaster.Core.Presentation;

namespace SporeMaster.Gui;

/// <summary>
/// A UI-less smoke test: it builds the real window (which loads the XAML) and, when a package is
/// given, exercises opening, the folder tree, navigation, filtering, sorting and the full preview
/// path (including image decoding) through the actual window code. The result is written to a
/// report file and returned as an exit code.
/// </summary>
internal static class SelfTest
{
    public static string ReportPath =>
        Path.Combine(Path.GetTempPath(), "sporemaster-gui-selftest.txt");

    public static int Run(string? packagePath)
    {
        var report = new StringBuilder();
        int exitCode = 0;

        try
        {
            // Constructing the window loads and validates the XAML (a common source of runtime errors).
            var window = new MainWindow();
            report.AppendLine("MainWindow constructed: OK");

            if (!string.IsNullOrEmpty(packagePath) && File.Exists(packagePath))
            {
                window.OpenPackageForTest(packagePath);
                report.AppendLine($"Opened: {packagePath}");
                report.AppendLine($"Resources: {window.ResourceCount}, folders: {window.FolderCount}");

                // Force a real layout pass so the TreeView/GridView actually create their items
                // (this is where WPF resolves binding cultures and where config problems surface).
                window.Show();
                window.UpdateLayout();
                report.AppendLine("Layout pass: OK");

                // Exercise folder navigation.
                int foldersToVisit = Math.Min(window.FolderCount, 20);
                for (int i = 0; i < foldersToVisit; i++)
                {
                    window.SelectFolderForTest(i);
                }

                report.AppendLine($"Folder navigation: {foldersToVisit} folder(s) selected");

                // Checked folders (multi-select) with standard tri-state root behaviour.
                if (window.FolderCount > 0)
                {
                    window.CheckFolderForTest(0, true);
                    int checkedResources = window.CheckedResourceCountForTest();
                    bool? partialRoot = window.RootCheckStateForTest();

                    window.SetAllFoldersCheckedForTest(true);
                    bool? allRoot = window.RootCheckStateForTest();
                    int allFolders = window.CheckedFolderCountForTest();

                    window.SetAllFoldersCheckedForTest(false);
                    bool? noneRoot = window.RootCheckStateForTest();

                    report.AppendLine(
                        $"Checked folder 0: {checkedResources} resource(s), root={partialRoot}; " +
                        $"check-all: {allFolders} folder(s), root={allRoot}; uncheck-all: root={noneRoot}");
                }

                // Details pane toggle is state-driven (visible -> hidden -> visible).
                window.ToggleDetailsPaneForTest();
                bool hidden = !window.DetailsPaneVisibleForTest();
                window.ToggleDetailsPaneForTest();
                bool shown = window.DetailsPaneVisibleForTest();
                report.AppendLine($"Details pane toggle: hidden={hidden}, shown again={shown}");

                // View menu: Details and Large Icons are mutually exclusive.
                window.ApplyDetailsViewForTest();
                (bool d1, bool l1) = window.ViewMenuStateForTest();
                window.ApplyLargeIconsViewForTest();
                (bool d2, bool l2) = window.ViewMenuStateForTest();
                window.ApplyDetailsViewForTest();
                report.AppendLine($"View menu: details=({d1},{l1}) largeIcons=({d2},{l2})");

                // Address bar: lists "(all)" + folders, and navigates by name.
                report.AppendLine($"Address bar items: {window.AddressItemCountForTest()}");
                if (window.FolderCount > 0)
                {
                    window.NavigateAddressForTest("all");
                    window.NavigateAddressForTest("this-folder-does-not-exist");
                    window.NavigateAddressForTest("creature_parts");
                    report.AppendLine($"Address navigation: done ({window.VisibleRowsForTest().Count} visible)");
                    window.SelectFolderForTest(0);
                }

                // Multiple selection: aggregate details + select all.
                window.SelectFolderForTest(0);
                int selectedCount = window.SelectAllForTest();
                string aggregateDetails = window.DetailsTextForTest();
                window.ClearSelectionForTest();
                report.AppendLine($"Select all: {selectedCount} item(s); aggregate details has 'selected': {aggregateDetails.Contains("selected", StringComparison.OrdinalIgnoreCase)}");

                // Package history: opening a nested package and going back.
                string originalPath = window.CurrentPackagePathForTest()!;
                string copy = Path.Combine(Path.GetTempPath(), "SporeMaster-selftest-copy.package");
                File.Copy(originalPath, copy, overwrite: true);
                window.OpenPackageForTest(copy);
                int depthAfterNested = window.PackageStackDepthForTest();
                window.OpenPreviousPackageForTest();
                bool returned = string.Equals(window.CurrentPackagePathForTest(), originalPath, StringComparison.OrdinalIgnoreCase);
                try
                {
                    File.Delete(copy);
                }
                catch
                {
                    // best effort
                }

                report.AppendLine($"Package history: stack after nested={depthAfterNested}, returned to parent={returned}");

                // Exercise filtering.
                window.SelectFolderForTest(0);
                int before = window.VisibleRowsForTest().Count;
                window.ApplyFilterForTest("prop");
                report.AppendLine($"Filter 'prop': {before} -> {window.VisibleRowsForTest().Count} visible");
                window.ApplyFilterForTest(string.Empty);

                window.UpdateLayout();
                window.Close();

                IReadOnlyList<ResourceItem> rows = window.RowsForTest();
                int previewed = 0;
                var kinds = new SortedSet<string>(StringComparer.Ordinal);
                var failures = new List<string>();

                // Exercise every row's full preview path (the same code the UI runs on selection,
                // including WPF image decoding) to surface anything that would crash the window.
                foreach (ResourceItem row in rows)
                {
                    ResourcePreview preview = window.PreviewForTest(row);
                    kinds.Add(preview.Kind);

                    string? selectionError = window.SelectionForTest(row);
                    if (selectionError is not null)
                    {
                        failures.Add($"{row.Path}: {selectionError}");
                    }

                    previewed++;
                }

                report.AppendLine($"Previewed: {previewed}");
                report.AppendLine($"Preview kinds seen: {string.Join(", ", kinds)}");
                report.AppendLine($"Selection failures: {failures.Count}");
                foreach (string failure in failures.Take(10))
                {
                    report.AppendLine($"  - {failure}");
                }

                // Explorer-style details pane + per-type glyphs.
                int missingGlyphs = rows.Count(r => string.IsNullOrEmpty(r.Glyph));
                report.AppendLine($"Rows missing a glyph: {missingGlyphs}");

                // Drag-out export writes a temporary converted file.
                if (rows.Count > 0)
                {
                    string temp = window.ExtractToTempForTest(rows[0]);
                    report.AppendLine($"Drag-out temp file: {File.Exists(temp)} ({Path.GetFileName(temp)})");
                }

                int detailsFailures = 0;
                foreach (ResourceItem row in rows.Take(200))
                {
                    try
                    {
                        string details = window.DetailsForTest(row);
                        if (string.IsNullOrWhiteSpace(details) || !details.Contains(row.Key, StringComparison.Ordinal))
                        {
                            detailsFailures++;
                        }
                    }
                    catch
                    {
                        detailsFailures++;
                    }
                }

                report.AppendLine($"Details pane failures: {detailsFailures}");
            }
            else
            {
                report.AppendLine("No package supplied; UI construction only.");
            }

            report.AppendLine("RESULT: OK");
        }
        catch (Exception ex)
        {
            report.AppendLine("RESULT: FAIL");
            report.AppendLine(ex.ToString());
            exitCode = 1;
        }

        try
        {
            File.WriteAllText(ReportPath, report.ToString());
        }
        catch
        {
            // best effort
        }

        return exitCode;
    }
}
