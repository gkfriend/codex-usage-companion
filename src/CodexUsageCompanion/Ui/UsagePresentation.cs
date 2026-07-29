using CodexUsageCompanion.Localization;

namespace CodexUsageCompanion.Ui;

public enum UsageSignal
{
    Gray,
    Red,
    Orange,
    Yellow,
    Green
}

public static class UsagePresentation
{
    public static UsageSignal GetSignal(int remainingPercent)
    {
        var remaining = Math.Clamp(remainingPercent, 0, 100);
        return remaining switch
        {
            0 => UsageSignal.Gray,
            < 40 => UsageSignal.Red,
            < 60 => UsageSignal.Orange,
            < 80 => UsageSignal.Yellow,
            _ => UsageSignal.Green
        };
    }

    public static double[] GetCellFillRatios(int remainingPercent)
    {
        var remaining = Math.Clamp(remainingPercent, 0, 100);
        return Enumerable.Range(0, 5)
            .Select(index => Math.Clamp((remaining - index * 20) / 20d, 0d, 1d))
            .ToArray();
    }

    public static string FormatFiveHourReset(DateTimeOffset localReset)
    {
        return UiText.For(UiLanguage.TraditionalChinese).FormatFiveHourReset(localReset);
    }

    public static string FormatWeeklyReset(DateTimeOffset localReset)
    {
        return UiText.For(UiLanguage.TraditionalChinese).FormatWeeklyReset(localReset);
    }
}
