using System.IO;
using System.Text;
using SporeMaster.Core.Presentation;

namespace SporeMaster.Gui;

/// <summary>
/// A UI-less smoke test: it builds the real window (which loads the XAML) and, when a package is
/// given, exercises opening, filtering and the full preview path (including image decoding) through
/// the actual window code. The result is written to a report file and returned as an exit code.
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
                report.AppendLine($"Resources: {window.ResourceCount}, rows: {window.RowCount}");

                // Force a real layout pass so the GridView actually creates its cells (this is
                // where WPF resolves binding cultures and where config problems surface).
                window.Show();
                window.UpdateLayout();
                report.AppendLine("Layout pass: OK");
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
