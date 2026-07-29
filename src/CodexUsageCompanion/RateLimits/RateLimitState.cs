namespace CodexUsageCompanion.RateLimits;

public sealed record RateLimitWindowState(
    int RemainingPercent,
    int? WindowDurationMins,
    long? ResetsAt);

public sealed record RateLimitState(
    RateLimitWindowState? FiveHour,
    RateLimitWindowState? Weekly,
    int? AvailableResetCredits);
