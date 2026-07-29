using System.Diagnostics;

namespace CodexUsageCompanion.Windows;

public sealed record CodexWindowInfo(
    nint Handle,
    PixelRect ClientBounds,
    bool IsMinimized,
    bool IsCloaked);

public sealed class CodexWindowLocator
{
    public CodexWindowInfo? Find()
    {
        var candidates = new List<CodexWindowInfo>();

        NativeMethods.EnumWindows((windowHandle, _) =>
        {
            var candidate = TryCreateWindowInfo(windowHandle);
            if (candidate is null)
            {
                return true;
            }

            candidates.Add(candidate);

            return true;
        }, 0);

        return SelectPreferred(candidates, NativeMethods.GetForegroundWindow());
    }

    public bool IsCodexRunning()
    {
        foreach (var processName in new[] { "ChatGPT", "codex" })
        {
            foreach (var process in Process.GetProcessesByName(processName))
            {
                using (process)
                {
                    try
                    {
                        if (IsCodexHostProcess(process.ProcessName, process.MainModule?.FileName))
                        {
                            return true;
                        }
                    }
                    catch (InvalidOperationException)
                    {
                    }
                    catch (System.ComponentModel.Win32Exception)
                    {
                    }
                }
            }
        }

        return false;
    }

    public static bool IsCodexHostProcess(string processName, string? executablePath)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            return false;
        }

        var normalizedPath = executablePath.Replace('/', '\\');
        var isCodexInstallation = normalizedPath.Contains("\\WindowsApps\\OpenAI.Codex_", StringComparison.OrdinalIgnoreCase) ||
                                  normalizedPath.Contains("\\Programs\\OpenAI\\Codex\\", StringComparison.OrdinalIgnoreCase);
        if (!isCodexInstallation)
        {
            return false;
        }

        return processName.Equals("ChatGPT", StringComparison.OrdinalIgnoreCase) &&
               normalizedPath.EndsWith("\\ChatGPT.exe", StringComparison.OrdinalIgnoreCase) ||
               processName.Equals("codex", StringComparison.OrdinalIgnoreCase) &&
               normalizedPath.EndsWith("\\app\\resources\\codex.exe", StringComparison.OrdinalIgnoreCase);
    }

    public static CodexWindowInfo? SelectPreferred(IEnumerable<CodexWindowInfo> candidates, nint foregroundWindow)
    {
        var eligible = candidates.ToArray();
        return eligible.FirstOrDefault(candidate => candidate.Handle == foregroundWindow) ??
               eligible.MaxBy(candidate => (long)candidate.ClientBounds.Width * candidate.ClientBounds.Height);
    }

    private static CodexWindowInfo? TryCreateWindowInfo(nint windowHandle)
    {
        if (!NativeMethods.IsWindowVisible(windowHandle))
        {
            return null;
        }

        NativeMethods.GetWindowThreadProcessId(windowHandle, out var processId);
        if (processId == 0)
        {
            return null;
        }

        try
        {
            using var process = Process.GetProcessById((int)processId);
            var executablePath = process.MainModule?.FileName;
            if (!IsCodexHostProcess(process.ProcessName, executablePath) ||
                !TryGetClientBounds(windowHandle, out var bounds) ||
                bounds.Width < 320 ||
                bounds.Height < 240)
            {
                return null;
            }

            return new CodexWindowInfo(
                windowHandle,
                bounds,
                NativeMethods.IsIconic(windowHandle),
                IsCloaked(windowHandle));
        }
        catch (ArgumentException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return null;
        }
    }

    private static bool TryGetClientBounds(nint windowHandle, out PixelRect bounds)
    {
        bounds = default;
        if (!NativeMethods.GetClientRect(windowHandle, out var rectangle))
        {
            return false;
        }

        var topLeft = new NativePoint(rectangle.Left, rectangle.Top);
        var bottomRight = new NativePoint(rectangle.Right, rectangle.Bottom);
        if (!NativeMethods.ClientToScreen(windowHandle, ref topLeft) ||
            !NativeMethods.ClientToScreen(windowHandle, ref bottomRight))
        {
            return false;
        }

        bounds = new PixelRect(topLeft.X, topLeft.Y, bottomRight.X, bottomRight.Y);
        return true;
    }

    private static bool IsCloaked(nint windowHandle)
    {
        return NativeMethods.DwmGetWindowAttribute(
                   windowHandle,
                   NativeMethods.DwmwaCloaked,
                   out var cloaked,
                   sizeof(int)) == 0 &&
               cloaked != 0;
    }
}
