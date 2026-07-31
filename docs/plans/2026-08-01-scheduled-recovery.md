# Scheduled Recovery Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Recover Codex Usage Companion automatically within one minute after Codex Desktop starts or the resident exits, without a permanently running watcher.

**Architecture:** Add a one-shot `--recover` command to the companion and invoke it from a stable PowerShell launcher installed under LocalAppData. A per-user scheduled task runs the launcher every minute; the launcher discovers the newest installed companion binary, while the existing mutex and refresh event prevent duplicates.

**Tech Stack:** .NET 8 WPF, C# 12, xUnit, PowerShell ScheduledTasks, GitHub Actions.

---

### Task 1: Add the one-shot recovery command

**Files:**
- Modify: `src/CodexUsageCompanion/Lifecycle/CommandMode.cs`
- Modify: `src/CodexUsageCompanion/Program.cs`
- Modify: `tests/CodexUsageCompanion.Tests/CommandModeTests.cs`

**Step 1: Write the failing test**

Add `--recover` to the supported-mode theory and expect `CommandMode.Recovery`.

**Step 2: Run the focused test and verify failure**

Run: `dotnet test CodexUsageCompanion.slnx --configuration Debug --filter "FullyQualifiedName~CommandModeTests"`

Expected: FAIL because `Recovery` does not exist.

**Step 3: Implement the minimal command**

Add `Recovery` to `CommandMode`, parse `--recover`, and route it from `Program.Main` to a handler with this behavior:

```csharp
private static int HandleRecovery()
{
    if (!new CodexWindowLocator().IsCodexRunning())
    {
        return 0;
    }

    var coordinator = new InstanceCoordinator();
    if (coordinator.SignalRefresh())
    {
        return 0;
    }

    CompanionLog.Shared.Write("recovery", "scheduled-launch");
    return DetachedLauncher.Start() ? 0 : 1;
}
```

**Step 4: Run the focused test and full Debug suite**

Run: `dotnet test CodexUsageCompanion.slnx --configuration Debug`

Expected: all tests pass.

**Step 5: Commit**

Run: `git add src/CodexUsageCompanion/Lifecycle/CommandMode.cs src/CodexUsageCompanion/Program.cs tests/CodexUsageCompanion.Tests/CommandModeTests.cs && git commit -m "Add scheduled recovery command"`

### Task 2: Add the stable launcher and scheduled-task management

**Files:**
- Create: `scripts/recovery-launcher.ps1`
- Create: `scripts/install-recovery.ps1`
- Create: `scripts/uninstall-recovery.ps1`
- Create: `scripts/test-recovery.ps1`

**Step 1: Write the failing script verification**

Create a temporary fake user profile, place multiple companion executables in source and versioned cache layouts, and assert that resolve-only mode selects the highest file version. Assert repeated installation keeps one task definition and uninstall removes it. Skip live task mutation when `-SkipTaskIntegration` is supplied.

**Step 2: Run it and verify failure**

Run: `pwsh -NoProfile -File scripts/test-recovery.ps1 -SkipTaskIntegration`

Expected: FAIL because the launcher scripts do not exist.

**Step 3: Implement the stable launcher**

The launcher accepts optional profile and LocalAppData roots plus `-ResolveOnly`. It reads `recovery.json`, checks the preferred executable and these patterns, removes duplicates, and chooses the highest valid file version:

```powershell
$patterns = @(
    (Join-Path $UserProfilePath 'plugins\codex-usage-companion\bin\win-x64\CodexUsageCompanion.exe'),
    (Join-Path $UserProfilePath '.codex\plugins\cache\*\codex-usage-companion\*\bin\win-x64\CodexUsageCompanion.exe')
)
```

In normal mode it runs the selected executable with `--recover`, waits for the one-shot command to finish, and records discovery or launch failures in a bounded recovery log.

**Step 4: Implement idempotent installation**

`install-recovery.ps1` copies the launcher to `%LOCALAPPDATA%\CodexUsageCompanion\Recovery`, writes the preferred executable path to `recovery.json`, and creates `\CodexUsageCompanion\Recovery` with:

- one-minute repetition;
- hidden, noninteractive PowerShell;
- `IgnoreNew` multiple-instance policy;
- battery operation allowed;
- one-minute execution limit;
- current-user registration without elevation.

It then starts the task once so installation takes effect without restarting Windows or Codex.

**Step 5: Implement removal**

`uninstall-recovery.ps1` unregisters only `\CodexUsageCompanion\Recovery` and removes only its stable recovery directory. It leaves settings and logs intact.

