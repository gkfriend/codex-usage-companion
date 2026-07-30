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
    public void DetachedLauncherCreatesBreakawayBackgroundRequestWhenAllowed()
    {
        var request = DetachedLauncher.CreateRequest(
            @"C:\Tools\CodexUsageCompanion.exe",
            JobBreakawayPolicy.ExplicitBreakawayAllowed);

        Assert.Equal(@"C:\Tools\CodexUsageCompanion.exe", request.ExecutablePath);
        Assert.Equal("\"C:\\Tools\\CodexUsageCompanion.exe\" --background", request.CommandLine);
        Assert.Equal(@"C:\Tools", request.WorkingDirectory);
        Assert.True(request.CreationFlags.HasFlag(DetachedProcessCreationFlags.CreateNoWindow));
        Assert.True(request.CreationFlags.HasFlag(DetachedProcessCreationFlags.CreateBreakawayFromJob));
    }

    [Theory]
    [InlineData(JobBreakawayPolicy.OutsideJob)]
    [InlineData(JobBreakawayPolicy.SilentBreakaway)]
    [InlineData(JobBreakawayPolicy.Restricted)]
    public void DetachedLauncherOmitsExplicitBreakawayWhenNotRequiredOrDenied(JobBreakawayPolicy policy)
    {
        var request = DetachedLauncher.CreateRequest(@"C:\Tools\CodexUsageCompanion.exe", policy);

        Assert.True(request.CreationFlags.HasFlag(DetachedProcessCreationFlags.CreateNoWindow));
        Assert.False(request.CreationFlags.HasFlag(DetachedProcessCreationFlags.CreateBreakawayFromJob));
    }

    [Theory]
    [InlineData(false, 0u, JobBreakawayPolicy.OutsideJob)]
    [InlineData(true, 0x00000800u, JobBreakawayPolicy.ExplicitBreakawayAllowed)]
    [InlineData(true, 0x00001000u, JobBreakawayPolicy.SilentBreakaway)]
    [InlineData(true, 0u, JobBreakawayPolicy.Restricted)]
    public void DetachedLauncherClassifiesJobBreakawayPolicy(
        bool isInJob,
        uint limitFlags,
        JobBreakawayPolicy expected)
    {
        Assert.Equal(expected, DetachedLauncher.ClassifyJobPolicy(isInJob, limitFlags));
    }
}
