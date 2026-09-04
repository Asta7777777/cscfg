# Phantom Installer

**CS2 CFG installer, builder, pro preset library and local hardware optimizer for Windows.**

[Download the latest PhantomInstaller.exe](https://github.com/Alexg7372/cscfg/releases/latest/download/PhantomInstaller.exe)

One standalone EXE. No `assets` folder, DLLs or separate runtime are required next to it.

## Features

### Built-in Phantom.cfg

Installs the bundled `Phantom.cfg` directly into the detected Counter-Strike 2 `game\csgo\cfg` directory. If a file with the same name already exists, Phantom Installer creates a timestamped backup first.

### Install any custom CFG

Choose any `.cfg` file. The installer finds CS2, copies the file into the correct CFG directory and generates the exact Steam launch option from the real filename.

Examples:

- `MyConfig.cfg` → `+exec MyConfig.cfg`
- `My Cool Config.cfg` → `+exec "My Cool Config.cfg"`

### Pro Presets

Phantom Installer v1.2.0 includes offline preset snapshots for:

- donk
- m0NESY
- ZywOo
- s1mple
- NiKo
- ropz
- XANTARES
- device
- sh1ro
- b1t

Each preset contains publicly listed CFG-compatible values for mouse sensitivity, zoom sensitivity, crosshair and viewmodel. The app also displays reference values such as DPI/eDPI, resolution, aspect ratio, scaling mode and the published FPS cap.

Reference-only hardware/display values are deliberately not forced onto another PC. This avoids changing a user's Windows mouse setup or applying a pro player's resolution blindly.

A preset can be:

1. applied on top of the CFG Builder while keeping the user's other local settings and binds;
2. signed and saved as `Phantom-<player>.cfg` to Downloads;
3. installed into CS2 immediately;
4. launched with the exact generated `+exec Phantom-<player>.cfg` command.

Preset source snapshots and update dates are documented in [`docs/PRO_PRESETS.md`](docs/PRO_PRESETS.md). Pro settings can change, so the application presents them as dated snapshots rather than permanent or officially endorsed configurations.

### CFG Builder / Settings editor

Phantom Installer reads local CS2 files from Steam `userdata/<account>/730/local/cfg` and displays discovered settings inside the app:

- user convars;
- binds;
- machine values;
- video values;
- other discovered CS2 VCFG/TXT key-value settings.

Executable convars and binds can be edited and included in the generated CFG. Machine/video values that are not safe console commands are preserved as comments so the generated file can still contain a local snapshot without sending invalid commands to CS2.

### Hardware optimization

The optimizer runs locally and inspects available Windows hardware information:

- CPU;
- logical processor count;
- physical RAM;
- GPU adapters;
- active display resolution / refresh-rate information when Windows exposes it.

It then applies a conservative CS2 profile to the builder, including an FPS target matched to the detected hardware/display tier plus safe performance/network/audio values. It does not upload hardware data anywhere.

### Build + install in one click

Enter a CFG name and a user/signature name, then choose **Downloads + Install**. Phantom Installer:

1. builds the CFG from the current editor values;
2. saves a copy to the user's `Downloads` folder;
3. finds CS2 even when Steam or the game is on another drive or custom Steam library;
4. installs the same CFG into CS2;
5. creates a backup before replacing an existing file;
6. shows the exact `+exec` command with a one-click copy button.

### Phantom signature / watermark

Generated CFG files contain comment-only Phantom metadata:

- owner/user name;
- unique CFG ID;
- generated timestamp;
- SHA-256 payload signature;
- repeated Phantom watermark comments;
- final `by Phantom • <user>` marker.

These lines are comments and do not change CS2 behavior. A Phantom-signed CFG that is later modified can be detected when it is loaded again through the custom CFG installer.

## Steam / CS2 discovery

The installer checks the Steam registry path, standard Steam locations on available fixed drives, common `SteamLibrary` locations, and `libraryfolders.vdf`. If `appmanifest_730.acf` is present, its actual CS2 `installdir` value is used instead of assuming a fixed game path.

This means CS2 can be on `C:`, `D:`, another fixed drive, or a custom Steam library.

## Privacy

Phantom Installer is local-only. It does not send CS2 settings, CFG contents or detected hardware information to a server or cloud API. Pro preset snapshots are built into the executable, so selecting them does not require an internet connection.

## Requirements

- Windows 10/11 x64
- Steam + Counter-Strike 2 for installation/build-from-current-settings features

The release is self-contained with the .NET runtime bundled into the EXE.

## Build from source

```powershell
dotnet restore installer/PhantomInstaller.csproj
dotnet publish installer/PhantomInstaller.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish
```

GitHub Actions builds the standalone EXE and publishes `PhantomInstaller.exe` as the asset of release `v1.2.0`.

## Security note

The current release requests administrator privileges because CS2/Steam may be installed in protected directories. The EXE is not Authenticode-signed with a commercial Windows code-signing certificate, so SmartScreen may show a warning on some systems.

## License

MIT. See `LICENSE`.
