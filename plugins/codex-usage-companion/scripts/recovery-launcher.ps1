param(
    [string]$UserProfilePath = $env:USERPROFILE,
    [string]$LocalAppDataPath = $env:LOCALAPPDATA,
    [switch]$ResolveOnly
)

$ErrorActionPreference = 'Stop'
$recoveryRoot = Join-Path $LocalAppDataPath 'CodexUsageCompanion\Recovery'
$configurationPath = Join-Path $recoveryRoot 'recovery.json'
$logPath = Join-Path $LocalAppDataPath 'CodexUsageCompanion\recovery.log'
$previousLogPath = Join-Path $LocalAppDataPath 'CodexUsageCompanion\recovery.previous.log'

function Write-RecoveryLog([string]$Message) {
    try {
        $directory = Split-Path -Parent $logPath
        New-Item -ItemType Directory -Force -Path $directory | Out-Null
        if ((Test-Path -LiteralPath $logPath) -and (Get-Item -LiteralPath $logPath).Length -ge 262144) {
            Move-Item -LiteralPath $logPath -Destination $previousLogPath -Force
        }
        Add-Content -LiteralPath $logPath -Value "$(Get-Date -Format o) [recovery] $Message" -Encoding utf8
    }
    catch {
    }
}

try {
    $candidatePaths = [System.Collections.Generic.List[string]]::new()
    if (Test-Path -LiteralPath $configurationPath) {
        $configuration = Get-Content -LiteralPath $configurationPath -Raw | ConvertFrom-Json
        if (-not [string]::IsNullOrWhiteSpace($configuration.preferredExecutable)) {
            $candidatePaths.Add([string]$configuration.preferredExecutable)
        }
    }

    $patterns = @(
        (Join-Path $UserProfilePath 'plugins\codex-usage-companion\bin\win-x64\CodexUsageCompanion.exe'),
        (Join-Path $UserProfilePath '.codex\plugins\cache\*\codex-usage-companion\*\bin\win-x64\CodexUsageCompanion.exe')
    )
    foreach ($pattern in $patterns) {
        foreach ($item in @(Get-Item -Path $pattern -ErrorAction SilentlyContinue)) {
            $candidatePaths.Add($item.FullName)
        }
    }

    $candidates = @($candidatePaths |
        Sort-Object -Unique |
        Where-Object { Test-Path -LiteralPath $_ } |
        ForEach-Object {
            $item = Get-Item -LiteralPath $_
            $version = [version]'0.0.0.0'
            if (-not [string]::IsNullOrWhiteSpace($item.VersionInfo.FileVersion)) {
                [version]::TryParse($item.VersionInfo.FileVersion, [ref]$version) | Out-Null
            }
            [pscustomobject]@{
                Path = $item.FullName
                Version = $version
                LastWriteTimeUtc = $item.LastWriteTimeUtc
            }
        } |
        Sort-Object @{ Expression = { $_.Version }; Descending = $true }, @{ Expression = { $_.LastWriteTimeUtc }; Descending = $true })

    $selected = $candidates | Select-Object -First 1
    if ($null -eq $selected) {
        Write-RecoveryLog 'no-installed-executable'
        exit 2
    }

    if ($ResolveOnly) {
        Write-Output $selected.Path
        exit 0
    }

    $process = Start-Process -FilePath $selected.Path -ArgumentList '--recover' -WindowStyle Hidden -Wait -PassThru
    if ($process.ExitCode -ne 0) {
        Write-RecoveryLog "command-failed exit=$($process.ExitCode) executable=$($selected.Path)"
    }
    exit $process.ExitCode
}
catch {
    Write-RecoveryLog "$($_.Exception.GetType().Name): $($_.Exception.Message)"
    exit 1
}
