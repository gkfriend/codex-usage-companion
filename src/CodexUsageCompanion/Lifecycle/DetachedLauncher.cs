using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using CodexUsageCompanion.Diagnostics;

namespace CodexUsageCompanion.Lifecycle;

[Flags]
public enum DetachedProcessCreationFlags : uint
{
    None = 0,
    CreateBreakawayFromJob = 0x01000000,
    CreateNoWindow = 0x08000000
}

public sealed record DetachedLaunchRequest(
    string ExecutablePath,
    string CommandLine,
    string WorkingDirectory,
    DetachedProcessCreationFlags CreationFlags);

public static class DetachedLauncher
{
    public static DetachedLaunchRequest CreateRequest(string executablePath, bool isInJob)
    {
        var flags = DetachedProcessCreationFlags.CreateNoWindow;
        if (isInJob)
        {
            flags |= DetachedProcessCreationFlags.CreateBreakawayFromJob;
        }

        return new DetachedLaunchRequest(
            executablePath,
            $"\"{executablePath}\" --background",
            Path.GetDirectoryName(executablePath) ?? Environment.CurrentDirectory,
            flags);
    }

    public static bool Start()
    {
        var executablePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            return false;
        }

        var request = CreateRequest(executablePath, IsCurrentProcessInJob());
        var startupInfo = new StartupInfo
        {
            Size = Marshal.SizeOf<StartupInfo>()
        };
        var commandLine = new StringBuilder(request.CommandLine);
        if (!CreateProcess(
                request.ExecutablePath,
                commandLine,
                0,
                0,
                false,
                request.CreationFlags,
                0,
                request.WorkingDirectory,
                ref startupInfo,
                out var processInformation))
        {
            CompanionLog.Shared.Write("launcher", new Win32Exception(Marshal.GetLastWin32Error()));
            return false;
        }

        CloseHandle(processInformation.ThreadHandle);
        CloseHandle(processInformation.ProcessHandle);
        return true;
    }

    private static bool IsCurrentProcessInJob()
    {
        using var process = Process.GetCurrentProcess();
        if (IsProcessInJob(process.Handle, 0, out var isInJob))
        {
            return isInJob;
        }

        CompanionLog.Shared.Write("launcher-job-query", new Win32Exception(Marshal.GetLastWin32Error()));
        return false;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CreateProcess(
        string applicationName,
        StringBuilder commandLine,
        nint processAttributes,
        nint threadAttributes,
        bool inheritHandles,
        DetachedProcessCreationFlags creationFlags,
        nint environment,
        string currentDirectory,
        ref StartupInfo startupInfo,
        out ProcessInformation processInformation);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool IsProcessInJob(nint processHandle, nint jobHandle, out bool result);

    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(nint handle);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct StartupInfo
    {
        public int Size;
        public string? Reserved;
        public string? Desktop;
        public string? Title;
        public int X;
        public int Y;
        public int XSize;
        public int YSize;
        public int XCountChars;
        public int YCountChars;
        public int FillAttribute;
        public int Flags;
        public short ShowWindow;
        public short ReservedSize;
        public nint ReservedPointer;
        public nint StandardInput;
        public nint StandardOutput;
        public nint StandardError;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessInformation
    {
        public nint ProcessHandle;
        public nint ThreadHandle;
        public int ProcessId;
        public int ThreadId;
    }
}
