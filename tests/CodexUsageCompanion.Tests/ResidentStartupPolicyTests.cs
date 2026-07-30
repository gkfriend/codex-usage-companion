using CodexUsageCompanion.Lifecycle;
using Xunit;

namespace CodexUsageCompanion.Tests;

public sealed class ResidentStartupPolicyTests
{
    [Fact]
    public void StartupSignalWaitUsesMostOfStopHookTimeout()
    {
        var timeout = ResidentStartupPolicy.SignalWaitTimeout;

        Assert.True(timeout >= TimeSpan.FromSeconds(8));
        Assert.True(timeout < TimeSpan.FromSeconds(10));
    }
}
