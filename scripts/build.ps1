param(
    [switch]$SkipTests
)

$ErrorActionPreference = 'Stop'
$root = [System.IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$project = Join-Path $root 'src\CodexUsageCompanion\CodexUsageCompanion.csproj'
$solution = Join-Path $root 'CodexUsageCompanion.slnx'
$artifacts = Join-Path $root 'artifacts'
$publish = Join-Path $artifacts 'publish\win-x64'
$marketplace = Join-Path $artifacts 'marketplace'
$plugin = Join-Path $marketplace 'plugins\codex-usage-companion'
$manifestPath = Join-Path $root '.codex-plugin\plugin.json'
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$archive = Join-Path $artifacts "CodexUsageCompanionMarketplace-v$($manifest.version).zip"
$checksum = "$archive.sha256"

if (-not $artifacts.StartsWith($root, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw 'Artifact directory must remain inside the repository.'
}

if (Test-Path -LiteralPath $artifacts) {
    Remove-Item -LiteralPath $artifacts -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $publish,(Join-Path $marketplace '.agents\plugins'),(Join-Path $plugin '.codex-plugin'),(Join-Path $plugin 'hooks'),(Join-Path $plugin 'bin\win-x64'),(Join-Path $plugin 'assets\screenshots'),(Join-Path $plugin 'scripts') | Out-Null

dotnet restore $solution
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet restore $project --runtime win-x64
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if (-not $SkipTests) {
    dotnet test $solution --configuration Release --no-restore -p:TreatWarningsAsErrors=true -p:Optimize=true -p:DebugType=None -p:DebugSymbols=false
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

dotnet publish $project --configuration Release --runtime win-x64 --self-contained true --no-restore -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:PublishTrimmed=false -p:TreatWarningsAsErrors=true -p:Optimize=true -p:DebugType=None -p:DebugSymbols=false --output $publish
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Copy-Item -LiteralPath (Join-Path $root 'packaging\marketplace.json') -Destination (Join-Path $marketplace '.agents\plugins\marketplace.json') -Force
Copy-Item -LiteralPath $manifestPath -Destination (Join-Path $plugin '.codex-plugin\plugin.json') -Force
Copy-Item -LiteralPath (Join-Path $root 'hooks\hooks.json') -Destination (Join-Path $plugin 'hooks\hooks.json') -Force
Copy-Item -LiteralPath (Join-Path $root 'README.md') -Destination (Join-Path $plugin 'README.md') -Force
Copy-Item -LiteralPath (Join-Path $root 'README.zh-Hant.md') -Destination (Join-Path $plugin 'README.zh-Hant.md') -Force
Copy-Item -LiteralPath (Join-Path $root 'README.zh-Hans.md') -Destination (Join-Path $plugin 'README.zh-Hans.md') -Force
Copy-Item -LiteralPath (Join-Path $root 'LICENSE') -Destination (Join-Path $plugin 'LICENSE') -Force
Copy-Item -LiteralPath (Join-Path $root 'PRIVACY.md') -Destination (Join-Path $plugin 'PRIVACY.md') -Force
Copy-Item -Path (Join-Path $root 'assets\screenshots\*') -Destination (Join-Path $plugin 'assets\screenshots') -Force
Copy-Item -LiteralPath (Join-Path $publish 'CodexUsageCompanion.exe') -Destination (Join-Path $plugin 'bin\win-x64\CodexUsageCompanion.exe') -Force
Copy-Item -LiteralPath (Join-Path $root 'scripts\recovery-launcher.ps1') -Destination (Join-Path $plugin 'scripts\recovery-launcher.ps1') -Force
Copy-Item -LiteralPath (Join-Path $root 'scripts\install-recovery.ps1') -Destination (Join-Path $plugin 'scripts\install-recovery.ps1') -Force
Copy-Item -LiteralPath (Join-Path $root 'scripts\uninstall-recovery.ps1') -Destination (Join-Path $plugin 'scripts\uninstall-recovery.ps1') -Force

Compress-Archive -Path (Join-Path $marketplace '*') -DestinationPath $archive -CompressionLevel Optimal -Force
& (Join-Path $PSScriptRoot 'verify-package.ps1') -Archive $archive
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$hash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
"$hash  $([System.IO.Path]::GetFileName($archive))" | Set-Content -LiteralPath $checksum -Encoding ascii
Write-Output $archive
Write-Output $checksum
