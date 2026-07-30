using System.Text.Json;
using Xunit;

namespace CodexUsageCompanion.Tests;

public sealed class HookManifestTests
{
    [Fact]
    public void StopHookRefreshesOrRestartsResidentAfterResponses()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "hooks.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var stop = document.RootElement
            .GetProperty("hooks")
            .GetProperty("Stop");

        var hook = Assert.Single(stop.EnumerateArray())
            .GetProperty("hooks");
        var command = Assert.Single(hook.EnumerateArray());

        Assert.Equal("command", command.GetProperty("type").GetString());
        Assert.Equal(
            "\"${PLUGIN_ROOT}/bin/win-x64/CodexUsageCompanion.exe\" --refresh",
            command.GetProperty("command").GetString());
        Assert.Equal(10, command.GetProperty("timeout").GetInt32());
    }
}
