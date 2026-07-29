namespace CodexUsageCompanion.Lifecycle;

public static class ShutdownSequence
{
    public static async Task RunAsync(
        Action uiCleanup,
        Func<Task> drainBackground,
        Func<ValueTask> asyncCleanup)
    {
        uiCleanup();
        await drainBackground();
        await asyncCleanup();
    }
}
