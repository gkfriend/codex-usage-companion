using CodexUsageCompanion.Windows;
using Xunit;

namespace CodexUsageCompanion.Tests;

public sealed class OverlayPlacementTests
{
    [Fact]
    public void ShouldApplyOnlyWhenPlacementChanges()
    {
        var current = new OverlayPlacementRequest(10, 984, 670, 300, 164);

        Assert.True(OverlayPlacement.ShouldApply(null, current));
        Assert.False(OverlayPlacement.ShouldApply(current, current));
        Assert.True(OverlayPlacement.ShouldApply(current, current with { X = 985 }));
    }

    [Fact]
    public void CodexHostCandidateAcceptsStoreAndPerUserDesktopHosts()
    {
        Assert.True(CodexWindowLocator.IsCodexHostProcess(
            "ChatGPT",
            @"C:\Program Files\WindowsApps\OpenAI.Codex_26.707.3748.0_x64__2p2nqsd0c76g0\app\ChatGPT.exe"));
        Assert.True(CodexWindowLocator.IsCodexHostProcess(
            "ChatGPT",
            @"C:\Users\Test\AppData\Local\Programs\OpenAI\Codex\app\ChatGPT.exe"));
        Assert.False(CodexWindowLocator.IsCodexHostProcess(
            "ChatGPT",
            @"C:\Tools\ChatGPT.exe"));
        Assert.False(CodexWindowLocator.IsCodexHostProcess(
            "explorer",
            @"C:\Windows\explorer.exe"));
    }

    [Fact]
    public void CodexHostCandidateAcceptsDesktopCodexAndRejectsCliProcesses()
    {
        Assert.True(CodexWindowLocator.IsCodexHostProcess(
            "codex",
            @"C:\Program Files\WindowsApps\OpenAI.Codex_26.707.3748.0_x64__2p2nqsd0c76g0\app\resources\codex.exe"));
        Assert.False(CodexWindowLocator.IsCodexHostProcess(
            "codex",
            @"C:\Users\Test\AppData\Local\Programs\OpenAI\Codex\bin\codex.exe"));
        Assert.False(CodexWindowLocator.IsCodexHostProcess(
            "codex-code-mode-host",
            @"C:\Program Files\WindowsApps\OpenAI.Codex_26.707.3748.0_x64__2p2nqsd0c76g0\app\resources\codex-code-mode-host.exe"));
    }

    [Fact]
    public void SelectPreferredUsesForegroundThenLargestWindow()
    {
        var smaller = new CodexWindowInfo(10, new PixelRect(0, 0, 800, 600), false, false);
        var larger = new CodexWindowInfo(20, new PixelRect(0, 0, 1200, 900), false, false);

        Assert.Equal(smaller, CodexWindowLocator.SelectPreferred([smaller, larger], 10));
        Assert.Equal(larger, CodexWindowLocator.SelectPreferred([smaller, larger], 99));
    }

    [Fact]
    public void CalculateAnchorsOverlayInsideLowerRightCorner()
    {
        var owner = new PixelRect(100, 50, 1300, 850);

        var point = OverlayPlacement.Calculate(owner, 300, 164, 16);

        Assert.Equal(new PixelPoint(984, 670), point);
    }

    [Fact]
    public void CalculateUsesPhysicalPixelBoundsAtHighDpi()
    {
        var owner = new PixelRect(200, 120, 2600, 1720);

        var point = OverlayPlacement.Calculate(owner, 450, 246, 24);

        Assert.Equal(new PixelPoint(2126, 1450), point);
    }

    [Theory]
    [InlineData(OverlayPosition.TopLeft, 116, 66)]
    [InlineData(OverlayPosition.TopRight, 984, 66)]
    [InlineData(OverlayPosition.BottomLeft, 116, 670)]
    [InlineData(OverlayPosition.BottomRight, 984, 670)]
    public void CalculateSupportsAllConfiguredCorners(OverlayPosition position, int x, int y)
    {
        var owner = new PixelRect(100, 50, 1300, 850);

        var point = OverlayPlacement.Calculate(owner, 300, 164, 16, position);

        Assert.Equal(new PixelPoint(x, y), point);
    }
}
