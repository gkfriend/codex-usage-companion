param(
    [string]$LocalAppDataPath = $env:LOCALAPPDATA,
    [string]$UserProfilePath = $env:USERPROFILE,
    [string]$PreferredExecutable,
    [switch]$SkipTaskRegistration
)

$ErrorActionPreference = 'Stop'
$taskPath = '\CodexUsageCompanion\'
$taskName = 'Recovery'
$recoveryRoot = Join-Path $LocalAppDataPath 'CodexUsageCompanion\Recovery'
$launcherSource = Join-Path $PSScriptRoot 'recovery-launcher.ps1'
$launcherDestination = Join-Path $recoveryRoot 'recovery-launcher.ps1'
$wrapperSource = Join-Path $PSScriptRoot 'recovery-launcher.vbs'
$wrapperDestination = Join-Path $recoveryRoot 'recovery-launcher.vbs'
$configurationPath = Join-Path $recoveryRoot 'recovery.json'

if (-not (Test-Path -LiteralPath $launcherSource)) {
    throw "Recovery launcher is missing: $launcherSource"
}

if (-not (Test-Path -LiteralPath $wrapperSource)) {
    throw "Recovery wrapper is missing: $wrapperSource"
}

if ([string]::IsNullOrWhiteSpace($PreferredExecutable)) {
    $pluginRoot = Split-Path -Parent $PSScriptRoot
    $candidates = @(
        (Join-Path $pluginRoot 'bin\win-x64\CodexUsageCompanion.exe'),
        (Join-Path $pluginRoot 'artifacts\publish\win-x64\CodexUsageCompanion.exe'),
        (Join-Path $pluginRoot 'src\CodexUsageCompanion\bin\Release\net8.0-windows\win-x64\publish\CodexUsageCompanion.exe'),
        (Join-Path $pluginRoot 'src\CodexUsageCompanion\bin\Debug\net8.0-windows\CodexUsageCompanion.exe')
    )
    $PreferredExecutable = $candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
}

if ([string]::IsNullOrWhiteSpace($PreferredExecutable) -or -not (Test-Path -LiteralPath $PreferredExecutable)) {
    throw 'CodexUsageCompanion.exe could not be located.'
}

New-Item -ItemType Directory -Force -Path $recoveryRoot | Out-Null
Copy-Item -LiteralPath $launcherSource -Destination $launcherDestination -Force
Copy-Item -LiteralPath $wrapperSource -Destination $wrapperDestination -Force
[pscustomobject]@{
    preferredExecutable = [System.IO.Path]::GetFullPath($PreferredExecutable)
    userProfilePath = [System.IO.Path]::GetFullPath($UserProfilePath)
} | ConvertTo-Json | Set-Content -LiteralPath $configurationPath -Encoding utf8

if (-not $SkipTaskRegistration) {
    $windowsScriptHost = Join-Path $env:SystemRoot 'System32\wscript.exe'
    $arguments = "//B //NoLogo `"$wrapperDestination`""
    $action = New-ScheduledTaskAction -Execute $windowsScriptHost -Argument $arguments
    $trigger = New-ScheduledTaskTrigger -Once -At (Get-Date).AddMinutes(5) -RepetitionInterval (New-TimeSpan -Minutes 5)
    $trigger.Repetition.StopAtDurationEnd = $false
    $settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable -Hidden -ExecutionTimeLimit (New-TimeSpan -Minutes 1) -MultipleInstances IgnoreNew
    $userId = [System.Security.Principal.WindowsIdentity]::GetCurrent().Name
    $principal = New-ScheduledTaskPrincipal -UserId $userId -LogonType Interactive -RunLevel Limited
    Register-ScheduledTask -TaskPath $taskPath -TaskName $taskName -Action $action -Trigger $trigger -Settings $settings -Principal $principal -Description 'Recovers Codex Usage Companion when Codex Desktop is running.' -Force | Out-Null
    Start-ScheduledTask -TaskPath $taskPath -TaskName $taskName
}

Write-Output "Recovery installed at $recoveryRoot"
