using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace PhantomInstaller;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;

        try
        {
            var window = new MainWindow();
            MainWindow = window;

            // CI-only startup probe: construct the real WPF window and invoke its Loaded path
            // without requiring an interactive desktop or UAC prompt.
            if (string.Equals(Environment.GetEnvironmentVariable("PHANTOM_STARTUP_SMOKE"), "1", StringComparison.Ordinal))
            {
                window.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
                window.Close();
                Shutdown(0);
                return;
            }

            window.Show();
        }
        catch (Exception ex)
        {
            ReportFatal("startup", ex);
            Shutdown(1);
        }
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        ReportFatal("dispatcher", e.Exception);
        e.Handled = true;
        Shutdown(1);
    }

    private static void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            WriteCrashLog("domain", ex);
    }

    private static void ReportFatal(string stage, Exception ex)
    {
        string path = WriteCrashLog(stage, ex);
        try
        {
            MessageBox.Show(
                $"Phantom Installer не смог запуститься.\n\n{ex.GetType().Name}: {ex.Message}\n\nЛог сохранён:\n{path}",
                "Phantom Installer — ошибка запуска",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch
        {
            // If even the message box cannot be shown, the log still remains on disk.
        }
    }

    private static string WriteCrashLog(string stage, Exception ex)
    {
        string directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PhantomInstaller");
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "startup.log");

        var sb = new StringBuilder();
        sb.AppendLine($"UTC: {DateTime.UtcNow:O}");
        sb.AppendLine($"Stage: {stage}");
        sb.AppendLine($"OS: {Environment.OSVersion}");
        sb.AppendLine($"64-bit OS: {Environment.Is64BitOperatingSystem}");
        sb.AppendLine($"64-bit process: {Environment.Is64BitProcess}");
        sb.AppendLine();
        sb.AppendLine(ex.ToString());

        try { File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false)); }
        catch { }
        return path;
    }
}