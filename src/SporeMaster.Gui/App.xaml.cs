using System.Windows;

namespace SporeMaster.Gui;

/// <summary>Application entry point.</summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Headless-ish self test: build the window (validates XAML), exercise the Core pipeline
        // on an optional package, then exit without showing any UI.
        if (e.Args.Contains("--selftest"))
        {
            string? package = e.Args.FirstOrDefault(a => !a.StartsWith("--"));
            int exitCode = SelfTest.Run(package);
            Shutdown(exitCode);
            return;
        }

        var window = new MainWindow();
        window.Show();
    }
}
