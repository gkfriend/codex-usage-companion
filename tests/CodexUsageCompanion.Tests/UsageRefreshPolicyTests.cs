using CodexUsageCompanion.Lifecycle;
using Xunit;

namespace CodexUsageCompanion.Tests;

public sealed class UsageRefreshPolicyTests
{
    [Fact]
    public void InitialFailureShowsUnavailableState()
    {
        Assert.True(UsageRefreshPolicy.ShouldShowUnavailable(false));
    }

    [Fact]
    public void LaterFailureKeepsLastKnownUsage()
    {
        Assert.False(UsageRefreshPolicy.ShouldShowUnavailable(true));
    }
}
