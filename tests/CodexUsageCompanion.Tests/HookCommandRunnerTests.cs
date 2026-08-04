using CodexUsageCompanion.Lifecycle;
using Xunit;

namespace CodexUsageCompanion.Tests;

public sealed class HookCommandRunnerTests
{
    [Fact]
    public void CliOnlyInvocationSucceedsWithJsonWithoutSignalingOrLaunching()
    {
        using var output = new StringWriter();
        var signaled = false;
        var launched = false;
        var runner = new HookCommandRunner(
            () => false,
            () => signaled = true,
            () => launched = true,
            output);

        var exitCode = runner.Run(true);

        Assert.Equal(0, exitCode);
        Assert.Equal($"{{}}{Environment.NewLine}", output.ToString());
        Assert.False(signaled);
        Assert.False(launched);
    }

    [Fact]
    public void ExistingDesktopResidentIsSignaledWithoutLaunching()
    {
        using var output = new StringWriter();
        var launched = false;
        var runner = new HookCommandRunner(
            () => true,
            () => true,
            () => launched = true,
            output);

        var exitCode = runner.Run(true);

        Assert.Equal(0, exitCode);
        Assert.False(launched);
    }

    [Fact]
    public void SessionHookLaunchesMissingDesktopResidentWithoutWaiting()
    {
        using var output = new StringWriter();
        var launched = false;
        var runner = new HookCommandRunner(
            () => true,
            () => false,
            () => launched = true,
            output);

        var exitCode = runner.Run(true);

        Assert.Equal(0, exitCode);
        Assert.True(launched);
        Assert.Equal($"{{}}{Environment.NewLine}", output.ToString());
    }

    [Fact]
    public void StopHookDoesNotLaunchMissingResident()
    {
        using var output = new StringWriter();
        var launched = false;
        var runner = new HookCommandRunner(
            () => true,
            () => false,
            () => launched = true,
            output);

        var exitCode = runner.Run(false);

        Assert.Equal(0, exitCode);
        Assert.False(launched);
        Assert.Equal($"{{}}{Environment.NewLine}", output.ToString());
    }

    [Fact]
    public void HookOperationFailureStillSucceedsWithJson()
    {
        using var output = new StringWriter();
        var runner = new HookCommandRunner(
            () => throw new InvalidOperationException("failure"),
            () => false,
            () => false,
            output);

        var exitCode = runner.Run(true);

        Assert.Equal(0, exitCode);
        Assert.Equal($"{{}}{Environment.NewLine}", output.ToString());
    }
}
