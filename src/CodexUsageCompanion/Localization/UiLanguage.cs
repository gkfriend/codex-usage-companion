using System.Globalization;

namespace CodexUsageCompanion.Localization;

public enum UiLanguage
{
    English,
    TraditionalChinese,
    SimplifiedChinese
}

public static class UiLanguageResolver
{
    public static UiLanguage Resolve(string? setting, CultureInfo culture)
    {
        return setting switch
        {
            "en" => UiLanguage.English,
            "zh-Hant" => UiLanguage.TraditionalChinese,
            "zh-Hans" => UiLanguage.SimplifiedChinese,
            _ => ResolveCulture(culture)
        };
    }

    private static UiLanguage ResolveCulture(CultureInfo culture)
    {
        var name = culture.Name;
        if (name.StartsWith("zh-Hant", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("zh-TW", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("zh-HK", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("zh-MO", StringComparison.OrdinalIgnoreCase))
        {
            return UiLanguage.TraditionalChinese;
        }

        return name.StartsWith("zh", StringComparison.OrdinalIgnoreCase)
            ? UiLanguage.SimplifiedChinese
            : UiLanguage.English;
    }
}
