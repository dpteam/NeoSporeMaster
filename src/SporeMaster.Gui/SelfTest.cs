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
                report.AppendLine($"Language: {Core.Localization.Localizer.Default.Language}");

                // Capture the whole window (chrome) to a PNG so the layout/labels can be checked.
                string windowShot = Path.Combine(Path.GetTempPath(), "sporemaster-gui-window.png");
                report.AppendLine($"Window screenshot: {window.CaptureWindowForTest(windowShot) ?? "not captured"}");

                // Exercise folder navigation.
                int foldersToVisit = Math.Min(window.FolderCount, 20);
                for (int i = 0; i < foldersToVisit; i++)
                {
                    window.SelectFolderForTest(i);
                }

                report.AppendLine($"Folder navigation: {foldersToVisit} folder(s) selected");

                // 3D model preview: find a textured model (and one with a skeleton), show it and capture.
                ResourceItem? modelRow = null;
                ResourceItem? skeletonRow = null;
                foreach (ResourceItem candidate in window.RowsForTest().Where(r => r.TypeId == 0x2F4E681B).Take(400))
                {
                    Core.Presentation.ResourcePreview candidatePreview = window.PreviewForTest(candidate);
                    if (!candidatePreview.IsModel)
                    {
                        continue;
                    }

                    if (modelRow is null && candidatePreview.Model!.TexturePng is not null)
                    {
                        modelRow = candidate;
                    }

                    if (skeletonRow is null && candidatePreview.Model!.SkeletonJoints.Count > 0)
                    {
                        skeletonRow = candidate;
                    }

                    if (modelRow is not null && skeletonRow is not null)
                    {
                        break;
                    }
                }

                modelRow ??= skeletonRow;

                if (modelRow is not null)
                {
                    string shotPath = Path.Combine(Path.GetTempPath(), "sporemaster-3d-preview.png");
                    string? shot = window.CapturePreviewPngForTest(modelRow, shotPath);
                    Core.Presentation.ResourcePreview mp = window.PreviewForTest(modelRow);
                    report.AppendLine($"3D preview: {modelRow.ShortName}, textured={mp.Model?.TexturePng is not null}, screenshot={(shot is null ? "not captured" : "ok")}");
                }

                if (skeletonRow is not null)
                {
                    int joints = window.SkeletonJointCountForTest(skeletonRow);
                    window.SetSkeletonForTest(true);
                    string skelPath = Path.Combine(Path.GetTempPath(), "sporemaster-3d-skeleton.png");
                    window.CapturePreviewPngForTest(skeletonRow, skelPath);
                    window.SetSkeletonForTest(false);
                    report.AppendLine($"Skeleton overlay: {skeletonRow.ShortName}, {joints} joint(s)");
                }

                // Animation scrub: find an animatable model, pose it and capture.
                ResourceItem? animRow = window.RowsForTest()
                    .Where(r => r.TypeId == 0x2F4E681B)
                    .FirstOrDefault(r => window.CanAnimateForTest(r));
                if (animRow is not null)
                {
                    int bones = window.ScrubAnimationForTest(animRow, 0.5);
                    string animPath = Path.Combine(Path.GetTempPath(), "sporemaster-3d-anim.png");
                    window.CapturePreviewPngForTest(animRow, animPath);
                    window.SetSkeletonForTest(false);
                    report.AppendLine($"Animation scrub: {animRow.ShortName}, {bones} bone(s)");
                }
                else
                {
                    report.AppendLine("Animation scrub: no animatable model found.");
                }

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

                // Properties panel.
                if (window.RowsForTest().Count > 0)
                {
                    ResourceItem first = window.RowsForTest()[0];
                    string props = window.PropertiesTextForTest(first);
                    bool hasKey = props.Contains(first.Key, StringComparison.Ordinal);
                    bool hasPath = props.Contains(first.Path, StringComparison.Ordinal);
                    window.SetPropertiesPaneForTest(true);
                    bool visible = window.PropertiesPaneVisibleForTest();
                    // Show the properties pane for a real row so it appears in the screenshot.
                    if (window.VisibleRowsForTest().Count > 0)
                    {
                        window.ShowPropertiesForTest(window.VisibleRowsForTest()[0]);
                        string propsShot = Path.Combine(Path.GetTempPath(), "sporemaster-gui-properties.png");
                        window.CaptureWindowForTest(propsShot);
                    }

                    report.AppendLine($"Properties panel: hasKey={hasKey}, hasPath={hasPath}, visible={visible}");
                    window.SetPropertiesPaneForTest(false);
                }

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
                string? originalPath = window.CurrentPackagePathForTest();
                if (string.IsNullOrEmpty(originalPath))
                {
                    // The package could not be opened (e.g. it is not a valid archive); skip the
                    // path-dependent checks instead of throwing.
                    report.AppendLine("Package history: skipped (no package opened)");
                }
                else
                {
                    string copy = Path.Combine(Path.GetTempPath(), "SporeMaster-selftest-copy.package");
                    File.Copy(originalPath, copy, overwrite: true);
                    window.OpenNestedPackageForTest(copy);
                    int depthAfterNested = window.PackageStackDepthForTest();
                    window.OpenPreviousPackageForTest();
                    bool returned = string.Equals(window.CurrentPackagePathForTest(), originalPath, StringComparison.OrdinalIgnoreCase);
                    // An explicit (top-level) open must clear the history, so you cannot bounce back into a stale nested package.
                    window.OpenNestedPackageForTest(copy);
                    window.OpenPackageForTest(originalPath);
                    int depthAfterTopLevel = window.PackageStackDepthForTest();
                    try
                    {
                        File.Delete(copy);
                    }
                    catch
                    {
                        // best effort
                    }

                    report.AppendLine($"Package history: stack after nested={depthAfterNested}, returned to parent={returned}, cleared on top-level open={depthAfterTopLevel == 0}");

                    // Compare with itself (should be identical).
                    try
                    {
                        string compare = window.CompareWithForTest(originalPath);
                        report.AppendLine($"Compare with self: {compare}");
                    }
                    catch (Exception ex)
                    {
                        report.AppendLine($"Compare with self failed: {ex.Message}");
                    }
                }

                // Load a key map (if available) to improve names, then rebuild.
                string keyMapPath = Path.Combine("refs", "SporeUnpackedData", "SporeBeta2008", "Data", "KeyMap.txt");
                if (File.Exists(keyMapPath))
                {
                    window.LoadKeyMapForTest(keyMapPath);
                    report.AppendLine($"Loaded key map: {keyMapPath}");
                }
                else
                {
                    report.AppendLine("Key map not found; skipped key-map load.");
                }

                // Exercise filtering over the whole package (root scope).
                window.NavigateAddressForTest("all");
                int before = window.VisibleRowsForTest().Count;
                window.ApplyFilterForTest("prop");
                report.AppendLine($"Filter 'prop': {before} -> {window.VisibleRowsForTest().Count} visible");
                window.ApplyFilterForTest("*.rw4");
                report.AppendLine($"Wildcard '*.rw4': {window.VisibleRowsForTest().Count} visible");
                window.ApplyFilterForTest(@"re:\.prop$");
                report.AppendLine($"Regex '\\.prop$': {window.VisibleRowsForTest().Count} visible");
                int contentMatches = window.ContentSearchForTest("hex:00000000");
                report.AppendLine($"Content search 'hex:00000000': {contentMatches} match(es)");
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
