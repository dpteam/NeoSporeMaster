using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace SporeMaster.Gui;

/// <summary>Application entry point.</summary>
/// <remarks>
/// Installs global fault-tolerance handlers so that an unexpected exception in one action does not
/// tear down the whole application; problems are logged and (where possible) reported to the user.
/// </remarks>
public partial class App : Application
{
    private static readonly object LogLock = new();

    /// <summary>Path of the crash log written by <see cref="LogCrash"/>.</summary>
    public static string CrashLogPath { get; } =
        Path.Combine(Path.GetTempPath(), "sporemaster-gui-crash.log");

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            LogCrash("AppDomain", args.ExceptionObject as Exception ?? new Exception("unknown"));

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

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        // Keep the application alive: the failing action is aborted, the rest keeps working.
        LogCrash("Dispatcher", e.Exception);
        e.Handled = true;

        try
        {
            MessageBox.Show(
                e.Exception.Message,
                "SporeMaster 3 — unexpected error (the application will continue)",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        catch
        {
            // If even the message box fails, there is nothing more we can do.
        }
    }

    /// <summary>Appends an exception to the crash log; never throws.</summary>
    public static void LogCrash(string context, Exception exception)
    {
        try
        {
            string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {context}: {exception}{Environment.NewLine}";
            lock (LogLock)
            {
                File.AppendAllText(CrashLogPath, line, Encoding.UTF8);
            }
        }
        catch
        {
            // Logging must never be the cause of a failure.
        }
    }
}
