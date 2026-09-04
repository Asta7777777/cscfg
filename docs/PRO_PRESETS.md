# Phantom Pro Presets

The preset library contains factual snapshots of publicly listed CS2 settings. It is intentionally limited to CFG-compatible values that Phantom can apply safely: sensitivity, zoom sensitivity, crosshair and viewmodel. DPI, Windows mouse speed, resolution, scaling and published FPS caps are shown as reference only and are not forced on another PC.

Research snapshot: 2026-09-04.

| Player | Source updated | Source |
|---|---:|---|
| donk | 2026-09-03 | https://prosettings.net/players/donk/ |
| m0NESY | 2026-09-01 | https://prosettings.net/players/m0nesy/ |
| ZywOo | 2026-09-01 | https://prosettings.net/players/zywoo/ |
| s1mple | 2026-08-25 | https://prosettings.net/players/s1mple/ |
| NiKo | 2026-09-01 | https://prosettings.net/players/niko/ |
| ropz | 2026-09-01 | https://prosettings.net/players/ropz/ |
| XANTARES | 2026-09-01 | https://prosettings.net/players/xantares/ |
| device | 2026-08-25 | https://prosettings.net/players/dev1ce/ |
| sh1ro | 2026-08-31 | https://prosettings.net/players/sh1ro/ |
| b1t | 2026-08-30 | https://prosettings.net/players/b1t/ |

## Behavior

- **Apply in CFG Builder** preserves the user's existing local settings/binds when available and overlays the selected player's CFG-compatible preset values.
- **Downloads + Install** creates a standalone `Phantom-<player>.cfg`, adds the same Phantom owner/watermark/SHA-256 signature format, saves it to Downloads and installs it into the detected CS2 cfg directory.
- The finished screen generates the matching `+exec Phantom-<player>.cfg` command automatically.
- Pro settings can change at any time; the app displays the source update date so the snapshot is not presented as permanent or official affiliation.
