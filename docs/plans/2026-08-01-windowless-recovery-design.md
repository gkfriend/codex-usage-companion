# Windowless Recovery Design

## Problem

The scheduled recovery task currently launches `powershell.exe` directly every minute. Windows can briefly create a console window before PowerShell applies its hidden-window option, producing the visible flash reported by the user.

## Selected Design

The scheduled task will launch a Windows Script Host wrapper through `wscript.exe`. The wrapper starts the existing PowerShell recovery launcher with window style `0`, waits for it to finish, and returns its exit code. Because `wscript.exe` is a GUI-subsystem host, the recovery check does not create a console window.

The task repetition interval will change from one minute to three minutes. The existing `IgnoreNew` policy remains in place so overlapping runs cannot accumulate.

## Runtime Flow

1. Windows Task Scheduler starts `wscript.exe` every three minutes.
2. The wrapper launches the stable recovery PowerShell script without a visible window.
3. The PowerShell script finds the newest installed companion executable and invokes `--recover`.
4. The companion starts only when Codex is running and no companion process is already active.

## Installation and Updates

The installer copies both launcher files to `%LOCALAPPDATA%\CodexUsageCompanion\Recovery` and recreates the scheduled task with the new action and interval. Package verification requires both files so incomplete releases fail validation.

## Error Handling and Logging

The PowerShell launcher continues writing recovery failures to the existing local log. The wrapper propagates the PowerShell exit code to Task Scheduler. Missing launcher files fail installation before the task is registered.

## Alternatives Considered

- Running PowerShell directly keeps the current flash risk.
- Scheduling a copied companion executable avoids PowerShell but can leave an outdated executable after upgrades.
- Using a non-interactive service account complicates access to the signed-in user's Codex session and files.

## Acceptance Criteria

- The task action uses `wscript.exe`, not `powershell.exe`.
- The repetition interval is `PT3M`.
- Manual and scheduled recovery create no visible console window.
- Recovery still restores exactly one companion process.
- Tests, package verification, documentation, and all three language variants are updated for version 0.3.5.
