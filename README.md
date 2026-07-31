# Codex Usage Companion

[繁體中文](README.zh-Hant.md) · [简体中文](README.zh-Hans.md)

Codex Usage Companion is an open-source Windows plugin that keeps Codex rate-limit usage visible in a compact panel attached to Codex Desktop.

## Preview

| English | 繁體中文 | 简体中文 |
| --- | --- | --- |
| ![English usage overlay](assets/screenshots/overlay-en.png) | ![Traditional Chinese usage overlay](assets/screenshots/overlay-zh-Hant.png) | ![Simplified Chinese usage overlay](assets/screenshots/overlay-zh-Hans.png) |

## Features

- Updates when a prompt is submitted and after Codex responses, with live local notifications and a one-minute fallback refresh.
- Starting with v0.3.4, an independent Windows scheduled check restores the companion within one minute after Codex restarts or the resident exits, even when Codex Hooks do not run.
- Uses no permanent watcher while Codex is closed; the scheduled check exits immediately after each run.
- Keeps one resident companion process and never opens a separate taskbar, Alt+Tab, or tray entry.
- Follows the Codex window, hides when Codex is minimized, and exits after Codex closes.
- Shows a five-cell HP bar with green, yellow, orange, red, and empty gray states.
- Supports English, Traditional Chinese, and Simplified Chinese.
- Keeps the five-hour card implemented but hidden by default.
- Reads the local Codex app-server without storing authentication tokens.
- Includes no telemetry or external service.

## Requirements

- Windows 10 or later, x64
- Codex Desktop

The Release build is self-contained and does not require a separate .NET runtime.

## Install from GitHub

> [!TIP]
> **Don’t want to read the full instructions?**
>
> Paste `https://github.com/gkfriend/codex-usage-companion` into Codex and say:
>
> “Please read this project’s installation instructions, then install and enable Codex Usage Companion for me.”
>
> Codex can handle most of the installation. If a permission request or `/hooks` trust confirmation appears, review and approve it to continue.

Run:

```powershell
codex plugin marketplace add gkfriend/codex-usage-companion
```

Open the Codex Plugins directory, select **Codex Usage Companion**, review and trust the three bundled Hooks (`SessionStart`, `UserPromptSubmit`, and `Stop`), then install and enable it. Start a new Codex task after installation. After every upgrade, open `/hooks` and trust the current definitions again if Codex asks for confirmation.

Alternatively, download the marketplace ZIP from GitHub Releases, extract it, and add the extracted folder with `codex plugin marketplace add <folder>`.

## Automatic recovery

From the installed Codex Usage Companion plugin directory, run:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\install-recovery.ps1
```

This creates the per-user hidden task `\CodexUsageCompanion\Recovery`, starts it immediately, and checks once per minute. It requires no administrator permission and leaves no permanent PowerShell process. Existing Hooks remain enabled for immediate startup and refresh.

To remove only automatic recovery while keeping the plugin, settings, and logs:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\uninstall-recovery.ps1
```

## Settings

The plugin creates `settings.json` in its Codex plugin data directory. If that directory is unavailable, it uses `%LOCALAPPDATA%\CodexUsageCompanion\settings.json`.

```json
{
  "showFiveHourLimit": false,
  "language": "auto",
  "position": "bottom-right",
  "opacity": 1.0,
  "margin": 16
}
```

Language values: `auto`, `en`, `zh-Hant`, `zh-Hans`.

Position values: `top-left`, `top-right`, `bottom-left`, `bottom-right`.

Opacity is limited to `0.5`–`1.0`; margin is limited to `0`–`64` pixels.

## Build

Install the .NET 8 SDK, then run:

```powershell
pwsh -File scripts/build.ps1
```

The script tests the Release configuration, creates a self-contained single-file `win-x64` executable, validates the marketplace ZIP, and writes the ZIP plus SHA-256 checksum under `artifacts`.

## Compatibility

The plugin uses Codex's experimental local app-server rate-limit API. A future Codex update may require a compatibility update. Temporary failures retain the last valid usage state and are written only to bounded local diagnostic logs.

## Privacy and license

See [PRIVACY.md](PRIVACY.md), [SECURITY.md](SECURITY.md), and the [MIT License](LICENSE).
