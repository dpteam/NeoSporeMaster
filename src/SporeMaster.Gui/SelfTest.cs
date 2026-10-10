using System.IO;
using System.Text;
using SporeMaster.Core.Presentation;

namespace SporeMaster.Gui;

/// <summary>
/// A UI-less smoke test: it builds the real window (which loads the XAML) and, when a package is
/// given, exercises opening, filtering and previewing through the actual window code paths.
/// The result is written to a report file and returned as a process exit code.
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

                IReadOnlyList<ResourceItem> rows = window.RowsForTest();
                int previewed = 0;
                var kinds = new SortedSet<string>(StringComparer.Ordinal);
                string? firstError = null;

                foreach (ResourceItem row in rows.Take(200))
                {
                    ResourcePreview preview = window.PreviewForTest(row);
                    kinds.Add(preview.Kind);
                    if (preview.Kind == "error" && firstError is null)
                    {
                        firstError = $"{row.Path}: {preview.Text}";
                    }

                    previewed++;
                }

                report.AppendLine($"Previewed: {previewed}");
                report.AppendLine($"Preview kinds seen: {string.Join(", ", kinds)}");
                if (firstError is not null)
                {
                    report.AppendLine($"First error: {firstError}");
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
            report.AppendLine($"RESULT: FAIL");
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
