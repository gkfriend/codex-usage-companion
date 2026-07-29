using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CodexUsageCompanion.Configuration;
using CodexUsageCompanion.Localization;
using CodexUsageCompanion.RateLimits;

namespace CodexUsageCompanion.Ui;

public static class UsagePreviewRenderer
{
    public static void Render(string outputPath, string? language = null)
    {
        var fiveHourReset = new DateTimeOffset(2026, 7, 10, 23, 33, 0, TimeSpan.FromHours(8));
        var weeklyReset = new DateTimeOffset(2026, 7, 17, 0, 0, 0, TimeSpan.FromHours(8));
        var state = new RateLimitState(
            new RateLimitWindowState(51, 300, fiveHourReset.ToUnixTimeSeconds()),
            new RateLimitWindowState(58, 10080, weeklyReset.ToUnixTimeSeconds()),
            null);
        var settings = new CompanionSettings { Language = language ?? "auto" };
        var text = UiText.For(UiLanguageResolver.Resolve(settings.Language, System.Globalization.CultureInfo.CurrentUICulture));
        var window = new UsageOverlayWindow(settings, text);
        window.UpdateUsage(state);
        var content = (FrameworkElement)window.Content;
        var size = new Size(window.Width, window.Height);
        content.Measure(size);
        content.Arrange(new Rect(size));
        content.UpdateLayout();

        var bitmap = new RenderTargetBitmap(
            (int)window.Width,
            (int)window.Height,
            96,
            96,
            PixelFormats.Pbgra32);
        bitmap.Render(content);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var stream = File.Create(outputPath);
        encoder.Save(stream);
    }
}
