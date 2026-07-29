using CodexUsageCompanion.Configuration;
using Xunit;

namespace CodexUsageCompanion.Tests;

public sealed class CompanionSettingsTests
{
    [Fact]
    public void LoadCreatesDefaultsWhenSettingsFileIsMissing()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"CodexUsageCompanion.Tests.{Guid.NewGuid():N}");
        var path = Path.Combine(directory, "settings.json");
        try
        {
            var settings = CompanionSettingsStore.Load(path);

            Assert.False(settings.ShowFiveHourLimit);
            Assert.Equal("auto", settings.Language);
            Assert.Equal("bottom-right", settings.Position);
            Assert.Equal(1d, settings.Opacity);
            Assert.Equal(16, settings.Margin);
            Assert.True(File.Exists(path));
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }

    [Fact]
    public void LoadReadsAndNormalizesUserSettings()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"CodexUsageCompanion.Tests.{Guid.NewGuid():N}");
        var path = Path.Combine(directory, "settings.json");
        Directory.CreateDirectory(directory);
        File.WriteAllText(path, """
        {
          "showFiveHourLimit": true,
          "language": "zh-Hans",
          "position": "top-left",
          "opacity": 1.5,
          "margin": 200
        }
        """);
        try
        {
            var settings = CompanionSettingsStore.Load(path);

            Assert.True(settings.ShowFiveHourLimit);
            Assert.Equal("zh-Hans", settings.Language);
            Assert.Equal("top-left", settings.Position);
            Assert.Equal(1d, settings.Opacity);
            Assert.Equal(64, settings.Margin);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void LoadFallsBackSafelyForInvalidJson()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"CodexUsageCompanion.Tests.{Guid.NewGuid():N}");
        var path = Path.Combine(directory, "settings.json");
        Directory.CreateDirectory(directory);
        File.WriteAllText(path, "{");
        try
        {
            var settings = CompanionSettingsStore.Load(path);

            Assert.Equal(new CompanionSettings(), settings);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }
}
