using CodexUsageCompanion.Lifecycle;
using Xunit;

namespace CodexUsageCompanion.Tests;

public sealed class RefreshCoordinatorTests
{
    [Fact]
    public async Task RequestsDuringRefreshAreCoalescedIntoOneFollowUp()
    {
        var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        var concurrent = 0;
        var maximumConcurrent = 0;
        await using var coordinator = new RefreshCoordinator(async cancellationToken =>
        {
            var current = Interlocked.Increment(ref concurrent);
            maximumConcurrent = Math.Max(maximumConcurrent, current);
            var call = Interlocked.Increment(ref calls);
            if (call == 1)
            {
                firstStarted.SetResult();
                await releaseFirst.Task.WaitAsync(cancellationToken);
            }
            else
            {
                secondCompleted.SetResult();
            }

            Interlocked.Decrement(ref concurrent);
        });

        coordinator.Request();
        await firstStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        coordinator.Request();
        coordinator.Request();
        releaseFirst.SetResult();
        await secondCompleted.Task.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Equal(2, calls);
        Assert.Equal(1, maximumConcurrent);
    }

    [Fact]
    public async Task DisposeWaitsForActiveRefresh()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var coordinator = new RefreshCoordinator(async _ =>
        {
            started.SetResult();
            await release.Task;
        });
        coordinator.Request();
        await started.Task.WaitAsync(TimeSpan.FromSeconds(2));

        var dispose = coordinator.DisposeAsync().AsTask();

        Assert.False(dispose.IsCompleted);
        release.SetResult();
        await dispose.WaitAsync(TimeSpan.FromSeconds(2));
    }
}
