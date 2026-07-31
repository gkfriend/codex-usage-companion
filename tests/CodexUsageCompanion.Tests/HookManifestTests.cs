using System.Text.Json;
using Xunit;

namespace CodexUsageCompanion.Tests;

public sealed class HookManifestTests
{
    [Fact]
    public void ManifestRecoversOnSessionAndPromptThenRefreshesAfterResponse()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "hooks.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var hooks = document.RootElement.GetProperty("hooks");

        AssertCommand(hooks, "SessionStart", "--session-start");
        AssertCommand(hooks, "UserPromptSubmit", "--session-start");
        AssertCommand(hooks, "Stop", "--refresh");
    }

    private static void AssertCommand(JsonElement hooks, string eventName, string argument)
    {
        var groups = hooks.GetProperty(eventName);
        Assert.Equal(1, groups.GetArrayLength());
        var handlers = groups[0].GetProperty("hooks");
        Assert.Equal(1, handlers.GetArrayLength());
        var handler = handlers[0];
        Assert.Equal("command", handler.GetProperty("type").GetString());
        var command = handler.GetProperty("command").GetString();
        Assert.Contains("${PLUGIN_ROOT}/bin/win-x64/CodexUsageCompanion.exe", command);
        Assert.EndsWith(argument, command);
        Assert.Equal(10, handler.GetProperty("timeout").GetInt32());
    }
}
