param(
    [switch]$SkipTaskIntegration
)

$ErrorActionPreference = 'Stop'
$root = [System.IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$launcherSource = Join-Path $PSScriptRoot 'recovery-launcher.ps1'
$wrapperSource = Join-Path $PSScriptRoot 'recovery-launcher.vbs'
$installer = Join-Path $PSScriptRoot 'install-recovery.ps1'
$uninstaller = Join-Path $PSScriptRoot 'uninstall-recovery.ps1'
$executableSource = Join-Path $root 'src\CodexUsageCompanion\bin\Debug\net8.0-windows\CodexUsageCompanion.exe'

foreach ($path in @($launcherSource, $wrapperSource, $installer, $uninstaller, $executableSource)) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required test input is missing: $path"
    }
}

$temporaryRoot = Join-Path ([System.IO.Path]::GetTempPath()) "CodexUsageCompanion-Recovery-$([Guid]::NewGuid().ToString('N'))"
$profile = Join-Path $temporaryRoot 'profile'
$localAppData = Join-Path $temporaryRoot 'local'
$sourceExecutable = Join-Path $profile 'plugins\codex-usage-companion\bin\win-x64\CodexUsageCompanion.exe'
$cacheExecutable = Join-Path $profile '.codex\plugins\cache\personal\codex-usage-companion\0.3.6\bin\win-x64\CodexUsageCompanion.exe'

try {
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $sourceExecutable),(Split-Path -Parent $cacheExecutable) | Out-Null
    Copy-Item -LiteralPath $executableSource -Destination $sourceExecutable
    Copy-Item -LiteralPath $executableSource -Destination $cacheExecutable
    (Get-Item -LiteralPath $sourceExecutable).LastWriteTimeUtc = [DateTime]::UtcNow.AddMinutes(-5)
    (Get-Item -LiteralPath $cacheExecutable).LastWriteTimeUtc = [DateTime]::UtcNow

    & $installer -LocalAppDataPath $localAppData -UserProfilePath $profile -PreferredExecutable $sourceExecutable -SkipTaskRegistration
    & $installer -LocalAppDataPath $localAppData -UserProfilePath $profile -PreferredExecutable $sourceExecutable -SkipTaskRegistration

    $installedLauncher = Join-Path $localAppData 'CodexUsageCompanion\Recovery\recovery-launcher.ps1'
    $installedWrapper = Join-Path $localAppData 'CodexUsageCompanion\Recovery\recovery-launcher.vbs'
    if (-not (Test-Path -LiteralPath $installedWrapper)) {
        throw 'Recovery wrapper was not installed.'
    }
    $resolved = & pwsh -NoProfile -File $installedLauncher -LocalAppDataPath $localAppData -UserProfilePath $profile -ResolveOnly
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    if ($resolved -ne $cacheExecutable) {
        throw "Expected newest executable '$cacheExecutable' but resolved '$resolved'."
    }

    $configurationPath = Join-Path $localAppData 'CodexUsageCompanion\Recovery\recovery.json'
    if (-not (Test-Path -LiteralPath $configurationPath)) {
        throw 'Recovery configuration was not installed.'
    }

    & $uninstaller -LocalAppDataPath $localAppData -SkipTaskRegistration
    if (Test-Path -LiteralPath (Join-Path $localAppData 'CodexUsageCompanion\Recovery')) {
        throw 'Recovery directory was not removed.'
    }

    if (-not $SkipTaskIntegration) {
        & $installer
        $task = Get-ScheduledTask -TaskPath '\CodexUsageCompanion\' -TaskName 'Recovery' -ErrorAction Stop
        if ($task.Settings.MultipleInstances -ne 'IgnoreNew') {
            throw 'Scheduled task does not ignore duplicate invocations.'
        }
        if ([System.IO.Path]::GetFileName($task.Actions.Execute) -ne 'wscript.exe') {
            throw "Scheduled task must use wscript.exe but uses '$($task.Actions.Execute)'."
        }
        if ($task.Actions.Arguments -notlike '*recovery-launcher.vbs*') {
            throw 'Scheduled task does not invoke the recovery wrapper.'
        }
        if ($task.Actions.Arguments -notlike '*//B*') {
            throw 'Scheduled task does not suppress Windows Script Host dialogs.'
        }
        if ($task.Triggers.Repetition.Interval -ne 'PT5M') {
            throw "Scheduled task interval must be PT5M but is '$($task.Triggers.Repetition.Interval)'."
        }
    }

    Write-Output 'Recovery verification passed.'
}
finally {
    if (Test-Path -LiteralPath $temporaryRoot) {
        Remove-Item -LiteralPath $temporaryRoot -Recurse -Force
    }
}
