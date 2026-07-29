using System.IO;
using System.Text.Json;

namespace CodexUsageCompanion.Configuration;

public static class CompanionSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static CompanionSettings Load(string? path = null)
    {
        var settingsPath = path ?? GetDefaultPath();
        try
        {
            if (!File.Exists(settingsPath))
            {
                var defaults = new CompanionSettings();
                var directory = Path.GetDirectoryName(settingsPath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(settingsPath, JsonSerializer.Serialize(defaults, JsonOptions));
                return defaults;
            }

            var settings = JsonSerializer.Deserialize<CompanionSettings>(File.ReadAllText(settingsPath), JsonOptions);
            return Normalize(settings ?? new CompanionSettings());
        }
        catch (JsonException)
        {
            return new CompanionSettings();
        }
        catch (IOException)
        {
            return new CompanionSettings();
        }
        catch (UnauthorizedAccessException)
        {
            return new CompanionSettings();
        }
    }

    public static string GetDefaultPath()
    {
        var pluginData = Environment.GetEnvironmentVariable("PLUGIN_DATA");
        var directory = string.IsNullOrWhiteSpace(pluginData)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CodexUsageCompanion")
            : pluginData;
        return Path.Combine(directory, "settings.json");
    }

    private static CompanionSettings Normalize(CompanionSettings settings)
    {
        var language = settings.Language is "auto" or "en" or "zh-Hant" or "zh-Hans"
            ? settings.Language
            : "auto";
        var position = settings.Position is "top-left" or "top-right" or "bottom-left" or "bottom-right"
            ? settings.Position
            : "bottom-right";
        return settings with
        {
            Language = language,
            Position = position,
            Opacity = Math.Clamp(settings.Opacity, 0.5d, 1d),
            Margin = Math.Clamp(settings.Margin, 0, 64)
        };
    }
}
