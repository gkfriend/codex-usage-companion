param(
    [Parameter(Mandatory = $true)]
    [string]$Archive
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$required = @(
    '.agents/plugins/marketplace.json',
    'plugins/codex-usage-companion/.codex-plugin/plugin.json',
    'plugins/codex-usage-companion/hooks/hooks.json',
    'plugins/codex-usage-companion/bin/win-x64/CodexUsageCompanion.exe',
    'plugins/codex-usage-companion/scripts/recovery-launcher.ps1',
    'plugins/codex-usage-companion/scripts/recovery-launcher.vbs',
    'plugins/codex-usage-companion/scripts/install-recovery.ps1',
    'plugins/codex-usage-companion/scripts/uninstall-recovery.ps1',
    'plugins/codex-usage-companion/README.md',
    'plugins/codex-usage-companion/README.zh-Hant.md',
    'plugins/codex-usage-companion/README.zh-Hans.md',
    'plugins/codex-usage-companion/assets/screenshots/overlay-en.png',
    'plugins/codex-usage-companion/assets/screenshots/overlay-zh-Hant.png',
    'plugins/codex-usage-companion/assets/screenshots/overlay-zh-Hans.png',
    'plugins/codex-usage-companion/LICENSE',
    'plugins/codex-usage-companion/PRIVACY.md'
)

$zip = [System.IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $Archive))
try {
    $entries = @($zip.Entries | ForEach-Object { $_.FullName.Replace('\', '/') })
    foreach ($entry in $required) {
        if ($entry -notin $entries) {
            throw "Missing package entry: $entry"
        }
    }

    $forbidden = @($entries | Where-Object { $_ -match '(^|/)(obj|\.codegraph)/' -or $_ -match '\.(pdb|user|suo)$' })
    if ($forbidden.Count -gt 0) {
        throw "Forbidden package entries: $($forbidden -join ', ')"
    }

    $executable = $zip.Entries | Where-Object { $_.FullName.Replace('\', '/') -eq 'plugins/codex-usage-companion/bin/win-x64/CodexUsageCompanion.exe' } | Select-Object -First 1
    if ($null -eq $executable -or $executable.Length -lt 1000000) {
        throw 'Packaged executable is missing or unexpectedly small.'
    }
}
finally {
    $zip.Dispose()
}

Write-Output "Verified $Archive"
