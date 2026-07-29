# Security Policy

## Supported version

Security fixes are provided for the latest published version.

## Reporting a vulnerability

Use the repository's private GitHub Security Advisory reporting flow. Do not include authentication data, private Codex conversations, or diagnostic logs in a public issue.

## Trust boundary

The plugin starts a local executable through a Codex `SessionStart` hook. Codex asks users to review and trust bundled hooks before enabling them. The executable communicates only with the local Codex app-server and writes local settings and bounded diagnostic logs.
