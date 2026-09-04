using Microsoft.Win32;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace PhantomInstaller;

public partial class MainWindow : Window
{
    private string _launchOption = "+exec Phantom.cfg";

    public MainWindow() => InitializeComponent();

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(420))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        });
        WelcomeArtwork.BeginAnimation(OpacityProperty, new DoubleAnimation(0.69, 0.82, TimeSpan.FromSeconds(4.5))
        {
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        });
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed) DragMove();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
    private void Finish_Click(object sender, RoutedEventArgs e) => Close();

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(_launchOption);
        CopyButton.Content = "СКОПИРОВАНО ✓";
    }

    private async void PresetButton_Click(object sender, RoutedEventArgs e)
    {
        await InstallCfgAsync("Phantom.cfg", async tempPath =>
        {
            var resource = Application.GetResourceStream(new Uri("pack://application:,,,/Phantom.cfg"));
            if (resource is null) throw new InvalidOperationException("В установщике отсутствует встроенный Phantom.cfg.");
            await using (resource.Stream)
            await using (var output = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
                await resource.Stream.CopyToAsync(output);
        }, "УСТАНАВЛИВАЮ PHANTOM");
    }

    private async void CustomButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Выбери CFG для установки",
            Filter = "CS2 config (*.cfg)|*.cfg|Все файлы (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };
        if (dialog.ShowDialog() != true) return;

        string source = dialog.FileName;
        string fileName = Path.GetFileName(source);
        if (!fileName.EndsWith(".cfg", StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show("Выбери файл с расширением .cfg.", "Phantom Installer", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await InstallCfgAsync(fileName, tempPath => Task.Run(() => File.Copy(source, tempPath, true)), "УСТАНАВЛИВАЮ ТВОЙ CFG");
    }

    private async void BuildButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Назови новый CFG и выбери, куда его сохранить",
            Filter = "CS2 config (*.cfg)|*.cfg",
            DefaultExt = ".cfg",
            AddExtension = true,
            FileName = "MyConfig.cfg",
            OverwritePrompt = true
        };
        if (dialog.ShowDialog() != true) return;

        SwapPanels(IntroPanel, InstallPanel);
        StartInstallArtworkAnimation();
        InstallTitle.Text = "СОБИРАЮ ТВОЙ CFG";
        InstallStatus.Text = "Ищу текущие настройки Counter-Strike 2…";

        try
        {
            string cfgText = await Task.Run(BuildCfgSnapshot);
            InstallStatus.Text = "Сохраняю новый CFG…";
            await File.WriteAllTextAsync(dialog.FileName, cfgText, new UTF8Encoding(false));
            await Task.Delay(250);
            StopInstallArtworkAnimation();
            ShowDone("CFG СОЗДАН", "Текущие настройки CS2 сохранены", dialog.FileName, null);
        }
        catch (Exception ex)
        {
            StopInstallArtworkAnimation();
            ShowError("Не удалось создать CFG: " + ex.Message);
        }
    }

    private async Task InstallCfgAsync(string fileName, Func<string, Task> writeTemp, string title)
    {
        SwapPanels(IntroPanel, InstallPanel);
        StartInstallArtworkAnimation();
        InstallTitle.Text = title;
        InstallStatus.Text = "Ищу установленный Counter-Strike 2…";

        try
        {
            string? cfgDirectory = await Task.Run(FindCs2CfgDirectory);
            if (cfgDirectory is null) throw new DirectoryNotFoundException("Не удалось автоматически найти Counter-Strike 2.");

            Directory.CreateDirectory(cfgDirectory);
            string safeName = Path.GetFileName(fileName);
            string destination = Path.Combine(cfgDirectory, safeName);
            string temp = destination + ".phantom-tmp";

            InstallStatus.Text = "Создаю резервную копию и копирую CFG…";
            if (File.Exists(destination))
            {
                string backup = destination + $".backup-{DateTime.Now:yyyyMMdd-HHmmss}";
                File.Copy(destination, backup, false);
            }

            if (File.Exists(temp)) File.Delete(temp);
            await writeTemp(temp);
            if (!File.Exists(temp) || new FileInfo(temp).Length < 2) throw new IOException("CFG не удалось подготовить.");
            File.Move(temp, destination, true);

            InstallStatus.Text = "Проверяю установку…";
            await Task.Delay(250);
            if (!File.Exists(destination)) throw new IOException("CFG не найден после установки.");

            _launchOption = safeName.Contains(' ') ? $"+exec \"{safeName}\"" : $"+exec {safeName}";
            StopInstallArtworkAnimation();
            ShowDone("ГОТОВО", $"{safeName} установлен", destination, _launchOption);
        }
        catch (Exception ex)
        {
            StopInstallArtworkAnimation();
            ShowError("Установка не завершена: " + ex.Message);
        }
    }

    private void ShowDone(string title, string subtitle, string path, string? launchOption)
    {
        DoneTitle.Text = title;
        DoneSubtitle.Text = subtitle;
        InstalledPath.Text = path;
        CopyButton.Content = "КОПИРОВАТЬ";
        if (launchOption is null)
        {
            LaunchBox.Visibility = Visibility.Collapsed;
        }
        else
        {
            _launchOption = launchOption;
            LaunchCommandText.Text = launchOption;
            LaunchBox.Visibility = Visibility.Visible;
        }
        SwapPanels(InstallPanel, DonePanel);
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        SwapPanels(InstallPanel, ErrorPanel);
    }

    private void StartInstallArtworkAnimation()
    {
        InstallArtwork.BeginAnimation(OpacityProperty, new DoubleAnimation(0.47, 0.67, TimeSpan.FromSeconds(1.8))
        {
            AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        });
        var scale = new DoubleAnimation(1.035, 1.065, TimeSpan.FromSeconds(3.2))
        {
            AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        };
        InstallArtworkScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, scale);
        InstallArtworkScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, scale);
    }

    private void StopInstallArtworkAnimation()
    {
        InstallArtwork.BeginAnimation(OpacityProperty, null);
        InstallArtworkScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, null);
        InstallArtworkScale.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, null);
    }

    private static void SwapPanels(UIElement from, UIElement to)
    {
        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(150));
        fadeOut.Completed += (_, _) =>
        {
            from.Visibility = Visibility.Collapsed;
            to.Opacity = 0;
            to.Visibility = Visibility.Visible;
            to.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(290))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            });
        };
        from.BeginAnimation(OpacityProperty, fadeOut);
    }

    private static string BuildCfgSnapshot()
    {
        string? userCfg = FindSteamUserCfgDirectory();
        if (userCfg is null) throw new DirectoryNotFoundException("Не найдены пользовательские настройки CS2 в Steam userdata.");

        var convars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var binds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (string file in Directory.GetFiles(userCfg, "cs2_user_convars*.vcfg").OrderBy(File.GetLastWriteTimeUtc))
        {
            foreach (var pair in ReadVdfPairs(file))
                if (LooksLikeClientConvar(pair.Key)) convars[pair.Key] = pair.Value;
        }

        foreach (string file in Directory.GetFiles(userCfg, "cs2_user_keys*.vcfg").OrderBy(File.GetLastWriteTimeUtc))
        {
            foreach (var pair in ReadVdfPairs(file))
                if (LooksLikeBindKey(pair.Key) && !string.IsNullOrWhiteSpace(pair.Value)) binds[pair.Key] = pair.Value;
        }

        if (convars.Count == 0 && binds.Count == 0)
            throw new InvalidDataException("Файлы CS2 найдены, но настройки из них прочитать не удалось.");

        var sb = new StringBuilder();
        sb.AppendLine("// ========================================");
        sb.AppendLine("// CS2 CFG — exported by Phantom Installer");
        sb.AppendLine($"// Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("// Source: Steam userdata / 730 / local / cfg");
        sb.AppendLine("// ========================================");
        sb.AppendLine();
        sb.AppendLine("// SETTINGS");
        foreach (var pair in convars.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
            sb.AppendLine($"{pair.Key} \"{EscapeCfg(pair.Value)}\"");

        if (binds.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("// BINDS");
            foreach (var pair in binds.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
                sb.AppendLine($"bind \"{EscapeCfg(pair.Key)}\" \"{EscapeCfg(pair.Value)}\"");
        }

        sb.AppendLine();
        sb.AppendLine("echo \"CFG loaded\"");
        return sb.ToString();
    }

    private static IEnumerable<KeyValuePair<string, string>> ReadVdfPairs(string path)
    {
        string text = File.ReadAllText(path);
        var rx = new Regex("\\\"(?<k>[^\\\"]+)\\\"\\s*\\\"(?<v>[^\\\"]*)\\\"", RegexOptions.CultureInvariant);
        foreach (Match match in rx.Matches(text))
        {
            string key = UnescapeVdf(match.Groups["k"].Value);
            string value = UnescapeVdf(match.Groups["v"].Value);
            yield return new KeyValuePair<string, string>(key, value);
        }
    }

    private static bool LooksLikeClientConvar(string key)
    {
        if (!Regex.IsMatch(key, "^[A-Za-z_][A-Za-z0-9_.]*$")) return false;
        string[] prefixes = { "cl_", "hud_", "snd_", "voice_", "viewmodel_", "r_", "fps_", "engine_", "m_", "input_", "option_", "spec_", "safezone", "mapoverview_", "dsp_" };
        if (prefixes.Any(p => key.StartsWith(p, StringComparison.OrdinalIgnoreCase))) return true;
        return key.Equals("sensitivity", StringComparison.OrdinalIgnoreCase)
            || key.Equals("zoom_sensitivity_ratio", StringComparison.OrdinalIgnoreCase)
            || key.Equals("crosshair", StringComparison.OrdinalIgnoreCase)
            || key.Equals("volume", StringComparison.OrdinalIgnoreCase)
            || key.Equals("con_enable", StringComparison.OrdinalIgnoreCase)
            || key.Equals("rate", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikeBindKey(string key)
    {
        return Regex.IsMatch(key,
            "^(?:[A-Z0-9]|F(?:[1-9]|1[0-2])|MOUSE[1-5]|MWHEELUP|MWHEELDOWN|SPACE|SHIFT|CTRL|ALT|TAB|ENTER|ESCAPE|BACKSPACE|CAPSLOCK|UPARROW|DOWNARROW|LEFTARROW|RIGHTARROW|INS|DEL|HOME|END|PGUP|PGDN|SEMICOLON|APOSTROPHE|BACKQUOTE|COMMA|PERIOD|SLASH|MINUS|EQUALS)$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private static string EscapeCfg(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    private static string UnescapeVdf(string value) => value.Replace("\\\"", "\"").Replace("\\\\", "\\");

    private static string? FindSteamUserCfgDirectory()
    {
        var steamRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void Add(string? p) { if (!string.IsNullOrWhiteSpace(p) && Directory.Exists(p)) steamRoots.Add(p); }
        Add(ReadRegistrySteamPath());
        Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam"));
        Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Steam"));
        Add(@"D:\Steam");

        var candidates = new List<string>();
        foreach (string root in steamRoots)
        {
            string userdata = Path.Combine(root, "userdata");
            if (!Directory.Exists(userdata)) continue;
            foreach (string account in Directory.GetDirectories(userdata))
            {
                string cfg = Path.Combine(account, "730", "local", "cfg");
                if (Directory.Exists(cfg)) candidates.Add(cfg);
            }
        }

        return candidates
            .OrderByDescending(GetLatestCs2ConfigWriteTime)
            .FirstOrDefault();
    }

    private static DateTime GetLatestCs2ConfigWriteTime(string cfg)
    {
        try
        {
            var files = Directory.GetFiles(cfg, "cs2_*.*");
            return files.Length == 0 ? Directory.GetLastWriteTimeUtc(cfg) : files.Max(File.GetLastWriteTimeUtc);
        }
        catch { return DateTime.MinValue; }
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
            string vdf = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
            if (File.Exists(vdf))
            {
                foreach (Match m in Regex.Matches(File.ReadAllText(vdf), "\\\"path\\\"\\s*\\\"([^\\\"]+)\\\"", RegexOptions.IgnoreCase))
                    AddLibrary(m.Groups[1].Value.Replace("\\\\", "\\"));
            }
        }

        foreach (string library in libraries)
        {
            string cfg = Path.Combine(library, "steamapps", "common", "Counter-Strike Global Offensive", "game", "csgo", "cfg");
            string game = Path.GetDirectoryName(cfg)!;
            if (Directory.Exists(cfg) || Directory.Exists(game)) return cfg;
        }

        foreach (DriveInfo drive in DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed))
        {
            foreach (string rootName in new[] { "SteamLibrary", "Steam" })
            {
                string cfg = Path.Combine(drive.RootDirectory.FullName, rootName, "steamapps", "common", "Counter-Strike Global Offensive", "game", "csgo", "cfg");
                if (Directory.Exists(cfg) || Directory.Exists(Path.GetDirectoryName(cfg)!)) return cfg;
            }
        }
        return null;
    }

    private static string? ReadRegistrySteamPath()
    {
        string[] keys =
        {
            @"HKEY_CURRENT_USER\Software\Valve\Steam",
            @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam",
            @"HKEY_LOCAL_MACHINE\SOFTWARE\Valve\Steam"
        };
        foreach (string key in keys)
            foreach (string valueName in new[] { "SteamPath", "InstallPath" })
                if (Registry.GetValue(key, valueName, null) is string path && Directory.Exists(path)) return path;
        return null;
    }
}
