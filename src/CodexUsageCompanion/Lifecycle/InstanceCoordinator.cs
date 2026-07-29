namespace CodexUsageCompanion.Lifecycle;

public sealed class InstanceCoordinator
{
    public const string DefaultName = "Local\\CodexUsageCompanion.Resident";

    private readonly string _mutexName;
    private readonly string _eventName;

    public InstanceCoordinator(string name = DefaultName)
    {
        _mutexName = $"{name}.Mutex";
        _eventName = $"{name}.Refresh";
    }

    public ResidentLease? TryAcquireResident()
    {
        var mutex = new Mutex(true, _mutexName, out var createdNew);
        if (!createdNew)
        {
            mutex.Dispose();
            return null;
        }

        var refreshSignal = new EventWaitHandle(false, EventResetMode.AutoReset, _eventName);
        return new ResidentLease(mutex, refreshSignal);
    }

    public bool SignalRefresh()
    {
        try
        {
            using var refreshSignal = EventWaitHandle.OpenExisting(_eventName);
            return refreshSignal.Set();
        }
        catch (WaitHandleCannotBeOpenedException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }
}

public sealed class ResidentLease : IDisposable
{
    private readonly Mutex _mutex;
    private bool _disposed;

    internal ResidentLease(Mutex mutex, EventWaitHandle refreshSignal)
    {
        _mutex = mutex;
        RefreshSignal = refreshSignal;
    }

    public EventWaitHandle RefreshSignal { get; }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        RefreshSignal.Dispose();
        _mutex.ReleaseMutex();
        _mutex.Dispose();
    }
}
