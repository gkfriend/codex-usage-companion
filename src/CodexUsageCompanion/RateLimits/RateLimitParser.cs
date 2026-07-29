using System.Text.Json;

namespace CodexUsageCompanion.RateLimits;

public static class RateLimitParser
{
    private const int FiveHourDurationMins = 300;
    private const int WeeklyDurationMins = 10080;

    public static RateLimitState ParseResponse(string json)
    {
        using var document = JsonDocument.Parse(json);
        var result = document.RootElement.GetProperty("result");
        var limits = SelectSnapshot(result, out var allowLegacyPositionFallback);
        var primary = ParseWindow(limits, "primary");
        var secondary = ParseWindow(limits, "secondary");
        var fiveHour = FindWindow(primary, secondary, FiveHourDurationMins);
        var weekly = FindWindow(primary, secondary, WeeklyDurationMins);

        if (allowLegacyPositionFallback)
        {
            if (primary?.WindowDurationMins is null)
            {
                fiveHour ??= primary;
            }

            if (secondary?.WindowDurationMins is null)
            {
                weekly ??= secondary;
            }
        }

        var credits = ParseResetCredits(result);
        return new RateLimitState(fiveHour, weekly, credits);
    }

    private static JsonElement SelectSnapshot(JsonElement result, out bool allowLegacyPositionFallback)
    {
        if (result.TryGetProperty("rateLimitsByLimitId", out var buckets) &&
            buckets.ValueKind == JsonValueKind.Object &&
            buckets.TryGetProperty("codex", out var codex) &&
            codex.ValueKind == JsonValueKind.Object)
        {
            allowLegacyPositionFallback = false;
            return codex;
        }

        allowLegacyPositionFallback = true;
        return result.GetProperty("rateLimits");
    }

    private static RateLimitWindowState? FindWindow(
        RateLimitWindowState? primary,
        RateLimitWindowState? secondary,
        int durationMins)
    {
        if (primary?.WindowDurationMins == durationMins)
        {
            return primary;
        }

        return secondary?.WindowDurationMins == durationMins ? secondary : null;
    }

    private static RateLimitWindowState? ParseWindow(JsonElement limits, string name)
    {
        if (!limits.TryGetProperty(name, out var window) || window.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        var usedPercent = window.GetProperty("usedPercent").GetInt32();
        var remainingPercent = 100 - Math.Clamp(usedPercent, 0, 100);
        int? duration = window.TryGetProperty("windowDurationMins", out var durationElement) &&
                        durationElement.ValueKind == JsonValueKind.Number
            ? durationElement.GetInt32()
            : null;
        long? resetsAt = window.TryGetProperty("resetsAt", out var resetElement) &&
                         resetElement.ValueKind == JsonValueKind.Number
            ? resetElement.GetInt64()
            : null;

        return new RateLimitWindowState(remainingPercent, duration, resetsAt);
    }

    private static int? ParseResetCredits(JsonElement result)
    {
        if (!result.TryGetProperty("rateLimitResetCredits", out var credits) ||
            credits.ValueKind == JsonValueKind.Null ||
            !credits.TryGetProperty("availableCount", out var count) ||
            count.ValueKind != JsonValueKind.Number)
        {
            return null;
        }

        return count.GetInt32();
    }
}
