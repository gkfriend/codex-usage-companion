using System.Windows;
using System.Windows.Threading;
using CodexUsageCompanion.Diagnostics;
using CodexUsageCompanion.Configuration;
using CodexUsageCompanion.Localization;
using CodexUsageCompanion.RateLimits;
using CodexUsageCompanion.Ui;
using CodexUsageCompanion.Windows;

namespace CodexUsageCompanion.Lifecycle;

public sealed class CompanionRuntime : IAsyncDisposable
{
    private readonly ResidentLease _lease;
    private readonly CodexWindowLocator _windowLocator = new();
    private readonly CodexAppServerClient _appServerClient = new();
    private readonly UsageOverlayWindow _window;
    private readonly CancellationTokenSource _cancellation = new();
    private readonly RefreshCoordinator _refreshCoordinator;
    private readonly DispatcherTimer _windowTimer;
    private readonly DispatcherTimer _fallbackRefreshTimer;
    private Application? _application;
    private Task? _refreshSignalTask;
    private DateTimeOffset? _codexMissingSince;
    private bool _hasUsageState;
    private bool _windowUnavailable;

    public CompanionRuntime(ResidentLease lease, CompanionSettings? settings = null, UiText? text = null)
    {
        _lease = lease;
        _window = new UsageOverlayWindow(settings, text);
        _refreshCoordinator = new RefreshCoordinator(RefreshUsageAsync, RefreshPolicy.MinimumInterval);
        _windowTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _fallbackRefreshTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = RefreshPolicy.MinimumInterval
        };
        _windowTimer.Tick += HandleWindowTimer;
        _fallbackRefreshTimer.Tick += HandleFallbackRefresh;
        _appServerClient.RateLimitsChanged += HandleRateLimitsChanged;
    }

    public void Start(Application application)
    {
        _application = application;
        CompanionLog.Shared.Write("lifecycle", "resident-started");
        _windowTimer.Start();
        _fallbackRefreshTimer.Start();
        _refreshSignalTask = Task.Run(WatchRefreshSignal);
        HandleWindowTimer(this, EventArgs.Empty);
        _refreshCoordinator.Request();
    }

    public async ValueTask DisposeAsync()
    {
        await ShutdownSequence.RunAsync(
            () =>
            {
                _windowTimer.Stop();
                _fallbackRefreshTimer.Stop();
                _appServerClient.RateLimitsChanged -= HandleRateLimitsChanged;
                _cancellation.Cancel();
                _window.Close();
            },
            async () =>
            {
                if (_refreshSignalTask is null)
                {
                    return;
                }

                try
                {
                    await _refreshSignalTask;
                }
                catch (OperationCanceledException)
                {
                }
            },
            async () =>
            {
                await _refreshCoordinator.DisposeAsync();
                await _appServerClient.DisposeAsync();
                _cancellation.Dispose();
                CompanionLog.Shared.Write("lifecycle", "resident-stopped");
            });
    }

    private void HandleWindowTimer(object? sender, EventArgs eventArgs)
    {
        var owner = _windowLocator.Find();
        if (owner is not null)
        {
            if (_windowUnavailable)
            {
                CompanionLog.Shared.Write("lifecycle", "window-restored");
            }

            _windowUnavailable = false;
            _codexMissingSince = null;
            _window.AttachAndPosition(owner);
            return;
        }

        _window.Hide();
        if (!_windowUnavailable)
        {
            _windowUnavailable = true;
            CompanionLog.Shared.Write("lifecycle", "window-unavailable");
        }

        var now = DateTimeOffset.UtcNow;
        var codexRunning = _windowLocator.IsCodexRunning();
        var previousMissingSince = _codexMissingSince;
        var lifetime = CompanionLifetime.Evaluate(codexRunning, previousMissingSince, now);
        _codexMissingSince = lifetime.ProcessMissingSince;
        if (codexRunning)
        {
            return;
        }

        if (previousMissingSince is null)
        {
            CompanionLog.Shared.Write("lifecycle", "codex-process-missing");
        }

        if (lifetime.ShouldExit)
        {
            CompanionLog.Shared.Write("lifecycle", "resident-exit reason=codex-process-missing");
            _application?.Shutdown();
        }
    }

    private void HandleFallbackRefresh(object? sender, EventArgs eventArgs)
    {
        _refreshCoordinator.Request();
    }

    private void HandleRateLimitsChanged(RateLimitState state)
    {
        _ = ApplyRateLimitsChangedAsync(state);
    }

    private async Task ApplyRateLimitsChangedAsync(RateLimitState state)
    {
        try
        {
            await UpdateWindowAsync(state);
            _hasUsageState = true;
        }
        catch (Exception exception)
        {
            CompanionLog.Shared.Write("rate-limit-notification", exception);
        }
    }

    private async Task WatchRefreshSignal()
    {
        while (!_cancellation.IsCancellationRequested)
        {
            if (!_lease.RefreshSignal.WaitOne(500))
            {
                continue;
            }

            var application = _application;
            if (application is not null)
            {
                _refreshCoordinator.Request();
            }
        }
    }

    private async Task RefreshUsageAsync(CancellationToken cancellationToken)
    {
        try
        {
            var state = await _appServerClient.ReadRateLimitsAsync(cancellationToken);
            await UpdateWindowAsync(state);
            _hasUsageState = true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            CompanionLog.Shared.Write("refresh", exception);
            if (UsageRefreshPolicy.ShouldShowUnavailable(_hasUsageState))
            {
                await UpdateWindowAsync(null);
            }
        }
    }

    private async Task UpdateWindowAsync(RateLimitState? state)
    {
        var application = _application;
        if (application is null || application.Dispatcher.CheckAccess())
        {
            _window.UpdateUsage(state);
            return;
        }

        await application.Dispatcher.InvokeAsync(() => _window.UpdateUsage(state));
    }
}
