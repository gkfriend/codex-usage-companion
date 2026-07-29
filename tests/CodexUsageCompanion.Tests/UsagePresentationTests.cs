using CodexUsageCompanion.Ui;
using Xunit;

namespace CodexUsageCompanion.Tests;

public sealed class UsagePresentationTests
{
    [Theory]
    [InlineData(0, UsageSignal.Gray)]
    [InlineData(1, UsageSignal.Red)]
    [InlineData(39, UsageSignal.Red)]
    [InlineData(40, UsageSignal.Orange)]
    [InlineData(59, UsageSignal.Orange)]
    [InlineData(60, UsageSignal.Yellow)]
    [InlineData(79, UsageSignal.Yellow)]
    [InlineData(80, UsageSignal.Green)]
    [InlineData(100, UsageSignal.Green)]
    public void GetSignalUsesApprovedThresholds(int remainingPercent, UsageSignal expected)
    {
        Assert.Equal(expected, UsagePresentation.GetSignal(remainingPercent));
    }

    [Fact]
    public void GetCellFillRatiosUsesFiveTwentyPercentCells()
    {
        var ratios = UsagePresentation.GetCellFillRatios(51);

        Assert.Equal(new[] { 1d, 1d, 0.55d, 0d, 0d }, ratios);
    }

    [Fact]
    public void FormatFiveHourResetUsesTraditionalChineseDayPeriod()
    {
        var reset = new DateTimeOffset(2026, 7, 10, 23, 33, 0, TimeSpan.FromHours(8));

        Assert.Equal("於 晚上11:33 重置", UsagePresentation.FormatFiveHourReset(reset));
    }

    [Fact]
    public void FormatWeeklyResetUsesMonthAndDay()
    {
        var reset = new DateTimeOffset(2026, 7, 17, 9, 0, 0, TimeSpan.FromHours(8));

        Assert.Equal("於 7月17日 重置", UsagePresentation.FormatWeeklyReset(reset));
    }
}
