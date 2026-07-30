namespace CodexUsageCompanion.Lifecycle;

public static class UsageRefreshPolicy
{
    public static bool ShouldShowUnavailable(bool hasUsageState) => !hasUsageState;
}
