using System.Globalization;
using CodexUsageCompanion.Localization;
using Xunit;

namespace CodexUsageCompanion.Tests;

public sealed class UiTextTests
{
    [Theory]
    [InlineData("auto", "en-US", UiLanguage.English)]
    [InlineData("auto", "zh-TW", UiLanguage.TraditionalChinese)]
    [InlineData("auto", "zh-CN", UiLanguage.SimplifiedChinese)]
    [InlineData("zh-Hant", "en-US", UiLanguage.TraditionalChinese)]
    [InlineData("zh-Hans", "en-US", UiLanguage.SimplifiedChinese)]
    public void ResolveUsesExplicitSettingOrSystemCulture(string setting, string culture, UiLanguage expected)
    {
        Assert.Equal(expected, UiLanguageResolver.Resolve(setting, CultureInfo.GetCultureInfo(culture)));
    }

    [Fact]
    public void SimplifiedChineseContainsExpectedVisibleText()
    {
        var text = UiText.For(UiLanguage.SimplifiedChinese);

        Assert.Equal("5 小时使用量限制", text.FiveHourTitle);
        Assert.Equal("每周使用上限", text.WeeklyTitle);
        Assert.Equal("剩余 58%", text.FormatRemaining(58));
        Assert.Equal("当前方案未提供此额度", text.LimitUnavailable);
    }

    [Fact]
    public void EnglishContainsExpectedVisibleText()
    {
        var text = UiText.For(UiLanguage.English);

        Assert.Equal("5-hour usage limit", text.FiveHourTitle);
        Assert.Equal("Weekly usage limit", text.WeeklyTitle);
        Assert.Equal("58% remaining", text.FormatRemaining(58));
        Assert.Equal("Not available on this plan", text.LimitUnavailable);
    }

    [Theory]
    [InlineData(UiLanguage.TraditionalChinese, "於 晚上11:33 重置", "於 7月17日 重置")]
    [InlineData(UiLanguage.SimplifiedChinese, "于 晚上11:33 重置", "于 7月17日 重置")]
    [InlineData(UiLanguage.English, "Resets at 11:33 PM", "Resets Jul 17")]
    public void ResetFormattingIsLocalized(UiLanguage language, string fiveHour, string weekly)
    {
        var text = UiText.For(language);
        var time = new DateTimeOffset(2026, 7, 10, 23, 33, 0, TimeSpan.FromHours(8));
        var date = new DateTimeOffset(2026, 7, 17, 9, 0, 0, TimeSpan.FromHours(8));

        Assert.Equal(fiveHour, text.FormatFiveHourReset(time));
        Assert.Equal(weekly, text.FormatWeeklyReset(date));
    }
}
