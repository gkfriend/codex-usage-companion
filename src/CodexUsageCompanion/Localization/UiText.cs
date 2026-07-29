using System.Globalization;

namespace CodexUsageCompanion.Localization;

public sealed record UiText(
    UiLanguage Language,
    string FiveHourTitle,
    string WeeklyTitle,
    string WaitingForData,
    string LimitUnavailable,
    string ResetUnavailable)
{
    public static UiText For(UiLanguage language)
    {
        return language switch
        {
            UiLanguage.TraditionalChinese => new UiText(
                language,
                "5 小時使用量限制",
                "每週使用上限",
                "等待使用量資料",
                "目前方案未提供此額度",
                "重置時間未提供"),
            UiLanguage.SimplifiedChinese => new UiText(
                language,
                "5 小时使用量限制",
                "每周使用上限",
                "等待使用量数据",
                "当前方案未提供此额度",
                "未提供重置时间"),
            _ => new UiText(
                language,
                "5-hour usage limit",
                "Weekly usage limit",
                "Waiting for usage data",
                "Not available on this plan",
                "Reset time unavailable")
        };
    }

    public string FormatRemaining(int remainingPercent)
    {
        return Language == UiLanguage.English
            ? $"{remainingPercent}% remaining"
            : Language == UiLanguage.SimplifiedChinese
                ? $"剩余 {remainingPercent}%"
                : $"剩餘 {remainingPercent}%";
    }

    public string RemainingUnavailable => Language == UiLanguage.English
        ? "-- remaining"
        : Language == UiLanguage.SimplifiedChinese
            ? "剩余 --"
            : "剩餘 --";

    public string FormatFiveHourReset(DateTimeOffset localReset)
    {
        if (Language == UiLanguage.English)
        {
            return $"Resets at {localReset.ToString("h:mm tt", CultureInfo.GetCultureInfo("en-US"))}";
        }

        var period = localReset.Hour switch
        {
            < 6 => "凌晨",
            < 12 => "上午",
            < 18 => "下午",
            _ => "晚上"
        };
        var hour = localReset.Hour % 12;
        if (hour == 0)
        {
            hour = 12;
        }

        var prefix = Language == UiLanguage.SimplifiedChinese ? "于" : "於";
        return $"{prefix} {period}{hour}:{localReset.Minute:00} 重置";
    }

    public string FormatWeeklyReset(DateTimeOffset localReset)
    {
        if (Language == UiLanguage.English)
        {
            return $"Resets {localReset.ToString("MMM d", CultureInfo.GetCultureInfo("en-US"))}";
        }

        var prefix = Language == UiLanguage.SimplifiedChinese ? "于" : "於";
        return $"{prefix} {localReset.Month}月{localReset.Day}日 重置";
    }
}
