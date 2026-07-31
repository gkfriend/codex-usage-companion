# Scheduled Recovery Design

## Problem

Codex Usage Companion exits after Codex Desktop is absent for 30 seconds. Codex lifecycle hooks normally start it again, but Codex Desktop can discover and trust those hooks without dispatching them after an ordinary restart. This leaves the companion stopped until it is launched manually.

## Selected approach

Install a per-user Windows scheduled task that runs a short recovery check every minute. The check exits immediately unless Codex Desktop is running and the companion resident is absent. When both conditions are met, it launches the companion through a stable bootstrap path.

Existing hooks remain enabled for immediate startup and refresh. The scheduled task is an independent recovery layer rather than the primary refresh mechanism.

## Alternatives considered

- A permanently running watcher would recover faster but would consume memory while Codex is closed.
- Hook-only recovery has no additional Windows integration but has already failed after both updates and ordinary restarts.
- A Windows service would be durable but would require elevated installation and add unnecessary complexity.

## Components

- A recovery command performs one check and returns a success code without opening a window.
- A stable launcher stored under the user's local application data locates the currently installed companion executable instead of depending on a versioned plugin cache path.
- Installer commands create or update the scheduled task and stable launcher.
- Uninstaller commands remove the scheduled task and stable launcher without deleting user logs or settings.
- The existing resident mutex remains the final duplicate-instance guard.

## Runtime flow

1. Windows Task Scheduler invokes the stable launcher once per minute.
2. The launcher locates the newest enabled installation of Codex Usage Companion.
3. The recovery command checks for a real Codex Desktop root process.
4. If Codex is absent, it exits without starting the companion.
5. If a resident already exists, it exits without starting another process.
6. Otherwise it starts the resident in the background and records the recovery event.
7. The launcher process exits, leaving no permanent standby process.

## Reliability and safety

- Installation is per-user and requires no administrator privileges.
- All launches are hidden and create no taskbar window.
- Process detection excludes unrelated Electron applications and only recognizes Codex Desktop executables and package paths.
- The stable launcher tolerates plugin version directory changes and prefers the newest valid executable.
- The scheduled task is idempotent, so reinstalling or updating replaces its configuration safely.
- Failures are written to the existing rotating companion log.

## Verification

- Unit tests cover Codex process classification, executable discovery, duplicate suppression, and recovery decisions.
- Script-level verification checks task creation, repeated installation, stable-path discovery, and removal.
- Integration verification covers Codex running with no resident, Codex running with one resident, Codex absent, companion exit, and recovery within one minute.
- Existing Debug and Release test suites must remain green before publishing.

## Acceptance criteria

- No persistent recovery process exists while Codex is closed.
- Recovery occurs within 70 seconds after Codex starts or the resident unexpectedly exits.
- At most one companion resident runs.
- Codex updates and plugin version changes do not require manual repair.
- Installation, update, and removal instructions are available in English, Traditional Chinese, and Simplified Chinese.
