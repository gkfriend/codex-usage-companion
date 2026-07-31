namespace CodexUsageCompanion.Lifecycle;

public enum CommandMode
{
    Unknown,
    SessionStart,
    Refresh,
    Background,
    Recovery,
    Probe,
    RenderPreview
}

public static class CommandModeParser
{
    public static CommandMode Parse(IReadOnlyList<string> arguments)
    {
        if (arguments.Count == 0)
        {
            return CommandMode.Unknown;
        }

        return arguments[0] switch
        {
            "--session-start" => CommandMode.SessionStart,
            "--refresh" => CommandMode.Refresh,
            "--background" => CommandMode.Background,
            "--recover" => CommandMode.Recovery,
            "--probe" => CommandMode.Probe,
            "--render-preview" => CommandMode.RenderPreview,
            _ => CommandMode.Unknown
        };
    }
}
