using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace PhantomInstaller;

public partial class MainWindow
{
    private static readonly ProPreset[] ProPresets =
    {
        MakePreset(
            "donk", "donk", "Rifler", 800, 1.25, 1.00, "1280×960", "4:3", "Stretched", "600", "2026-09-03",
            "https://prosettings.net/players/donk/",
            1, 1, -4, false, 1, "green", true, 255, 0, 255, 0,
            68, 2.5, 0, -1.5, 2),

        MakePreset(
            "m0NESY", "m0nesy", "AWPer", 400, 2.30, 1.00, "1280×960", "4:3", "Stretched", "999", "2026-09-01",
            "https://prosettings.net/players/m0nesy/",
            1, 1, -4, false, 1, "cyan", false, 255, 0, 255, 0,
            68, 2.5, 0, -1.5, 3),

        MakePreset(
            "ZywOo", "zywoo", "AWPer", 400, 1.90, 1.00, "1280×960", "4:3", "Stretched", "400", "2026-09-01",
            "https://prosettings.net/players/zywoo/",
            2, 0, -3, false, 1, "green", false, 255, 0, 255, 0,
            68, 2.5, 0, -1.5, 1),

        MakePreset(
            "s1mple", "s1mple", "AWPer", 400, 3.09, 1.00, "1280×960", "4:3", "Stretched", "0 / launch 999", "2026-08-25",
            "https://prosettings.net/players/s1mple/",
            1, 1, -4, false, 1, "cyan", false, 200, 0, 255, 0,
            68, 2.5, 0, -1.5, 2),

        MakePreset(
            "NiKo", "niko", "Rifler", 800, 0.90, 0.90, "1280×960", "4:3", "Stretched", "400", "2026-09-01",
            "https://prosettings.net/players/niko/",
            1.5, 0, -4, false, 0, "custom", false, 255, 0, 255, 145,
            68, 2.5, 0, -1.5, 2),

        MakePreset(
            "ropz", "ropz", "Rifler", 400, 1.77, 1.00, "1920×1080", "16:9", "Native", "999", "2026-09-01",
            "https://prosettings.net/players/ropz/",
            2, 0.5, -3, false, 0, "green", false, 255, 0, 255, 0,
            68, 2.5, 0, -1.5, 2),

        MakePreset(
            "XANTARES", "xantares", "Rifler", 400, 2.30, 1.10, "1024×768", "4:3", "Stretched", "600", "2026-09-01",
            "https://prosettings.net/players/xantares/",
            3, 0.5, 0, false, 1, "green", false, 200, 50, 250, 50,
            60, 1, 1, -1, 1),

        MakePreset(
            "device", "device", "AWPer", 800, 0.95, 1.00, "1280×960", "4:3", "Stretched", "960", "2026-08-25",
            "https://prosettings.net/players/dev1ce/",
            1, 0, -4, false, 1, "custom", false, 255, 255, 255, 255,
            68, 2.5, 2, -2, 2),

        MakePreset(
            "sh1ro", "sh1ro", "AWPer", 800, 1.04, 1.00, "1280×960", "4:3", "Stretched", "500", "2026-08-31",
            "https://prosettings.net/players/sh1ro/",
            1, 1, -4, false, 1, "green", false, 255, 255, 255, 255,
            68, 2.5, 0, -1.5, 1),

        MakePreset(
            "b1t", "b1t", "Rifler", 800, 0.825, 1.00, "1280×960", "4:3", "Stretched", "400", "2026-08-30",
            "https://prosettings.net/players/b1t/",
            1, 1, -4, false, 1, "green", false, 255, 0, 0, 0,
            68, 2.5, 0, -1.5, 1)
    };

