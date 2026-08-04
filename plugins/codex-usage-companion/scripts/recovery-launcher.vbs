Dim shell, fileSystem, powerShell, launcher, command, result
Set shell = CreateObject("WScript.Shell")
Set fileSystem = CreateObject("Scripting.FileSystemObject")
powerShell = shell.ExpandEnvironmentStrings("%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe")
launcher = fileSystem.BuildPath(fileSystem.GetParentFolderName(WScript.ScriptFullName), "recovery-launcher.ps1")
command = Chr(34) & powerShell & Chr(34) & " -NoProfile -NonInteractive -WindowStyle Hidden -ExecutionPolicy Bypass -File " & Chr(34) & launcher & Chr(34)
result = shell.Run(command, 0, True)
WScript.Quit result
