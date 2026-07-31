# Prompt Recovery Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Recover the usage companion on the first submitted prompt when a Codex update skips `SessionStart`.

**Architecture:** Extend the plugin hook manifest with `UserPromptSubmit` and reuse the existing idempotent `--session-start` command. Keep `Stop` as the post-response refresh and recovery path, with the resident mutex enforcing a single instance.

**Tech Stack:** .NET 8, C#, xUnit, Codex plugin hooks, PowerShell packaging scripts

---

### Task 1: Add the failing hook contract test

**Files:**
- Modify: `tests/CodexUsageCompanion.Tests/PackageLayoutTests.cs`

**Step 1: Write the failing test**

Require `hooks.json` to contain a `UserPromptSubmit` command that invokes `${PLUGIN_ROOT}/bin/win-x64/CodexUsageCompanion.exe` with `--session-start`.

**Step 2: Run the focused test**

Run: `dotnet test tests/CodexUsageCompanion.Tests/CodexUsageCompanion.Tests.csproj --filter PackageLayoutTests`

Expected: FAIL because `UserPromptSubmit` is absent.

### Task 2: Add prompt recovery

**Files:**
- Modify: `hooks/hooks.json`

**Step 1: Implement the minimal manifest change**

Add:

```json
"UserPromptSubmit": [
  {
    "hooks": [
      {
        "type": "command",
        "command": "\"${PLUGIN_ROOT}/bin/win-x64/CodexUsageCompanion.exe\" --session-start",
        "timeout": 10
      }
    ]
  }
]
```

**Step 2: Run the focused test**

Run: `dotnet test tests/CodexUsageCompanion.Tests/CodexUsageCompanion.Tests.csproj --filter PackageLayoutTests`

Expected: PASS.

### Task 3: Version and document the release

**Files:**
- Modify: `.codex-plugin/plugin.json`
- Modify: `marketplace.json`
- Modify: `src/CodexUsageCompanion/CodexUsageCompanion.csproj`
- Modify: `README.md`
- Modify: `README.zh-Hant.md`
- Modify: `README.zh-Hans.md`
- Modify: `CHANGELOG.md`

**Step 1: Bump the version**

Set the plugin and application version to `0.3.3`.

**Step 2: Document recovery behavior**

Explain that the plugin recovers on session start, prompt submission, and response completion.

### Task 4: Verify, publish, and install

**Files:**
- Generated package output only

**Step 1: Run complete verification**

Run all Debug and Release tests with warnings treated as errors, build the marketplace package, and run the package verifier.

Expected: all tests pass and package verification succeeds.

**Step 2: Publish and merge**

Commit the scoped files, push `agent/recover-resident-on-prompt`, create a pull request to `main`, wait for checks, and merge.

**Step 3: Upgrade the local plugin**

Replace the personal source and installed cache with the verified `0.3.3` package, preserve settings, restart exactly one resident, and verify the installed version and hook manifest.
