# CLI-safe Five-minute Refresh Design

## Scope

Codex Usage Companion remains a Windows-only companion for Codex Desktop. Docker and non-Windows support are outside this change. The enabled plugin must still be harmless when Codex CLI loads its bundled hooks on Windows.

## Problems

- The resident performs a fallback `account/rateLimits/read` every minute.
- The recovery task runs every three minutes.
- `UserPromptSubmit` and `Stop` can request additional reads on every turn.
- The `Stop` command can return exit code `1`, waits close to its ten-second hook timeout, and does not emit the JSON object required by the Codex hook contract.
- App-server `account/rateLimits/updated` notifications currently cause another read instead of applying their supplied state.

## Selected Design

All network reads are centralized behind a five-minute minimum interval. Startup performs one immediate read, and the fallback timer requests another read every five minutes. Repeated hook signals within that interval are coalesced without another app-server read.

The app-server notification payload is parsed and applied directly to the panel. It does not count as a query and avoids an unnecessary follow-up `account/rateLimits/read`.

Windows hook commands remain bundled with the plugin, but they become non-blocking lifecycle notifications. When Codex Desktop is not running, including ordinary Codex CLI use, they do nothing. Every hook invocation exits `0` and emits an empty JSON object so `Stop` satisfies the official hook output contract. Launch failures are logged and left for the recovery task instead of failing the Codex turn.

The Windows recovery task changes to a five-minute interval and keeps the windowless `wscript.exe` launcher.

## Runtime Flow

1. `SessionStart` or `UserPromptSubmit` asks the companion to ensure the resident exists.
2. `Stop` signals a refresh request when a Desktop resident exists.
3. Every hook immediately emits `{}` and exits successfully.
4. The resident accepts at most one app-server read in each five-minute window.
5. `account/rateLimits/updated` notifications update the view directly.
6. The five-minute fallback read and five-minute recovery task cover missed notifications and terminated residents.

## Failure Handling

- CLI-only sessions do not start the Desktop companion.
- Hook launch or signal failures are logged but never fail the Codex turn.
- Invalid notification payloads are logged and ignored; the fallback read remains available.
- Existing app-server retry behavior remains unchanged for accepted reads.

## Verification

- Hook manifest tests require JSON-safe command modes and a short timeout.
- Command tests prove CLI-only hooks return `0` and output `{}`.
- Refresh coordinator tests prove repeated requests inside five minutes produce one read.
- Session tests prove rate-limit notifications carry parsed state without a follow-up read.
- Recovery tests require `PT5M`.
- Full tests, package verification, live CLI hook execution, and the installed scheduled task must pass before release `0.3.6`.
