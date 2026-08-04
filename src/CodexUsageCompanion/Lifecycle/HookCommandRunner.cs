using System.IO;
using CodexUsageCompanion.Diagnostics;

namespace CodexUsageCompanion.Lifecycle;

public sealed class HookCommandRunner
{
    private readonly Func<bool> _isCodexRunning;
    private readonly Func<bool> _signalRefresh;
    private readonly Func<bool> _startResident;
    private readonly TextWriter _output;

    public HookCommandRunner(
        Func<bool> isCodexRunning,
        Func<bool> signalRefresh,
        Func<bool> startResident,
        TextWriter output)
    {
        _isCodexRunning = isCodexRunning;
        _signalRefresh = signalRefresh;
        _startResident = startResident;
        _output = output;
    }

    public int Run(bool ensureResident)
    {
        try
        {
            if (_isCodexRunning() && !_signalRefresh() && ensureResident)
            {
                _startResident();
            }
        }
        catch (Exception exception)
        {
            CompanionLog.Shared.Write("hook", exception);
        }

        _output.WriteLine("{}");
        _output.Flush();
        return 0;
    }
}