**Step 6: Run script verification**

Run: `pwsh -NoProfile -File scripts/test-recovery.ps1 -SkipTaskIntegration`

Expected: PASS.

**Step 7: Commit**

Run: `git add scripts/recovery-launcher.ps1 scripts/install-recovery.ps1 scripts/uninstall-recovery.ps1 scripts/test-recovery.ps1 && git commit -m "Add Windows scheduled recovery"`

### Task 3: Package recovery and bump version 0.3.4

**Files:**
- Modify: `src/CodexUsageCompanion/CodexUsageCompanion.csproj`
- Modify: `.codex-plugin/plugin.json`
- Modify: `packaging/marketplace.json`
- Modify: `scripts/build.ps1`
- Modify: `scripts/verify-package.ps1`

**Step 1: Make package verification require recovery scripts**

Add the launcher, installer, and uninstaller paths to `$required` before changing the build script.

**Step 2: Run the package build and verify failure**

Run: `pwsh -NoProfile -File scripts/build.ps1`

Expected: FAIL because recovery scripts are not yet copied into the package.

**Step 3: Copy scripts into the packaged plugin and update versions**

Create the packaged `scripts` directory, copy the three public recovery scripts, and change all active versions from `0.3.3` to `0.3.4`.

**Step 4: Build and verify**

Run: `pwsh -NoProfile -File scripts/build.ps1`

Expected: Release tests pass and the v0.3.4 ZIP plus SHA256 file are verified.

**Step 5: Commit**

Run: `git add src/CodexUsageCompanion/CodexUsageCompanion.csproj .codex-plugin/plugin.json packaging/marketplace.json scripts/build.ps1 scripts/verify-package.ps1 && git commit -m "Package scheduled recovery in v0.3.4"`

### Task 4: Update three-language documentation

**Files:**
- Modify: `README.md`
- Modify: `README.zh-Hant.md`
- Modify: `README.zh-Hans.md`

**Step 1: Document behavior and commands**

Explain one-minute recovery, zero persistent standby process, immediate activation, task name, installation command, removal command, and that hooks remain an immediate-refresh optimization.

**Step 2: Verify commands and links**

Run: `rg -n "install-recovery|uninstall-recovery|0\.3\.4|one minute|一分鐘|一分钟" README*.md`

Expected: all three languages include recovery guidance and the new version.

**Step 3: Commit**

Run: `git add README.md README.zh-Hant.md README.zh-Hans.md && git commit -m "Document automatic recovery"`

### Task 5: Install and verify locally

**Files:**
- Update installed plugin: `%USERPROFILE%\plugins\codex-usage-companion`
- Update Codex plugin cache: `%USERPROFILE%\.codex\plugins\cache\personal\codex-usage-companion\0.3.4`
- Create scheduled recovery files: `%LOCALAPPDATA%\CodexUsageCompanion\Recovery`

**Step 1: Run Debug and Release verification**

Run: `dotnet test CodexUsageCompanion.slnx --configuration Debug`

Run: `dotnet test CodexUsageCompanion.slnx --configuration Release -p:TreatWarningsAsErrors=true`

Expected: both suites pass.

**Step 2: Install the packaged v0.3.4 plugin and recovery task**

Copy only the verified packaged plugin contents into the personal source and v0.3.4 cache, then run the packaged `install-recovery.ps1`.

**Step 3: Verify live recovery**

Confirm the task exists, no duplicate resident is created, the task returns success while Codex is running, and the log contains `scheduled-launch` after a controlled resident stop. Do not stop unrelated Electron processes.

**Step 4: Verify resource behavior**

Confirm the recovery launcher leaves no PowerShell process after each run and therefore has zero persistent standby memory.

### Task 6: Publish and integrate

**Files:**
- Git branch and GitHub release metadata

**Step 1: Inspect the final diff and repository status**

Run: `git status --short && git diff origin/main...HEAD --stat`

Expected: only scheduled-recovery implementation, tests, package metadata, and three-language documentation differ.

**Step 2: Push, open a PR, and merge after CI passes**

Push the implementation branch, create a pull request, wait for required checks, and merge it into `main`.

**Step 3: Tag and publish v0.3.4**

Create tag `v0.3.4` from merged `main` and publish the verified ZIP and SHA256 artifact.

**Step 4: Final verification**

Confirm `origin/main`, local `main`, release tag, installed source, installed cache, and running executable all correspond to v0.3.4.
