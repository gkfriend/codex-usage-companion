using CodexUsageCompanion.Lifecycle;
using Xunit;

namespace CodexUsageCompanion.Tests;

public sealed class CompanionLifetimeTests
{
    [Fact]
    public void ShouldExitAfterCodexWindowIsMissingForThirtySeconds()
    {
        var missingSince = new DateTimeOffset(2026, 7, 11, 0, 0, 0, TimeSpan.Zero);

        Assert.False(CompanionLifetime.ShouldExit(missingSince, missingSince.AddSeconds(29)));
        Assert.True(CompanionLifetime.ShouldExit(missingSince, missingSince.AddSeconds(30)));
    }
}
