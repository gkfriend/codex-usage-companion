namespace CodexUsageCompanion.Configuration;

public sealed record CompanionSettings
{
    public bool ShowFiveHourLimit { get; init; }
    public string Language { get; init; } = "auto";
    public string Position { get; init; } = "bottom-right";
    public double Opacity { get; init; } = 1d;
    public int Margin { get; init; } = 16;
}
