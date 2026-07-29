using System.Diagnostics;

namespace CodexUsageCompanion.Lifecycle;

public static class DetachedLauncher
{
    public static ProcessStartInfo CreateStartInfo(string executablePath)
    {
        return new ProcessStartInfo
        {
            FileName = executablePath,
            Arguments = "--background",
            UseShellExecute = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };
    }

    public static bool Start()
    {
        var executablePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            return false;
        }

        using var process = Process.Start(CreateStartInfo(executablePath));
        return process is not null;
    }
}
