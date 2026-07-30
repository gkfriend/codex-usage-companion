namespace CodexUsageCompanion.Lifecycle;

public static class ResidentStartupPolicy
{
    public static TimeSpan SignalWaitTimeout => TimeSpan.FromSeconds(8);
}
