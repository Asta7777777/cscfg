using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace PhantomInstaller;

public partial class MainWindow : Window
{
    private const string LaunchOption = "+exec Phantom.cfg";

    public MainWindow()
    {
        InitializeComponent();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(420))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        });
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
    private void Finish_Click(object sender, RoutedEventArgs e) => Close();

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(LaunchOption);
        CopyButton.Content = "СКОПИРОВАНО ✓";
    }

    private async void InstallButton_Click(object sender, RoutedEventArgs e)
    {
        InstallButton.IsEnabled = false;
        SwapPanels(IntroPanel, InstallPanel);

        try
        {
            await Task.Delay(450);
            InstallStatus.Text = "Ищу установленный Counter-Strike 2…";

            string? cfgDirectory = await Task.Run(FindCs2CfgDirectory);
            if (cfgDirectory is null)
            {
                ShowError("Не удалось автоматически найти Counter-Strike 2. Проверь, что Steam и CS2 установлены, затем запусти установщик снова.");
                return;
            }

            InstallStatus.Text = "Копирую Phantom.cfg…";
            await Task.Delay(300);

            Directory.CreateDirectory(cfgDirectory);
            string destination = Path.Combine(cfgDirectory, "Phantom.cfg");

            if (File.Exists(destination))
            {
                string backup = Path.Combine(cfgDirectory, $"Phantom.cfg.backup-{DateTime.Now:yyyyMMdd-HHmmss}");
                File.Copy(destination, backup, false);
            }

            var resource = Application.GetResourceStream(new Uri("pack://application:,,,/Phantom.cfg"));
            if (resource is null)
                throw new InvalidOperationException("В установщике отсутствует Phantom.cfg.");

            string temp = destination + ".tmp";
            await using (resource.Stream)
            await using (var output = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
            {
                await resource.Stream.CopyToAsync(output);
            }

            File.Copy(temp, destination, true);
            File.Delete(temp);

            InstallStatus.Text = "Проверяю установку…";
            await Task.Delay(300);

            if (!File.Exists(destination) || new FileInfo(destination).Length < 100)
                throw new IOException("Phantom.cfg не удалось записать корректно.");

            InstalledPath.Text = destination;
            SwapPanels(InstallPanel, DonePanel);
        }
        catch (UnauthorizedAccessException)
        {
            ShowError("Windows запретил запись в папку CS2. Перезапусти установщик и подтверди запрос администратора.");
        }
        catch (Exception ex)
        {
            ShowError("Установка не завершена: " + ex.Message);
        }
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        SwapPanels(InstallPanel, ErrorPanel);
    }

    private static void SwapPanels(UIElement from, UIElement to)
    {
        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(160));
        fadeOut.Completed += (_, _) =>
        {
            from.Visibility = Visibility.Collapsed;
            to.Opacity = 0;
            to.Visibility = Visibility.Visible;
            to.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            });
        };
        from.BeginAnimation(OpacityProperty, fadeOut);
    }

    private static string? FindCs2CfgDirectory()
    {
        var libraries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddLibrary(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            path = path.Trim().Trim('"').Replace('/', '\\');
            if (Directory.Exists(path)) libraries.Add(path);
        }

        string? steamPath = ReadRegistrySteamPath();
        AddLibrary(steamPath);
        AddLibrary(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam"));
        AddLibrary(@"D:\SteamLibrary");

        if (!string.IsNullOrWhiteSpace(steamPath))
        {
            string libraryVdf = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
            if (File.Exists(libraryVdf))
            {
                string text = File.ReadAllText(libraryVdf);
                foreach (Match match in Regex.Matches(text, "\\\"path\\\"\\s*\\\"([^\\\"]+)\\\"", RegexOptions.IgnoreCase))
                    AddLibrary(match.Groups[1].Value.Replace("\\\\", "\\"));
            }
        }

        foreach (string library in libraries.ToArray())
        {
            string cfg = Path.Combine(library, "steamapps", "common", "Counter-Strike Global Offensive", "game", "csgo", "cfg");
            string game = Path.Combine(library, "steamapps", "common", "Counter-Strike Global Offensive", "game", "csgo");
            if (Directory.Exists(cfg) || Directory.Exists(game))
                return cfg;
        }

        // Small, predictable fallback scan: only the conventional Steam/SteamLibrary roots.
        foreach (DriveInfo drive in DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed))
        {
            foreach (string rootName in new[] { "SteamLibrary", "Steam" })
            {
                string root = Path.Combine(drive.RootDirectory.FullName, rootName);
                string cfg = Path.Combine(root, "steamapps", "common", "Counter-Strike Global Offensive", "game", "csgo", "cfg");
                string game = Path.GetDirectoryName(cfg)!;
                if (Directory.Exists(cfg) || Directory.Exists(game))
                    return cfg;
            }
        }

        return null;
    }

    private static string? ReadRegistrySteamPath()
    {
        string?[] keys =
        {
            @"HKEY_CURRENT_USER\Software\Valve\Steam",
            @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam",
            @"HKEY_LOCAL_MACHINE\SOFTWARE\Valve\Steam"
        };

        foreach (string key in keys)
        {
            foreach (string valueName in new[] { "SteamPath", "InstallPath" })
            {
                if (Registry.GetValue(key, valueName, null) is string path && Directory.Exists(path))
                    return path;
            }
        }

        return null;
    }
}