    private static ProPreset MakePreset(
        string name, string slug, string role, int dpi, double sensitivity, double zoomSensitivity,
        string resolution, string aspectRatio, string scalingMode, string maxFps, string sourceUpdated, string sourceUrl,
        double crosshairSize, double crosshairThickness, double crosshairGap, bool outline, double outlineThickness,
        string color, bool alphaDisabled, int alpha, int red, int green, int blue,
        double viewmodelFov, double viewmodelX, double viewmodelY, double viewmodelZ, int publishedPresetPos)
    {
        int colorId = color.Equals("cyan", StringComparison.OrdinalIgnoreCase) ? 4
            : color.Equals("custom", StringComparison.OrdinalIgnoreCase) ? 5
            : 1;

        var commands = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["sensitivity"] = F(sensitivity),
            ["zoom_sensitivity_ratio"] = F(zoomSensitivity),
            ["cl_crosshairstyle"] = "4",
            ["cl_crosshair_recoil"] = "0",
            ["cl_crosshairdot"] = "0",
            ["cl_crosshairsize"] = F(crosshairSize),
            ["cl_crosshairthickness"] = F(crosshairThickness),
            ["cl_crosshairgap"] = F(crosshairGap),
            ["cl_crosshair_drawoutline"] = outline ? "1" : "0",
            ["cl_crosshair_outlinethickness"] = F(outlineThickness),
            ["cl_crosshaircolor"] = colorId.ToString(CultureInfo.InvariantCulture),
            ["cl_crosshaircolor_r"] = red.ToString(CultureInfo.InvariantCulture),
            ["cl_crosshaircolor_g"] = green.ToString(CultureInfo.InvariantCulture),
            ["cl_crosshaircolor_b"] = blue.ToString(CultureInfo.InvariantCulture),
            ["cl_crosshairusealpha"] = alphaDisabled ? "0" : "1",
            ["cl_crosshairalpha"] = alpha.ToString(CultureInfo.InvariantCulture),
            ["cl_crosshair_t"] = "0",
            ["cl_crosshairgap_useweaponvalue"] = "0",
            ["cl_crosshair_sniper_width"] = "0",
            // Use custom mode so the published numeric offsets are not overwritten by a preset position.
            ["viewmodel_presetpos"] = "0",
            ["viewmodel_fov"] = F(viewmodelFov),
            ["viewmodel_offset_x"] = F(viewmodelX),
            ["viewmodel_offset_y"] = F(viewmodelY),
            ["viewmodel_offset_z"] = F(viewmodelZ)
        };

