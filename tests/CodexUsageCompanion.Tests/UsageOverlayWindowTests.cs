using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using CodexUsageCompanion.Configuration;
using CodexUsageCompanion.Localization;
using CodexUsageCompanion.RateLimits;
using CodexUsageCompanion.Ui;
using Xunit;

namespace CodexUsageCompanion.Tests;

public sealed class UsageOverlayWindowTests
{
    [Fact]
    public void FiveHourCardIsHiddenWhileWeeklyCardRemainsVisible()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var weekly = new RateLimitWindowState(60, 10080, 1785915471);
                var window = new UsageOverlayWindow(
                    new CompanionSettings(),
                    UiText.For(UiLanguage.TraditionalChinese));

                window.UpdateUsage(new RateLimitState(null, weekly, 4));

                var text = FindText(window.Content)
                    .ToArray();
                var fiveHourTitle = Assert.Single(text, block => block.Text == "5 小時使用量限制");
                var unavailable = Assert.Single(text, block => block.Text == "目前方案未提供此額度");
                var weeklyTitle = Assert.Single(text, block => block.Text == "每週使用上限");
                Assert.Equal(Visibility.Collapsed, FindContainer(fiveHourTitle).Visibility);
                Assert.Equal(Visibility.Collapsed, FindContainer(unavailable).Visibility);
                Assert.Equal(Visibility.Visible, FindContainer(weeklyTitle).Visibility);
                Assert.Equal(96, window.Height);
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure is not null)
        {
            ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }

    [Fact]
    public void SimplifiedChineseCanShowBothCards()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var window = new UsageOverlayWindow(
                    new CompanionSettings { ShowFiveHourLimit = true, Language = "zh-Hans" },
                    UiText.For(UiLanguage.SimplifiedChinese));

                var text = FindText(window.Content).ToArray();

                Assert.Contains(text, block => block.Text == "5 小时使用量限制");
                Assert.Contains(text, block => block.Text == "每周使用上限");
                Assert.Equal(178, window.Height);
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure is not null)
        {
            ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }

    private static IEnumerable<TextBlock> FindText(object? value)
    {
        if (value is TextBlock text)
        {
            yield return text;
        }

        if (value is not DependencyObject dependencyObject)
        {
            yield break;
        }

        foreach (var child in LogicalTreeHelper.GetChildren(dependencyObject).OfType<object>())
        {
            foreach (var descendant in FindText(child))
            {
                yield return descendant;
            }
        }
    }

    private static Border FindContainer(DependencyObject element)
    {
        var current = LogicalTreeHelper.GetParent(element);
        while (current is not Border)
        {
            current = LogicalTreeHelper.GetParent(current);
        }

        return (Border)current;
    }
}
