using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace PhantomInstaller;

public partial class MainWindow : Window
{
    private string _launchOption = "+exec Phantom.cfg";
    private readonly ObservableCollection<SettingItem> _allSettings = new();
    private HardwareProfile? _hardwareProfile;

    public MainWindow() => InitializeComponent();

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        SignatureNameBox.Text = Environment.UserName;
        SettingsGrid.ItemsSource = _allSettings;
        ApplySettingsFilter();

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
    private void BackToHome_Click(object sender, RoutedEventArgs e) => SwapPanels(SettingsPanel, IntroPanel);

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

        SignatureState signature = InspectPhantomSignature(source);
        if (signature == SignatureState.Invalid)
        {
            var answer = MessageBox.Show(
                "В CFG найдена подпись Phantom, но контрольная сумма не совпадает. Файл меняли после создания. Всё равно установить?",
                "Phantom signature",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (answer != MessageBoxResult.Yes) return;
        }

        await InstallCfgAsync(fileName, tempPath => Task.Run(() => File.Copy(source, tempPath, true)), "УСТАНАВЛИВАЮ ТВОЙ CFG");
    }

    private async void OpenSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        SwapPanels(IntroPanel, SettingsPanel);
        await LoadSettingsAsync(force: _allSettings.Count == 0);
    }

    private async void RefreshSettings_Click(object sender, RoutedEventArgs e) => await LoadSettingsAsync(force: true);

    private async Task LoadSettingsAsync(bool force)
    {
        if (!force && _allSettings.Count > 0) return;
        HardwareSummaryText.Text = "Читаю локальные настройки CS2…";
        try
        {
            List<SettingItem> loaded = await Task.Run(LoadAllCs2Settings);
            _allSettings.Clear();
            foreach (SettingItem item in loaded) _allSettings.Add(item);
            SettingsCountText.Text = $"Найдено: {_allSettings.Count}";
            HardwareSummaryText.Text = "Настройки загружены. Нажми «Оптимизировать», чтобы определить железо, мониторы и применить безопасный профиль.";
            ApplySettingsFilter();
        }
        catch (Exception ex)
        {
            HardwareSummaryText.Text = "Не удалось прочитать CS2: " + ex.Message;
            SettingsCountText.Text = "Найдено: 0";
        }
    }

    private void SettingsSearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplySettingsFilter();

    private void ApplySettingsFilter()
    {
        if (SettingsGrid?.ItemsSource is null) return;
        string query = SettingsSearchBox?.Text?.Trim() ?? string.Empty;
        ICollectionView view = CollectionViewSource.GetDefaultView(SettingsGrid.ItemsSource);
        view.Filter = obj =>
        {
            if (obj is not SettingItem item) return false;
            if (string.IsNullOrWhiteSpace(query)) return true;
            return item.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                || item.Value.Contains(query, StringComparison.OrdinalIgnoreCase)
                || item.Category.Contains(query, StringComparison.OrdinalIgnoreCase)
                || item.Source.Contains(query, StringComparison.OrdinalIgnoreCase);
        };
        view.Refresh();
    }

    private async void OptimizeButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            HardwareSummaryText.Text = "Анализирую CPU, GPU, RAM и дисплеи…";
            _hardwareProfile = await Task.Run(DetectHardware);
            OptimizationProfile profile = BuildOptimizationProfile(_hardwareProfile);
            ApplyOptimization(profile);
            HardwareSummaryText.Text = BuildHardwareSummary(_hardwareProfile, profile);
            SettingsGrid.Items.Refresh();
            SettingsCountText.Text = $"Найдено: {_allSettings.Count} • оптимизировано";
        }
        catch (Exception ex)
        {
            HardwareSummaryText.Text = "Оптимизация не завершена: " + ex.Message;
        }
    }

    private async void SaveAsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            CommitSettingsGridEdits();
            string fileName = NormalizeCfgFileName(ConfigNameBox.Text);
            string owner = SanitizeOwner(SignatureNameBox.Text);
            string text = BuildSignedCfg(owner);

            var dialog = new SaveFileDialog
            {
                Title = "Сохранить подписанный CFG",
                Filter = "CS2 config (*.cfg)|*.cfg",
                DefaultExt = ".cfg",
                AddExtension = true,
                FileName = fileName,
                OverwritePrompt = true
            };
            if (dialog.ShowDialog() != true) return;

            await File.WriteAllTextAsync(dialog.FileName, text, new UTF8Encoding(false));
            ShowDoneFrom(SettingsPanel, "CFG СОЗДАН", $"Подписан для {owner}", dialog.FileName, null);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Phantom CFG Builder", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void BuildInstallButton_Click(object sender, RoutedEventArgs e)
    {
        CommitSettingsGridEdits();
        string fileName;
        string owner;
        string text;
        try
        {
            fileName = NormalizeCfgFileName(ConfigNameBox.Text);
            owner = SanitizeOwner(SignatureNameBox.Text);
            text = BuildSignedCfg(owner);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Phantom CFG Builder", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        SwapPanels(SettingsPanel, InstallPanel);
        StartInstallArtworkAnimation();
        InstallTitle.Text = "СОЗДАЮ И УСТАНАВЛИВАЮ CFG";

        try
        {
            string downloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            Directory.CreateDirectory(downloads);
            string downloadPath = Path.Combine(downloads, fileName);

            InstallStatus.Text = "Подписываю CFG и сохраняю копию в Загрузки…";
            if (File.Exists(downloadPath))
                File.Copy(downloadPath, downloadPath + $".backup-{DateTime.Now:yyyyMMdd-HHmmss}", false);
            await File.WriteAllTextAsync(downloadPath, text, new UTF8Encoding(false));

            string? cfgDirectory = await Task.Run(FindCs2CfgDirectory);
            if (cfgDirectory is null)
                throw new DirectoryNotFoundException("Counter-Strike 2 не найден ни в одной Steam-библиотеке.");

            InstallStatus.Text = "Устанавливаю этот же CFG в Counter-Strike 2…";
            Directory.CreateDirectory(cfgDirectory);
            string destination = Path.Combine(cfgDirectory, fileName);
            if (File.Exists(destination))
                File.Copy(destination, destination + $".backup-{DateTime.Now:yyyyMMdd-HHmmss}", false);
            File.Copy(downloadPath, destination, true);

            if (!File.Exists(destination) || new FileInfo(destination).Length < 2)
                throw new IOException("CFG не удалось установить.");

            _launchOption = BuildLaunchOption(fileName);
            await Task.Delay(250);
            StopInstallArtworkAnimation();
            ShowDoneFrom(InstallPanel, "ГОТОВО", $"{fileName} сохранён в Загрузки и установлен в CS2", downloadPath, _launchOption);
        }
        catch (Exception ex)
        {
            StopInstallArtworkAnimation();
            ShowErrorFrom(InstallPanel, "Не удалось создать или установить CFG: " + ex.Message);
        }
    }

    private void CommitSettingsGridEdits()
    {
        SettingsGrid.CommitEdit(DataGridEditingUnit.Cell, true);
        SettingsGrid.CommitEdit(DataGridEditingUnit.Row, true);
    }

    private string BuildSignedCfg(string owner)
    {
        if (_allSettings.Count == 0)
            throw new InvalidOperationException("Сначала загрузи настройки CS2.");

        string id = Guid.NewGuid().ToString("N");
        string body = BuildCfgPayload(owner, id);
        string signature = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(owner + "\n" + id + "\n" + body))).ToLowerInvariant();

        var sb = new StringBuilder();
        sb.AppendLine("// ============================================================================");
        sb.AppendLine("// PHANTOM CFG • GENERATED LOCALLY");
        sb.AppendLine($"// PHANTOM-OWNER: {owner}");
        sb.AppendLine($"// PHANTOM-ID: {id}");
        sb.AppendLine($"// PHANTOM-GENERATED: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"// PHANTOM-SIGNATURE-SHA256: {signature}");
        sb.AppendLine("// Watermark lines are comments only and do not change CS2 behavior.");
        sb.AppendLine("// PHANTOM-PAYLOAD-BEGIN");
        sb.Append(body);
        if (!body.EndsWith('\n')) sb.AppendLine();
        sb.AppendLine("// PHANTOM-PAYLOAD-END");
        sb.AppendLine($"// PHANTOM-SIGNATURE-SHA256: {signature}");
        sb.AppendLine($"// by Phantom • {owner}");
        sb.AppendLine($"// PHANTOM WATERMARK • {owner} • {id}");
        return sb.ToString();
    }

    private string BuildCfgPayload(string owner, string id)
    {
        var sb = new StringBuilder();
        int commandIndex = 0;

        void Watermark()
        {
            sb.AppendLine($"// ── PHANTOM • {owner} • {id[..8]} ─────────────────────────────────────────");
        }

        Watermark();
        sb.AppendLine("// ACTIVE CS2 SETTINGS");
        foreach (SettingItem item in _allSettings
                     .Where(x => x.Included && x.Kind == SettingKind.Convar && LooksLikeCfgCommandName(x.Name))
                     .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
        {
            sb.AppendLine($"{item.Name} \"{EscapeCfg(item.Value)}\"");
            if (++commandIndex % 16 == 0) Watermark();
        }

        sb.AppendLine();
        Watermark();
        sb.AppendLine("// BINDS");
        foreach (SettingItem item in _allSettings
                     .Where(x => x.Included && x.Kind == SettingKind.Bind && LooksLikeBindKey(x.Name))
                     .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
        {
            sb.AppendLine($"bind \"{EscapeCfg(item.Name)}\" \"{EscapeCfg(item.Value)}\"");
            if (++commandIndex % 16 == 0) Watermark();
        }

        sb.AppendLine();
        Watermark();
        sb.AppendLine("// DISCOVERED MACHINE / VIDEO / OTHER CS2 SETTINGS");
        sb.AppendLine("// Stored as comments because not every machine/video key is a console command.");
        foreach (SettingItem item in _allSettings
                     .Where(x => x.Kind == SettingKind.Raw)
                     .OrderBy(x => x.Category, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
        {
            sb.AppendLine($"// [{SanitizeComment(item.Category)}] {SanitizeComment(item.Name)} = {SanitizeComment(item.Value)}");
            if (++commandIndex % 20 == 0) Watermark();
        }

        sb.AppendLine();
        Watermark();
        sb.AppendLine($"echo \"Phantom CFG loaded • {EscapeCfg(owner)}\"");
        return sb.ToString();
    }

    private async Task InstallCfgAsync(string fileName, Func<string, Task> writeTemp, string title)
    {
        SwapPanels(IntroPanel, InstallPanel);
        StartInstallArtworkAnimation();
        InstallTitle.Text = title;
        InstallStatus.Text = "Ищу Counter-Strike 2 во всех Steam-библиотеках и на доступных дисках…";

        try
        {
            string? cfgDirectory = await Task.Run(FindCs2CfgDirectory);
            if (cfgDirectory is null) throw new DirectoryNotFoundException("Не удалось автоматически найти Counter-Strike 2.");

            Directory.CreateDirectory(cfgDirectory);
            string safeName = NormalizeCfgFileName(fileName);
            string destination = Path.Combine(cfgDirectory, safeName);
            string temp = destination + ".phantom-tmp";

            InstallStatus.Text = "Создаю резервную копию и копирую CFG…";
            if (File.Exists(destination))
                File.Copy(destination, destination + $".backup-{DateTime.Now:yyyyMMdd-HHmmss}", false);

            if (File.Exists(temp)) File.Delete(temp);
            await writeTemp(temp);
            if (!File.Exists(temp) || new FileInfo(temp).Length < 2) throw new IOException("CFG не удалось подготовить.");
            File.Move(temp, destination, true);

            InstallStatus.Text = "Проверяю установку…";
            await Task.Delay(250);
            if (!File.Exists(destination)) throw new IOException("CFG не найден после установки.");

            _launchOption = BuildLaunchOption(safeName);
            StopInstallArtworkAnimation();
            ShowDoneFrom(InstallPanel, "ГОТОВО", $"{safeName} установлен", destination, _launchOption);
        }
        catch (Exception ex)
        {
            StopInstallArtworkAnimation();
            ShowErrorFrom(InstallPanel, "Установка не завершена: " + ex.Message);
        }
    }

    private void ShowDoneFrom(UIElement from, string title, string subtitle, string path, string? launchOption)
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
        SwapPanels(from, DonePanel);
    }

    private void ShowErrorFrom(UIElement from, string message)
    {
        ErrorText.Text = message;
        SwapPanels(from, ErrorPanel);
    }

    private void StartInstallArtworkAnimation()
    {
        InstallArtwork.BeginAnimation(OpacityProperty, new DoubleAnimation(0.47, 0.67, TimeSpan.FromSeconds(1.8))
        {
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        });
        var scale = new DoubleAnimation(1.035, 1.065, TimeSpan.FromSeconds(3.2))
        {
            AutoReverse = true,
            RepeatBehavior = RepeatBehavior.Forever,
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
        var fadeOut = new DoubleAnimation(from.Opacity <= 0 ? 1 : from.Opacity, 0, TimeSpan.FromMilliseconds(150));
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

    private static List<SettingItem> LoadAllCs2Settings()
    {
        string? userCfg = FindSteamUserCfgDirectory();
        if (userCfg is null) throw new DirectoryNotFoundException("Не найдены пользовательские настройки CS2 в Steam userdata\\<account>\\730\\local\\cfg.");

        var map = new Dictionary<string, SettingItem>(StringComparer.OrdinalIgnoreCase);
        string[] files = Directory.GetFiles(userCfg, "*.*", SearchOption.TopDirectoryOnly)
            .Where(p => p.EndsWith(".vcfg", StringComparison.OrdinalIgnoreCase) || p.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            .OrderBy(File.GetLastWriteTimeUtc)
            .ToArray();

        foreach (string file in files)
        {
            string source = Path.GetFileName(file);
            bool keysFile = source.Contains("keys", StringComparison.OrdinalIgnoreCase);
            bool convarFile = source.Contains("convars", StringComparison.OrdinalIgnoreCase) && !source.Contains("machine", StringComparison.OrdinalIgnoreCase);
            string rawCategory = source.Contains("video", StringComparison.OrdinalIgnoreCase) ? "Video"
                : source.Contains("machine", StringComparison.OrdinalIgnoreCase) ? "Machine"
                : "Other";

            foreach (var pair in ReadVdfPairs(file))
            {
                string key = pair.Key.Trim();
                string value = pair.Value;
                if (string.IsNullOrWhiteSpace(key)) continue;

                SettingItem item;
                string dictionaryKey;
                if (keysFile && LooksLikeBindKey(key))
                {
                    item = new SettingItem(true, SettingKind.Bind, "Bind", key, value, source);
                    dictionaryKey = "bind|" + key;
                }
                else if (convarFile && LooksLikeCfgCommandName(key))
                {
                    item = new SettingItem(true, SettingKind.Convar, GetConvarCategory(key), key, value, source);
                    dictionaryKey = "convar|" + key;
                }
                else
                {
                    item = new SettingItem(false, SettingKind.Raw, rawCategory, key, value, source);
                    dictionaryKey = "raw|" + source + "|" + key;
                }
                map[dictionaryKey] = item;
            }
        }

        if (map.Count == 0) throw new InvalidDataException("Файлы CS2 найдены, но настройки прочитать не удалось.");
        return map.Values
            .OrderBy(x => x.Kind)
            .ThenBy(x => x.Category, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private void ApplyOptimization(OptimizationProfile profile)
    {
        SetConvar("fps_max", profile.FpsMax.ToString(CultureInfo.InvariantCulture), "Performance");
        SetConvar("fps_max_ui", profile.FpsMaxUi.ToString(CultureInfo.InvariantCulture), "Performance");
        SetConvar("engine_no_focus_sleep", "20", "Performance");
        SetConvar("r_player_visibility_mode", "1", "Video");
        SetConvar("snd_mixahead", "0.001", "Audio");
        SetConvar("rate", "786432", "Network");
    }

    private void SetConvar(string name, string value, string category)
    {
        SettingItem? existing = _allSettings.FirstOrDefault(x => x.Kind == SettingKind.Convar && x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (existing is null)
            _allSettings.Add(new SettingItem(true, SettingKind.Convar, category, name, value, "Phantom Optimizer"));
        else
        {
            existing.Value = value;
            existing.Included = true;
            existing.Source = existing.Source.Contains("Optimizer", StringComparison.OrdinalIgnoreCase) ? existing.Source : existing.Source + " + Optimizer";
        }
    }

    private static HardwareProfile DetectHardware()
    {
        string cpu = "Unknown CPU";
        int logical = Environment.ProcessorCount;
        double ramGb = 0;
        var gpus = new List<string>();
        var monitorBits = new List<string>();
        int maxRefresh = 0;
        int maxWidth = 0;
        int maxHeight = 0;

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name,NumberOfLogicalProcessors FROM Win32_Processor");
            foreach (ManagementObject o in searcher.Get())
            {
                cpu = Convert.ToString(o["Name"])?.Trim() ?? cpu;
                if (int.TryParse(Convert.ToString(o["NumberOfLogicalProcessors"]), out int parsed)) logical = Math.Max(logical, parsed);
                break;
            }
        }
        catch { }

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
            foreach (ManagementObject o in searcher.Get())
            {
                if (double.TryParse(Convert.ToString(o["TotalPhysicalMemory"]), NumberStyles.Any, CultureInfo.InvariantCulture, out double bytes))
                    ramGb = bytes / 1024d / 1024d / 1024d;
                break;
            }
        }
        catch { }

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name,CurrentHorizontalResolution,CurrentVerticalResolution,CurrentRefreshRate FROM Win32_VideoController");
            foreach (ManagementObject o in searcher.Get())
            {
                string name = Convert.ToString(o["Name"])?.Trim() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(name) && !gpus.Contains(name, StringComparer.OrdinalIgnoreCase)) gpus.Add(name);
                int.TryParse(Convert.ToString(o["CurrentHorizontalResolution"]), out int w);
                int.TryParse(Convert.ToString(o["CurrentVerticalResolution"]), out int h);
                int.TryParse(Convert.ToString(o["CurrentRefreshRate"]), out int hz);
                if (w > 0 && h > 0)
                {
                    monitorBits.Add($"{w}×{h}{(hz > 0 ? $" @{hz}Hz" : string.Empty)}");
                    if (w * h > maxWidth * maxHeight) { maxWidth = w; maxHeight = h; }
                }
                maxRefresh = Math.Max(maxRefresh, hz);
            }
        }
        catch { }

        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name,ScreenWidth,ScreenHeight FROM Win32_DesktopMonitor");
            foreach (ManagementObject o in searcher.Get())
            {
                string name = Convert.ToString(o["Name"])?.Trim() ?? string.Empty;
                int.TryParse(Convert.ToString(o["ScreenWidth"]), out int w);
                int.TryParse(Convert.ToString(o["ScreenHeight"]), out int h);
                if (!string.IsNullOrWhiteSpace(name) && !monitorBits.Any(x => x.Contains(name, StringComparison.OrdinalIgnoreCase)))
                    monitorBits.Add(name + (w > 0 && h > 0 ? $" {w}×{h}" : string.Empty));
            }
        }
        catch { }

        if (maxRefresh <= 1) maxRefresh = 60;
        if (ramGb <= 0) ramGb = 8;
        if (gpus.Count == 0) gpus.Add("Unknown GPU");
        if (monitorBits.Count == 0) monitorBits.Add($"Display @{maxRefresh}Hz");

        return new HardwareProfile(cpu, logical, ramGb, gpus, monitorBits.Distinct(StringComparer.OrdinalIgnoreCase).ToList(), maxRefresh, maxWidth, maxHeight);
    }

    private static OptimizationProfile BuildOptimizationProfile(HardwareProfile hw)
    {
        string gpu = string.Join(" ", hw.Gpus).ToUpperInvariant();
        bool high = Regex.IsMatch(gpu, @"RTX\s*(30|40|50)\d{2}|RX\s*(6|7|8|9)\d{3}|ARC\s*[AB]\d{3}");
        bool medium = high || Regex.IsMatch(gpu, @"RTX\s*20\d{2}|GTX\s*16\d{2}|RX\s*(5\d{3}|5\d{2})|VEGA|ARC\s*A\d{3}");
        if (hw.RamGb < 8 || hw.LogicalProcessors <= 4) { high = false; medium = false; }
        else if (hw.RamGb < 12 && high) { high = false; medium = true; }

        int hz = Math.Clamp(hw.MaxRefreshRate, 50, 500);
        int fps;
        if (high)
        {
            fps = hz >= 300 ? 360 : hz >= 240 ? 300 : hz >= 165 ? 240 : hz >= 120 ? 180 : 120;
        }
        else if (medium)
        {
            fps = hz >= 240 ? 240 : hz >= 144 ? 180 : 120;
        }
        else
        {
            fps = hz >= 120 ? 120 : 90;
        }

        int ui = high || medium ? Math.Min(120, fps) : Math.Min(90, fps);
        string tier = high ? "High" : medium ? "Balanced" : "Light";
        return new OptimizationProfile(fps, ui, tier);
    }

    private static string BuildHardwareSummary(HardwareProfile hw, OptimizationProfile profile)
    {
        string gpu = string.Join(" / ", hw.Gpus);
        string displays = string.Join("; ", hw.Displays.Take(3));
        return $"{hw.Cpu} • {hw.LogicalProcessors} потоков • {hw.RamGb:0.#} GB RAM\n{gpu}\n{displays} • профиль {profile.Tier} • целевой fps_max {profile.FpsMax}";
    }

    private static SignatureState InspectPhantomSignature(string path)
    {
        try
        {
            string text = File.ReadAllText(path);
            if (!text.Contains("PHANTOM-SIGNATURE-SHA256:", StringComparison.Ordinal)) return SignatureState.None;

            string? owner = Regex.Match(text, @"(?m)^// PHANTOM-OWNER:\s*(.+?)\s*$", RegexOptions.CultureInvariant).Groups[1].Value.Trim();
            string? id = Regex.Match(text, @"(?m)^// PHANTOM-ID:\s*([a-fA-F0-9]+)\s*$", RegexOptions.CultureInvariant).Groups[1].Value.Trim();
            string? signature = Regex.Match(text, @"(?m)^// PHANTOM-SIGNATURE-SHA256:\s*([a-fA-F0-9]{64})\s*$", RegexOptions.CultureInvariant).Groups[1].Value.Trim().ToLowerInvariant();
            const string begin = "// PHANTOM-PAYLOAD-BEGIN";
            const string end = "// PHANTOM-PAYLOAD-END";
            int a = text.IndexOf(begin, StringComparison.Ordinal);
            int b = text.IndexOf(end, StringComparison.Ordinal);
            if (string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(signature) || a < 0 || b <= a) return SignatureState.Invalid;
            a += begin.Length;
            if (a < text.Length && text[a] == '\r') a++;
            if (a < text.Length && text[a] == '\n') a++;
            string body = text[a..b];
            if (body.EndsWith("\r\n", StringComparison.Ordinal)) body = body[..^2] + "\n";
            else if (body.EndsWith("\n", StringComparison.Ordinal)) { }
            string calculated = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(owner + "\n" + id + "\n" + body))).ToLowerInvariant();
            return CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(calculated), Encoding.ASCII.GetBytes(signature)) ? SignatureState.Valid : SignatureState.Invalid;
        }
        catch
        {
            return SignatureState.Invalid;
        }
    }

    private static IEnumerable<KeyValuePair<string, string>> ReadVdfPairs(string path)
    {
        string text;
        try { text = File.ReadAllText(path); }
        catch { yield break; }
        var rx = new Regex("\\\"(?<k>[^\\\"]+)\\\"\\s*\\\"(?<v>[^\\\"]*)\\\"", RegexOptions.CultureInvariant);
        foreach (Match match in rx.Matches(text))
        {
            string key = UnescapeVdf(match.Groups["k"].Value);
            string value = UnescapeVdf(match.Groups["v"].Value);
            yield return new KeyValuePair<string, string>(key, value);
        }
    }

    private static string GetConvarCategory(string key)
    {
        if (key.StartsWith("cl_crosshair", StringComparison.OrdinalIgnoreCase)) return "Crosshair";
        if (key.StartsWith("viewmodel_", StringComparison.OrdinalIgnoreCase)) return "Viewmodel";
        if (key.StartsWith("snd_", StringComparison.OrdinalIgnoreCase) || key.StartsWith("voice_", StringComparison.OrdinalIgnoreCase) || key.Equals("volume", StringComparison.OrdinalIgnoreCase)) return "Audio";
        if (key.StartsWith("r_", StringComparison.OrdinalIgnoreCase) || key.StartsWith("mat_", StringComparison.OrdinalIgnoreCase)) return "Video";
        if (key.StartsWith("fps_", StringComparison.OrdinalIgnoreCase) || key.StartsWith("engine_", StringComparison.OrdinalIgnoreCase)) return "Performance";
        if (key.StartsWith("hud_", StringComparison.OrdinalIgnoreCase) || key.StartsWith("safezone", StringComparison.OrdinalIgnoreCase)) return "HUD";
        if (key.StartsWith("m_", StringComparison.OrdinalIgnoreCase) || key.Contains("sensitivity", StringComparison.OrdinalIgnoreCase)) return "Mouse";
        if (key.Equals("rate", StringComparison.OrdinalIgnoreCase) || key.StartsWith("cl_net", StringComparison.OrdinalIgnoreCase)) return "Network";
        return "CS2";
    }

    private static bool LooksLikeCfgCommandName(string key) => Regex.IsMatch(key, "^[A-Za-z_][A-Za-z0-9_.]*$", RegexOptions.CultureInvariant);

    private static bool LooksLikeBindKey(string key)
    {
        return Regex.IsMatch(key,
            "^(?:[A-Z0-9]|F(?:[1-9]|1[0-2])|MOUSE[1-9]|MWHEELUP|MWHEELDOWN|SPACE|SHIFT|CTRL|ALT|TAB|ENTER|ESCAPE|BACKSPACE|CAPSLOCK|UPARROW|DOWNARROW|LEFTARROW|RIGHTARROW|INS|DEL|HOME|END|PGUP|PGDN|SEMICOLON|APOSTROPHE|BACKQUOTE|COMMA|PERIOD|SLASH|MINUS|EQUALS)$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private static string EscapeCfg(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", " ").Replace("\n", " ");
    private static string UnescapeVdf(string value) => value.Replace("\\\"", "\"").Replace("\\\\", "\\");
    private static string SanitizeComment(string value) => value.Replace("\r", " ").Replace("\n", " ").Replace("//", "/ /").Trim();

    private static string SanitizeOwner(string? value)
    {
        string owner = Regex.Replace(value?.Trim() ?? string.Empty, @"[^\p{L}\p{N} ._\-@]+", "", RegexOptions.CultureInvariant).Trim();
        if (string.IsNullOrWhiteSpace(owner)) throw new InvalidOperationException("Введи имя пользователя для подписи CFG.");
        return owner.Length > 48 ? owner[..48] : owner;
    }

    private static string NormalizeCfgFileName(string? value)
    {
        string name = Path.GetFileName(value?.Trim() ?? string.Empty);
        if (string.IsNullOrWhiteSpace(name)) name = "MyConfig";
        foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
        if (!name.EndsWith(".cfg", StringComparison.OrdinalIgnoreCase)) name += ".cfg";
        if (name.Length > 100) name = name[..96] + ".cfg";
        return name;
    }

    private static string BuildLaunchOption(string fileName) => fileName.Contains(' ') ? $"+exec \"{fileName}\"" : $"+exec {fileName}";

    private static string? FindSteamUserCfgDirectory()
    {
        var candidates = new List<string>();
        foreach (string root in EnumerateSteamRoots())
        {
            string userdata = Path.Combine(root, "userdata");
            if (!Directory.Exists(userdata)) continue;
            foreach (string account in Directory.GetDirectories(userdata))
            {
                string cfg = Path.Combine(account, "730", "local", "cfg");
                if (Directory.Exists(cfg)) candidates.Add(cfg);
            }
        }
        return candidates.Distinct(StringComparer.OrdinalIgnoreCase).OrderByDescending(GetLatestCs2ConfigWriteTime).FirstOrDefault();
    }

    private static DateTime GetLatestCs2ConfigWriteTime(string cfg)
    {
        try
        {
            string[] files = Directory.GetFiles(cfg, "cs2_*.*");
            return files.Length == 0 ? Directory.GetLastWriteTimeUtc(cfg) : files.Max(File.GetLastWriteTimeUtc);
        }
        catch { return DateTime.MinValue; }
    }

    private static string? FindCs2CfgDirectory()
    {
        foreach (string library in EnumerateSteamLibraries())
        {
            string steamapps = Path.Combine(library, "steamapps");
            string manifest = Path.Combine(steamapps, "appmanifest_730.acf");
            string installDirName = "Counter-Strike Global Offensive";
            if (File.Exists(manifest))
            {
                try
                {
                    Match m = Regex.Match(File.ReadAllText(manifest), "\\\"installdir\\\"\\s*\\\"([^\\\"]+)\\\"", RegexOptions.IgnoreCase);
                    if (m.Success && !string.IsNullOrWhiteSpace(m.Groups[1].Value)) installDirName = m.Groups[1].Value;
                }
                catch { }
            }

            string gameRoot = Path.Combine(steamapps, "common", installDirName, "game", "csgo");
            string cfg = Path.Combine(gameRoot, "cfg");
            if (Directory.Exists(cfg) || Directory.Exists(gameRoot)) return cfg;
        }

        return null;
    }

    private static IEnumerable<string> EnumerateSteamLibraries()
    {
        var libraries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string root in EnumerateSteamRoots())
        {
            libraries.Add(root);
            string vdf = Path.Combine(root, "steamapps", "libraryfolders.vdf");
            if (!File.Exists(vdf)) continue;
            try
            {
                foreach (Match m in Regex.Matches(File.ReadAllText(vdf), "\\\"path\\\"\\s*\\\"([^\\\"]+)\\\"", RegexOptions.IgnoreCase))
                {
                    string path = m.Groups[1].Value.Replace("\\\\", "\\").Trim();
                    if (Directory.Exists(path)) libraries.Add(path);
                }
            }
            catch { }
        }

        foreach (DriveInfo drive in DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed))
        {
            foreach (string relative in new[] { "SteamLibrary", "Steam", "Games\\SteamLibrary", "Games\\Steam" })
            {
                string path = Path.Combine(drive.RootDirectory.FullName, relative);
                if (Directory.Exists(Path.Combine(path, "steamapps"))) libraries.Add(path);
            }
        }

        return libraries;
    }

    private static IEnumerable<string> EnumerateSteamRoots()
    {
        var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void Add(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            path = path.Trim().Trim('"').Replace('/', '\\');
            if (Directory.Exists(path)) roots.Add(path);
        }

        Add(ReadRegistrySteamPath());
        Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam"));
        Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Steam"));

        foreach (DriveInfo drive in DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed))
        {
            foreach (string relative in new[] { "Steam", "SteamLibrary", "Games\\Steam", "Games\\SteamLibrary", "Program Files (x86)\\Steam", "Program Files\\Steam" })
                Add(Path.Combine(drive.RootDirectory.FullName, relative));
        }

        return roots;
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
        {
            foreach (string valueName in new[] { "SteamPath", "InstallPath" })
            {
                if (Registry.GetValue(key, valueName, null) is string path && Directory.Exists(path)) return path;
            }
        }
        return null;
    }

    private enum SignatureState { None, Valid, Invalid }
    public enum SettingKind { Convar, Bind, Raw }

    public sealed class SettingItem
    {
        public bool Included { get; set; }
        public SettingKind Kind { get; set; }
        public string Category { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
        public string Source { get; set; }

        public SettingItem(bool included, SettingKind kind, string category, string name, string value, string source)
        {
            Included = included;
            Kind = kind;
            Category = category;
            Name = name;
            Value = value;
            Source = source;
        }
    }

    private sealed record HardwareProfile(string Cpu, int LogicalProcessors, double RamGb, List<string> Gpus, List<string> Displays, int MaxRefreshRate, int MaxWidth, int MaxHeight);
    private sealed record OptimizationProfile(int FpsMax, int FpsMaxUi, string Tier);
}
