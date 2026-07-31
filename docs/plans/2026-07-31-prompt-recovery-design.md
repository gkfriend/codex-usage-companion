# Prompt Recovery Design

## Problem

Codex Desktop can restart after an update without running the plugin's `SessionStart` hook. The resident process exits when the old Codex process disappears, so the overlay remains absent until a later `Stop` hook runs.

## Approaches

1. Add a `UserPromptSubmit` hook that invokes the existing `--session-start` recovery path.
2. Install a Windows watchdog that monitors Codex independently of hooks.
3. Register the companion as a Windows startup application.

## Decision

Use `UserPromptSubmit`. It stays inside the plugin model, does not create another always-running launcher, and recovers on the first user interaction even when Codex skips `SessionStart`.

## Behavior

- `SessionStart` remains the primary startup path.
- `UserPromptSubmit` invokes `--session-start`.
- If the resident exists, the command signals an immediate refresh and exits.
- If the resident does not exist, the command launches one detached background resident.
- The resident mutex continues to prevent duplicates.
- `Stop` continues to refresh after every AI response and can also recover a missing resident.

## Validation

- Package tests verify all three hook events and commands.
- Existing command and resident-lifetime tests remain green.
- Package validation confirms the published plugin contains the expected manifest and version.
- A local installed copy is upgraded and verified with exactly one responsive resident process.
