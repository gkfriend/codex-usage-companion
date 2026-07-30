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

public enum JobBreakawayPolicy
{
    OutsideJob,
    ExplicitBreakawayAllowed,
    SilentBreakaway,
    Restricted
}

public sealed record DetachedLaunchRequest(
    string ExecutablePath,
    string CommandLine,
    string WorkingDirectory,
    DetachedProcessCreationFlags CreationFlags);

public static class DetachedLauncher
{
    private const uint JobObjectLimitBreakawayOk = 0x00000800;
    private const uint JobObjectLimitSilentBreakawayOk = 0x00001000;

    public static DetachedLaunchRequest CreateRequest(string executablePath, JobBreakawayPolicy policy)
    {
        var flags = DetachedProcessCreationFlags.CreateNoWindow;
        if (policy == JobBreakawayPolicy.ExplicitBreakawayAllowed)
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

        var policy = GetCurrentJobPolicy();
        var request = CreateRequest(executablePath, policy);
        if (!TryCreateProcess(request, out var processInformation))
        {
            var error = Marshal.GetLastWin32Error();
            CompanionLog.Shared.Write("launcher", new Win32Exception(error));
            if (policy != JobBreakawayPolicy.ExplicitBreakawayAllowed)
            {
                return false;
            }

            CompanionLog.Shared.Write("launcher-fallback", "Explicit Job breakaway was denied; starting with response-hook recovery.");
            request = CreateRequest(executablePath, JobBreakawayPolicy.Restricted);
            if (!TryCreateProcess(request, out processInformation))
            {
                CompanionLog.Shared.Write("launcher-fallback", new Win32Exception(Marshal.GetLastWin32Error()));
                return false;
            }
        }

        VerifyChildJobState(processInformation.ProcessHandle, policy);
        CloseHandle(processInformation.ThreadHandle);
        CloseHandle(processInformation.ProcessHandle);
        return true;
    }

    private static bool TryCreateProcess(DetachedLaunchRequest request, out ProcessInformation processInformation)
    {
        var startupInfo = new StartupInfo
        {
            Size = Marshal.SizeOf<StartupInfo>()
        };
        var commandLine = new StringBuilder(request.CommandLine);
        return CreateProcess(
                request.ExecutablePath,
                commandLine,
                0,
                0,
                false,
                request.CreationFlags,
                0,
                request.WorkingDirectory,
                ref startupInfo,
                out processInformation);
    }

    public static JobBreakawayPolicy ClassifyJobPolicy(bool isInJob, uint limitFlags)
    {
        if (!isInJob)
        {
            return JobBreakawayPolicy.OutsideJob;
        }

        if ((limitFlags & JobObjectLimitSilentBreakawayOk) != 0)
        {
            return JobBreakawayPolicy.SilentBreakaway;
        }

        return (limitFlags & JobObjectLimitBreakawayOk) != 0
            ? JobBreakawayPolicy.ExplicitBreakawayAllowed
            : JobBreakawayPolicy.Restricted;
    }

    private static JobBreakawayPolicy GetCurrentJobPolicy()
    {
        using var process = Process.GetCurrentProcess();
        if (!IsProcessInJob(process.Handle, 0, out var isInJob))
        {
            CompanionLog.Shared.Write("launcher-job-query", new Win32Exception(Marshal.GetLastWin32Error()));
            return JobBreakawayPolicy.Restricted;
        }

        if (!isInJob)
        {
            return JobBreakawayPolicy.OutsideJob;
        }

        var information = new JobObjectExtendedLimitInformation();
        if (!QueryInformationJobObject(
                0,
                JobObjectInformationClass.ExtendedLimitInformation,
                ref information,
                Marshal.SizeOf<JobObjectExtendedLimitInformation>(),
                out _))
        {
            CompanionLog.Shared.Write("launcher-job-limits", new Win32Exception(Marshal.GetLastWin32Error()));
            return JobBreakawayPolicy.Restricted;
        }

        return ClassifyJobPolicy(true, information.BasicLimitInformation.LimitFlags);
    }

    private static void VerifyChildJobState(nint processHandle, JobBreakawayPolicy policy)
    {
        if (!IsProcessInJob(processHandle, 0, out var isInJob))
        {
            CompanionLog.Shared.Write("launcher-child-job-query", new Win32Exception(Marshal.GetLastWin32Error()));
            return;
        }

        if (isInJob && policy != JobBreakawayPolicy.Restricted)
        {
            CompanionLog.Shared.Write("launcher-child-job", "The resident remained in a parent Job; response-hook recovery remains active.");
        }
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

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool QueryInformationJobObject(
        nint jobHandle,
        JobObjectInformationClass jobObjectInformationClass,
        ref JobObjectExtendedLimitInformation jobObjectInformation,
        int jobObjectInformationLength,
        out int returnLength);

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

    private enum JobObjectInformationClass
    {
        ExtendedLimitInformation = 9
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct JobObjectBasicLimitInformation
    {
        public long PerProcessUserTimeLimit;
        public long PerJobUserTimeLimit;
        public uint LimitFlags;
        public nuint MinimumWorkingSetSize;
        public nuint MaximumWorkingSetSize;
        public uint ActiveProcessLimit;
        public nuint Affinity;
        public uint PriorityClass;
        public uint SchedulingClass;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IoCounters
    {
        public ulong ReadOperationCount;
        public ulong WriteOperationCount;
        public ulong OtherOperationCount;
        public ulong ReadTransferCount;
        public ulong WriteTransferCount;
        public ulong OtherTransferCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct JobObjectExtendedLimitInformation
    {
        public JobObjectBasicLimitInformation BasicLimitInformation;
        public IoCounters IoInfo;
        public nuint ProcessMemoryLimit;
        public nuint JobMemoryLimit;
        public nuint PeakProcessMemoryUsed;
        public nuint PeakJobMemoryUsed;
    }
}
