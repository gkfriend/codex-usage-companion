using CodexUsageCompanion.Lifecycle;
using System.Diagnostics;
using Xunit;

namespace CodexUsageCompanion.Tests;

public sealed class CommandModeTests
{
    [Theory]
    [InlineData("--session-start", CommandMode.SessionStart)]
    [InlineData("--refresh", CommandMode.Refresh)]
    [InlineData("--background", CommandMode.Background)]
    [InlineData("--probe", CommandMode.Probe)]
    [InlineData("--render-preview", CommandMode.RenderPreview)]
    public void ParseRecognizesSupportedMode(string argument, CommandMode expected)
    {
        Assert.Equal(expected, CommandModeParser.Parse([argument]));
    }

    [Fact]
    public void ParseRejectsMissingOrUnknownMode()
    {
        Assert.Equal(CommandMode.Unknown, CommandModeParser.Parse([]));
        Assert.Equal(CommandMode.Unknown, CommandModeParser.Parse(["--other"]));
    }

    [Fact]
    public void DetachedLauncherCreatesHiddenBackgroundProcess()
    {
        var startInfo = DetachedLauncher.CreateStartInfo(@"C:\Tools\CodexUsageCompanion.exe");

        Assert.Equal(@"C:\Tools\CodexUsageCompanion.exe", startInfo.FileName);
        Assert.Equal("--background", startInfo.Arguments);
        Assert.True(startInfo.UseShellExecute);
        Assert.Equal(ProcessWindowStyle.Hidden, startInfo.WindowStyle);
    }
}
