namespace CodexUsageCompanion.Lifecycle;

public sealed class RefreshCoordinator : IAsyncDisposable
{
    private readonly Func<CancellationToken, Task> _refresh;
    private readonly SemaphoreSlim _signal = new(0, 1);
    private readonly CancellationTokenSource _cancellation = new();
    private readonly Task _worker;
    private readonly TimeSpan _minimumInterval;
    private readonly Func<DateTimeOffset> _utcNow;
    private readonly object _requestSync = new();
    private DateTimeOffset? _lastAcceptedAt;
    private int _pending;
    private int _disposed;

    public RefreshCoordinator(
        Func<CancellationToken, Task> refresh,
        TimeSpan? minimumInterval = null,
        Func<DateTimeOffset>? utcNow = null)
    {
        _refresh = refresh;
        _minimumInterval = minimumInterval ?? TimeSpan.Zero;
        _utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
        _worker = Task.Run(RunAsync);
    }

    public void Request()
    {
        lock (_requestSync)
        {
            if (Volatile.Read(ref _disposed) != 0 || _pending != 0)
            {
                return;
            }

            var now = _utcNow();
            if (_lastAcceptedAt is { } lastAcceptedAt && now - lastAcceptedAt < _minimumInterval)
            {
                return;
            }

            _lastAcceptedAt = now;
            _pending = 1;
        }

        try
        {
            _signal.Release();
        }
        catch (ObjectDisposedException)
        {
            lock (_requestSync)
            {
                _pending = 0;
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _cancellation.Cancel();
        try
        {
            await _worker;
        }
        catch (OperationCanceledException)
        {
        }

        _signal.Dispose();
        _cancellation.Dispose();
    }

    private async Task RunAsync()
    {
        while (true)
        {
            await _signal.WaitAsync(_cancellation.Token);
            lock (_requestSync)
            {
                _pending = 0;
            }
            await _refresh(_cancellation.Token);
        }
    }
}
