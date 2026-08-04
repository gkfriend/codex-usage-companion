# CLI-safe Five-minute Refresh Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Release version 0.3.6 with CLI-safe hooks, direct rate-limit notifications, and no more than one fallback usage query every five minutes.

**Architecture:** A testable hook runner separates Codex lifecycle compliance from resident startup. The resident applies app-server notification payloads directly and routes explicit reads through a five-minute gate. Windows Task Scheduler remains a separate five-minute recovery fallback.

**Tech Stack:** C# 12, .NET 8 WPF, JSON-RPC, xUnit, PowerShell, Windows Task Scheduler

---

### Task 1: CLI-safe Hook Contract

**Files:**
- Create: `src/CodexUsageCompanion/Lifecycle/HookCommandRunner.cs`
- Create: `tests/CodexUsageCompanion.Tests/HookCommandRunnerTests.cs`
- Modify: `src/CodexUsageCompanion/Program.cs`
- Modify: `hooks/hooks.json`
- Modify: `tests/CodexUsageCompanion.Tests/HookManifestTests.cs`

1. Add failing tests that require hook commands to exit `0`, emit `{}`, skip work without Codex Desktop, signal an existing resident, and never wait for a refresh result.
2. Run the focused tests and confirm the expected failures.
3. Implement the smallest hook runner and route `--session-start` and `--refresh` through it.
4. Set hook timeouts to five seconds and keep Windows executable commands.
5. Run the focused tests and commit.

### Task 2: Five-minute Query Gate

**Files:**
- Create: `src/CodexUsageCompanion/Lifecycle/RefreshPolicy.cs`
- Modify: `src/CodexUsageCompanion/Lifecycle/RefreshCoordinator.cs`
- Modify: `src/CodexUsageCompanion/Lifecycle/CompanionRuntime.cs`
- Modify: `tests/CodexUsageCompanion.Tests/RefreshCoordinatorTests.cs`

1. Add a failing fake-clock test proving repeated requests within five minutes produce only one read and a request at five minutes is accepted.
2. Run the test and confirm it fails because no interval gate exists.
3. Add a thread-safe minimum interval to `RefreshCoordinator` and pass `RefreshPolicy.MinimumInterval` from the runtime.
4. Change the fallback timer to five minutes.
5. Run the focused and lifecycle tests and commit.

### Task 3: Apply App-server Notifications Directly

**Files:**
- Modify: `src/CodexUsageCompanion/RateLimits/JsonLineRpcClient.cs`
- Modify: `src/CodexUsageCompanion/RateLimits/CodexAppServerSession.cs`
- Modify: `src/CodexUsageCompanion/RateLimits/CodexAppServerClient.cs`
- Modify: `src/CodexUsageCompanion/RateLimits/RateLimitParser.cs`
- Modify: `src/CodexUsageCompanion/Lifecycle/CompanionRuntime.cs`
- Modify: `tests/CodexUsageCompanion.Tests/JsonLineRpcClientTests.cs`
- Modify: `tests/CodexUsageCompanion.Tests/CodexAppServerSessionTests.cs`
- Modify: `tests/CodexUsageCompanion.Tests/RateLimitParserTests.cs`

1. Add failing tests that preserve raw notification JSON and parse `account/rateLimits/updated` into `RateLimitState`.
2. Run the tests and confirm they fail against method-only notifications.
3. Carry raw notification payloads through the connection, session, and client.
4. Apply valid notification states directly on the WPF dispatcher and log invalid payloads.
5. Run focused tests and commit.

### Task 4: Five-minute Recovery and Version 0.3.6

**Files:**
- Modify: `scripts/install-recovery.ps1`
- Modify: `scripts/test-recovery.ps1`
- Modify: `.codex-plugin/plugin.json`
- Modify: `src/CodexUsageCompanion/CodexUsageCompanion.csproj`
- Modify: `tests/CodexUsageCompanion.Tests/CodexAppServerSessionTests.cs`
- Modify: `README.md`
- Modify: `README.zh-Hant.md`
- Modify: `README.zh-Hans.md`

1. Change the recovery test expectation from `PT3M` to `PT5M` and confirm the integration test fails.
2. Change the recovery trigger and initial delay to five minutes.
3. Bump all active version assertions to `0.3.6`.
4. Update all three README variants with the five-minute and CLI-safe behavior.
5. Run recovery tests and commit.

### Task 5: Verify, Publish, and Install

**Files:**
- Verify all modified source and package files.

1. Run `dotnet test CodexUsageCompanion.slnx --configuration Release` and require zero failures.
2. Run `pwsh -NoProfile -File scripts/test-recovery.ps1` and require `PT5M`.
3. Run `pwsh -NoProfile -File scripts/build.ps1` and verify the v0.3.6 package.
4. Execute installed-style hook commands with redirected output and require exit `0` plus `{}`.
5. Review the complete diff, push the branch, merge the pull request, tag `v0.3.6`, and wait for the GitHub release workflow.
6. Verify the release checksum, synchronize the local source and cache, reinstall recovery, and confirm one v0.3.6 resident with a successful five-minute task.
