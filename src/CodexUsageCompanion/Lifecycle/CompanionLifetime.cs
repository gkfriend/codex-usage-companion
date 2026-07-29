namespace CodexUsageCompanion.Lifecycle;

public readonly record struct CompanionLifetimeState(
    DateTimeOffset? ProcessMissingSince,
    bool ShouldExit);

public static class CompanionLifetime
{
    public static readonly TimeSpan MissingWindowGracePeriod = TimeSpan.FromSeconds(30);

    public static CompanionLifetimeState Evaluate(
        bool isCodexRunning,
        DateTimeOffset? processMissingSince,
        DateTimeOffset now)
    {
        if (isCodexRunning)
        {
            return new CompanionLifetimeState(null, false);
        }

        var missingSince = processMissingSince ?? now;
        return new CompanionLifetimeState(missingSince, ShouldExit(missingSince, now));
    }

    public static bool ShouldExit(DateTimeOffset missingSince, DateTimeOffset now)
    {
        return now - missingSince >= MissingWindowGracePeriod;
    }
}
