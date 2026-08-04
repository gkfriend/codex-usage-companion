# Privacy

Codex Usage Companion runs locally on Windows. It does not include telemetry, analytics, advertising, or an external service.

The companion reads rate-limit information from the locally installed Codex app-server. It does not read, copy, or store authentication tokens.

Local files:

- Settings are stored in the Codex plugin data directory when available, otherwise `%LOCALAPPDATA%\CodexUsageCompanion\settings.json`.
- Bounded diagnostic logs are stored under `%LOCALAPPDATA%\CodexUsageCompanion`.
- No settings or logs are uploaded by the plugin.

The source code and release workflows are public so these behaviors can be inspected.
