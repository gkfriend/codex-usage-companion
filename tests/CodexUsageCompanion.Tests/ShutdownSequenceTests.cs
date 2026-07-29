using CodexUsageCompanion.Lifecycle;
using Xunit;

namespace CodexUsageCompanion.Tests;

public sealed class ShutdownSequenceTests
{
    [Fact]
    public async Task UiCleanupRunsBeforeWaitingForBackgroundWork()
    {
        var backgroundCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var uiCleanupRan = false;

        var shutdown = ShutdownSequence.RunAsync(
            () => uiCleanupRan = true,
            () => backgroundCompletion.Task,
            () => ValueTask.CompletedTask);

        Assert.True(uiCleanupRan);
        Assert.False(shutdown.IsCompleted);

        backgroundCompletion.SetResult();
        await shutdown;
    }
}
