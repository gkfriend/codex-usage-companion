using CodexUsageCompanion.Diagnostics;
using Xunit;

namespace CodexUsageCompanion.Tests;

public sealed class CompanionLogTests
{
    [Fact]
    public void WriteRotatesLogBeforeItExceedsConfiguredSize()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"CodexUsageCompanionTests-{Guid.NewGuid():N}");
        try
        {
            var log = new CompanionLog(directory, 160);

            log.Write("first", new InvalidOperationException(new string('a', 100)));
            log.Write("second", new InvalidOperationException(new string('b', 100)));

            Assert.True(File.Exists(Path.Combine(directory, "companion.previous.log")));
            Assert.Contains("second", File.ReadAllText(Path.Combine(directory, "companion.log")));
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }

    [Fact]
    public void WritesLifecycleMessage()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"CodexUsageCompanionTests-{Guid.NewGuid():N}");
        try
        {
            var log = new CompanionLog(directory);

            log.Write("lifecycle", "resident-started");

            var text = File.ReadAllText(Path.Combine(directory, "companion.log"));
            Assert.Contains("[lifecycle] resident-started", text);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }
}
