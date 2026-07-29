# Contributing

Issues and pull requests are welcome.

Requirements:

- Windows 10 or later
- .NET 8 SDK
- Codex Desktop for manual integration testing

Before submitting a change:

```powershell
dotnet test CodexUsageCompanion.slnx --configuration Debug -p:TreatWarningsAsErrors=true
dotnet test CodexUsageCompanion.slnx --configuration Release -p:TreatWarningsAsErrors=true
dotnet list CodexUsageCompanion.slnx package --vulnerable --include-transitive
pwsh -File scripts/build.ps1
```

Keep changes focused, add regression tests for behavior changes, and do not commit generated output, logs, user settings, credentials, or local paths.
