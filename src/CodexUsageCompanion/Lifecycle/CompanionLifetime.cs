namespace CodexUsageCompanion.Lifecycle;

public static class CompanionLifetime
{
    public static readonly TimeSpan MissingWindowGracePeriod = TimeSpan.FromSeconds(30);

    public static bool ShouldExit(DateTimeOffset missingSince, DateTimeOffset now)
    {
        return now - missingSince >= MissingWindowGracePeriod;
    }
}
