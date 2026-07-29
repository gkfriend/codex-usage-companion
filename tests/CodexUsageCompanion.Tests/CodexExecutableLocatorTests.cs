using CodexUsageCompanion.RateLimits;
using Xunit;

namespace CodexUsageCompanion.Tests;

public sealed class CodexExecutableLocatorTests
{
    [Fact]
    public void FindUsesExplicitOverrideFirst()
    {
        var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            @"D:\Codex\codex.exe",
            @"C:\Tools\codex.exe"
        };

        var result = CodexExecutableLocator.Find(
            @"D:\Codex\codex.exe",
            @"C:\Tools",
            @"C:\Users\Test\AppData\Local",
            @"C:\Users\Test",
            existing.Contains);

        Assert.Equal(@"D:\Codex\codex.exe", result);
    }

    [Fact]
    public void FindUsesPathBeforeKnownInstallLocations()
    {
        var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            @"C:\Tools\codex.exe",
            @"C:\Users\Test\AppData\Local\Programs\OpenAI\Codex\bin\codex.exe"
        };

        var result = CodexExecutableLocator.Find(
            null,
            @"C:\Missing;C:\Tools",
            @"C:\Users\Test\AppData\Local",
            @"C:\Users\Test",
            existing.Contains);

        Assert.Equal(@"C:\Tools\codex.exe", result);
    }

    [Fact]
    public void FindUsesKnownDesktopInstallLocation()
    {
        var expected = @"C:\Users\Test\AppData\Local\Programs\OpenAI\Codex\bin\codex.exe";

        var result = CodexExecutableLocator.Find(
            null,
            string.Empty,
            @"C:\Users\Test\AppData\Local",
            @"C:\Users\Test",
            path => path.Equals(expected, StringComparison.OrdinalIgnoreCase));

        Assert.Equal(expected, result);
    }

    [Fact]
    public void FindReturnsNullWhenNoCandidateExists()
    {
        var result = CodexExecutableLocator.Find(
            null,
            @"C:\Missing",
            @"C:\Users\Test\AppData\Local",
            @"C:\Users\Test",
            _ => false);

        Assert.Null(result);
    }
}
