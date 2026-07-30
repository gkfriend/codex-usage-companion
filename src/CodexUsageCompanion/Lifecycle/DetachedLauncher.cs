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
    CreateSuspended = 0x00000004,
    CreateNoWindow = 0x08000000
}

public enum JobBreakawayPolicy
{
    OutsideJob,
    ExplicitBreakawayAllowed,
    SilentBreakaway,
    Restricted
}

public enum DetachedLaunchStrategy
{
    Direct,
    SystemBroker
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
    private const int BrokerTimeoutMilliseconds = 6000;

    public static DetachedLaunchRequest CreateRequest(string executablePath, JobBreakawayPolicy policy)
    {
        var flags = DetachedProcessCreationFlags.CreateNoWindow | DetachedProcessCreationFlags.CreateSuspended;
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
        if (SelectLaunchStrategy(policy) == DetachedLaunchStrategy.SystemBroker)
        {
            return StartWithSystemBroker(executablePath);
        }

        var request = CreateRequest(executablePath, policy);
        if (!TryCreateProcess(request, out var processInformation))
        {
            CompanionLog.Shared.Write("launcher", new Win32Exception(Marshal.GetLastWin32Error()));
            return StartWithSystemBroker(executablePath);
        }

        var useBroker = false;
        if (!IsProcessInJob(processInformation.ProcessHandle, 0, out var childIsInJob))
        {
            CompanionLog.Shared.Write("launcher-child-job-query", new Win32Exception(Marshal.GetLastWin32Error()));
            useBroker = true;
        }
        else if (childIsInJob)
        {
            CompanionLog.Shared.Write("launcher-child-job", "Direct launch remained in a Job; retrying through the system broker.");
            useBroker = true;
        }

        if (useBroker)
        {
            TerminateProcess(processInformation.ProcessHandle, 1);
            CloseHandle(processInformation.ThreadHandle);
            CloseHandle(processInformation.ProcessHandle);
            return StartWithSystemBroker(executablePath);
        }

        var resumeResult = ResumeThread(processInformation.ThreadHandle);
        if (resumeResult == uint.MaxValue)
        {
            CompanionLog.Shared.Write("launcher-resume", new Win32Exception(Marshal.GetLastWin32Error()));
            TerminateProcess(processInformation.ProcessHandle, 1);
        }

        CloseHandle(processInformation.ThreadHandle);
        CloseHandle(processInformation.ProcessHandle);
        return resumeResult != uint.MaxValue;
    }

    public static DetachedLaunchStrategy SelectLaunchStrategy(JobBreakawayPolicy policy)
    {
        return policy == JobBreakawayPolicy.Restricted
            ? DetachedLaunchStrategy.SystemBroker
            : DetachedLaunchStrategy.Direct;
    }

    public static ProcessStartInfo CreateBrokerStartInfo(string executablePath)
    {
        var workingDirectory = Path.GetDirectoryName(executablePath) ?? Environment.CurrentDirectory;
        var commandLine = $"\"{executablePath}\" --background";
        var script = string.Concat(
            "$r=Invoke-CimMethod -ClassName Win32_Process -MethodName Create -Arguments @{CommandLine=",
            ToPowerShellLiteral(commandLine),
            ";CurrentDirectory=",
            ToPowerShellLiteral(workingDirectory),
            "};if([int]$r.ReturnValue -ne 0){exit [int]$r.ReturnValue};[Console]::Out.Write([string]$r.ProcessId)");
        var windowsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var startInfo = new ProcessStartInfo
        {
            FileName = Path.Combine(windowsDirectory, "System32", "WindowsPowerShell", "v1.0", "powershell.exe"),
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        startInfo.ArgumentList.Add("-NoLogo");
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-NonInteractive");
        startInfo.ArgumentList.Add("-WindowStyle");
        startInfo.ArgumentList.Add("Hidden");
        startInfo.ArgumentList.Add("-Command");
        startInfo.ArgumentList.Add(script);
        return startInfo;
    }

    private static bool StartWithSystemBroker(string executablePath)
    {
        using var broker = Process.Start(CreateBrokerStartInfo(executablePath));
        if (broker is null)
        {
            return false;
        }

        var outputTask = broker.StandardOutput.ReadToEndAsync();
        var errorTask = broker.StandardError.ReadToEndAsync();
        if (!broker.WaitForExit(BrokerTimeoutMilliseconds))
        {
            broker.Kill(true);
            CompanionLog.Shared.Write("launcher-broker", "System broker timed out.");
            return false;
        }

        var output = outputTask.GetAwaiter().GetResult().Trim();
        var error = errorTask.GetAwaiter().GetResult().Trim();
        if (broker.ExitCode != 0 || !int.TryParse(output, out var processId))
        {
            CompanionLog.Shared.Write("launcher-broker", $"System broker failed with code {broker.ExitCode}: {error}");
            return false;
        }

        try
        {
            using var process = Process.GetProcessById(processId);
            if (!IsProcessInJob(process.Handle, 0, out var isInJob))
            {
                CompanionLog.Shared.Write("launcher-broker-job-query", new Win32Exception(Marshal.GetLastWin32Error()));
                process.Kill();
                return false;
            }

            if (isInJob)
            {
                CompanionLog.Shared.Write("launcher-broker-job", "System broker returned a process that remained in a Job.");
                process.Kill();
                return false;
            }
        }
        catch (ArgumentException)
        {
        }

        return true;
    }

    private static string ToPowerShellLiteral(string value)
    {
        return $"'{value.Replace("'", "''", StringComparison.Ordinal)}'";
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

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint ResumeThread(nint threadHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool TerminateProcess(nint processHandle, uint exitCode);

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
