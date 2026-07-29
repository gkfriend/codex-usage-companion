using CodexUsageCompanion.Lifecycle;
using Xunit;

namespace CodexUsageCompanion.Tests;

public sealed class CompanionLifetimeTests
{
    [Fact]
    public void KeepsResidentAliveWhileCodexProcessStillRuns()
    {
        var missingSince = new DateTimeOffset(2026, 7, 30, 1, 0, 0, TimeSpan.Zero);

        var state = CompanionLifetime.Evaluate(true, missingSince, missingSince.AddMinutes(10));

        Assert.Null(state.ProcessMissingSince);
        Assert.False(state.ShouldExit);
    }

    [Fact]
    public void StartsGracePeriodWhenCodexProcessStops()
    {
        var now = new DateTimeOffset(2026, 7, 30, 1, 0, 0, TimeSpan.Zero);

        var state = CompanionLifetime.Evaluate(false, null, now);

        Assert.Equal(now, state.ProcessMissingSince);
        Assert.False(state.ShouldExit);
    }

    [Fact]
    public void ExitsAfterCodexProcessIsMissingForThirtySeconds()
    {
        var missingSince = new DateTimeOffset(2026, 7, 30, 1, 0, 0, TimeSpan.Zero);

        var state = CompanionLifetime.Evaluate(false, missingSince, missingSince.AddSeconds(30));

        Assert.Equal(missingSince, state.ProcessMissingSince);
        Assert.True(state.ShouldExit);
    }

}
