param(
    [string]$LocalAppDataPath = $env:LOCALAPPDATA,
    [switch]$SkipTaskRegistration
)

$ErrorActionPreference = 'Stop'
$taskPath = '\CodexUsageCompanion\'
$taskName = 'Recovery'
$recoveryRoot = Join-Path $LocalAppDataPath 'CodexUsageCompanion\Recovery'

if (-not $SkipTaskRegistration) {
    $task = Get-ScheduledTask -TaskPath $taskPath -TaskName $taskName -ErrorAction SilentlyContinue
    if ($null -ne $task) {
        Unregister-ScheduledTask -TaskPath $taskPath -TaskName $taskName -Confirm:$false
    }
}

if (Test-Path -LiteralPath $recoveryRoot) {
    Remove-Item -LiteralPath $recoveryRoot -Recurse -Force
}

Write-Output 'Recovery removed.'
