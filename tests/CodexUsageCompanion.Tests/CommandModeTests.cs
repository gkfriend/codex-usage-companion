using CodexUsageCompanion.Lifecycle;
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
    public void DetachedLauncherCreatesBreakawayBackgroundRequestInsideJob()
    {
        var request = DetachedLauncher.CreateRequest(@"C:\Tools\CodexUsageCompanion.exe", true);

        Assert.Equal(@"C:\Tools\CodexUsageCompanion.exe", request.ExecutablePath);
        Assert.Equal("\"C:\\Tools\\CodexUsageCompanion.exe\" --background", request.CommandLine);
        Assert.Equal(@"C:\Tools", request.WorkingDirectory);
        Assert.True(request.CreationFlags.HasFlag(DetachedProcessCreationFlags.CreateNoWindow));
        Assert.True(request.CreationFlags.HasFlag(DetachedProcessCreationFlags.CreateBreakawayFromJob));
    }

    [Fact]
    public void DetachedLauncherOmitsBreakawayFlagOutsideJob()
    {
        var request = DetachedLauncher.CreateRequest(@"C:\Tools\CodexUsageCompanion.exe", false);

        Assert.True(request.CreationFlags.HasFlag(DetachedProcessCreationFlags.CreateNoWindow));
        Assert.False(request.CreationFlags.HasFlag(DetachedProcessCreationFlags.CreateBreakawayFromJob));
    }
}