        double edpi = dpi * sensitivity;
        return new ProPreset(name, slug, role, dpi, sensitivity, edpi, zoomSensitivity, resolution, aspectRatio,
            scalingMode, maxFps, sourceUpdated, sourceUrl, publishedPresetPos, commands);
    }

    private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    private void OpenProPresetsButton_Click(object sender, RoutedEventArgs e)
    {
        if (ProPresetCombo.ItemsSource is null)
        {
            ProPresetCombo.ItemsSource = ProPresets;
            ProPresetCombo.DisplayMemberPath = nameof(ProPreset.Name);
            ProPresetCombo.SelectedIndex = 0;
        }
        UpdateProPresetPreview();
        SwapPanels(IntroPanel, ProPresetsPanel);
    }

    private void BackFromProPresets_Click(object sender, RoutedEventArgs e) => SwapPanels(ProPresetsPanel, IntroPanel);

    private void ProPresetSelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateProPresetPreview();

    private ProPreset? SelectedProPreset() => ProPresetCombo.SelectedItem as ProPreset;

    private void UpdateProPresetPreview()
    {
        ProPreset? preset = SelectedProPreset();
        if (preset is null) return;

        ProPresetInfoText.Text =
            $"{preset.Name} • {preset.Role}\n" +
            $"DPI {preset.Dpi} • sensitivity {F(preset.Sensitivity)} • eDPI {F(preset.Edpi)} • zoom {F(preset.ZoomSensitivity)}\n" +
            $"Видео: {preset.Resolution} • {preset.AspectRatio} • {preset.ScalingMode} • опубликованный max FPS: {preset.MaxFps}\n" +
            $"Viewmodel presetpos {preset.PublishedPresetPos} (в CFG сохраняются его точные offsets в custom mode).";

        ProPresetCommandsText.Text =
            "Применяются CFG-совместимые параметры: sensitivity, zoom, прицел и viewmodel. " +
            "DPI, Windows mouse speed, разрешение и графические параметры показаны как справка — Phantom не меняет их вслепую на чужом ПК.";

        ProPresetSourceText.Text = $"Источник: ProSettings.net • обновлено {preset.SourceUpdated} • {preset.SourceUrl}";
    }

    private async void ApplyProPresetToBuilder_Click(object sender, RoutedEventArgs e)
    {
        ProPreset? preset = SelectedProPreset();
        if (preset is null) return;

        // Keep the user's existing binds/settings when they are available, then overlay the selected pro preset.
        if (_allSettings.Count == 0)
        {
            try { await LoadSettingsAsync(force: true); }
            catch { /* A preset can still be used without readable userdata. */ }
        }

        foreach ((string command, string value) in preset.Commands)
        {
            SettingItem? existing = _allSettings.FirstOrDefault(x =>
                x.Kind == SettingKind.Convar && x.Name.Equals(command, StringComparison.OrdinalIgnoreCase));
            string category = GetConvarCategory(command);
            if (existing is null)
            {
                _allSettings.Add(new SettingItem(true, SettingKind.Convar, category, command, value, $"Pro preset: {preset.Name}"));
            }
            else
            {
                existing.Value = value;
                existing.Included = true;
                existing.Source = $"Pro preset: {preset.Name}";
            }
        }

        ConfigNameBox.Text = $"Phantom-{preset.Slug}";
        HardwareSummaryText.Text = $"Применён preset {preset.Name}. DPI {preset.Dpi}, eDPI {F(preset.Edpi)}, {preset.Resolution} {preset.AspectRatio} {preset.ScalingMode} — справочно.";
        SettingsCountText.Text = $"Найдено: {_allSettings.Count} • preset {preset.Name}";
        SettingsGrid.Items.Refresh();
        ApplySettingsFilter();
        SwapPanels(ProPresetsPanel, SettingsPanel);
    }

    private async void InstallProPreset_Click(object sender, RoutedEventArgs e)
    {
        ProPreset? preset = SelectedProPreset();
        if (preset is null) return;

        string owner;
        try { owner = SanitizeOwner(SignatureNameBox.Text); }
        catch { owner = Environment.UserName; }

        string fileName = $"Phantom-{preset.Slug}.cfg";
        string text = BuildSignedPresetCfg(preset, owner);

        SwapPanels(ProPresetsPanel, InstallPanel);
        StartInstallArtworkAnimation();
        InstallTitle.Text = $"УСТАНАВЛИВАЮ {preset.Name.ToUpperInvariant()} PRESET";

        try
        {
            string downloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            Directory.CreateDirectory(downloads);
            string downloadPath = Path.Combine(downloads, fileName);
            InstallStatus.Text = "Сохраняю подписанный preset в Загрузки…";
            if (File.Exists(downloadPath))
                File.Copy(downloadPath, downloadPath + $".backup-{DateTime.Now:yyyyMMdd-HHmmss}", false);
            await File.WriteAllTextAsync(downloadPath, text, new UTF8Encoding(false));

            string? cfgDirectory = await Task.Run(FindCs2CfgDirectory);
            if (cfgDirectory is null)
                throw new DirectoryNotFoundException("Counter-Strike 2 не найден ни в одной Steam-библиотеке.");

            Directory.CreateDirectory(cfgDirectory);
            string destination = Path.Combine(cfgDirectory, fileName);
            InstallStatus.Text = "Устанавливаю preset в Counter-Strike 2…";
            if (File.Exists(destination))
                File.Copy(destination, destination + $".backup-{DateTime.Now:yyyyMMdd-HHmmss}", false);
            File.Copy(downloadPath, destination, true);

            _launchOption = BuildLaunchOption(fileName);
            await Task.Delay(250);
            StopInstallArtworkAnimation();
            ShowDoneFrom(InstallPanel, "ГОТОВО", $"{preset.Name} preset сохранён в Загрузки и установлен", downloadPath, _launchOption);
        }
        catch (Exception ex)
        {
            StopInstallArtworkAnimation();
            ShowErrorFrom(InstallPanel, "Preset не установлен: " + ex.Message);
        }
    }

    private static string BuildSignedPresetCfg(ProPreset preset, string owner)
    {
        string id = Guid.NewGuid().ToString("N");
        var payload = new StringBuilder();
        payload.AppendLine($"// PHANTOM PRO PRESET • {preset.Name}");
        payload.AppendLine($"// Source snapshot: {preset.SourceUrl} • updated {preset.SourceUpdated}");
        payload.AppendLine($"// Reference only: DPI {preset.Dpi} • eDPI {F(preset.Edpi)} • {preset.Resolution} {preset.AspectRatio} {preset.ScalingMode}");
        payload.AppendLine("// DPI/resolution/video reference comments do not change CS2 behavior.");
        payload.AppendLine();
        foreach ((string command, string value) in preset.Commands.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
            payload.AppendLine($"{command} \"{EscapeCfg(value)}\"");
        payload.AppendLine();
        payload.AppendLine($"echo \"Phantom {EscapeCfg(preset.Name)} preset loaded\"");

        string body = payload.ToString();
        string signature = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(owner + "\n" + id + "\n" + body))).ToLowerInvariant();

        var sb = new StringBuilder();
        sb.AppendLine("// ============================================================================");
        sb.AppendLine("// PHANTOM CFG • PRO PRESET • GENERATED LOCALLY");
        sb.AppendLine($"// PHANTOM-OWNER: {owner}");
        sb.AppendLine($"// PHANTOM-ID: {id}");
        sb.AppendLine($"// PHANTOM-GENERATED: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"// PHANTOM-SIGNATURE-SHA256: {signature}");
        sb.AppendLine("// PHANTOM-PAYLOAD-BEGIN");
        sb.Append(body);
        if (!body.EndsWith('\n')) sb.AppendLine();
        sb.AppendLine("// PHANTOM-PAYLOAD-END");
        sb.AppendLine($"// PHANTOM-SIGNATURE-SHA256: {signature}");
        sb.AppendLine($"// by Phantom • {owner}");
        sb.AppendLine($"// PHANTOM WATERMARK • {owner} • {id}");
        return sb.ToString();
    }

    private sealed record ProPreset(
        string Name,
        string Slug,
        string Role,
        int Dpi,
        double Sensitivity,
        double Edpi,
        double ZoomSensitivity,
        string Resolution,
        string AspectRatio,
        string ScalingMode,
        string MaxFps,
        string SourceUpdated,
        string SourceUrl,
        int PublishedPresetPos,
        IReadOnlyDictionary<string, string> Commands);
}
