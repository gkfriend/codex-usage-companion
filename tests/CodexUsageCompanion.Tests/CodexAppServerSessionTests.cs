using CodexUsageCompanion.RateLimits;
using Xunit;

namespace CodexUsageCompanion.Tests;

public sealed class CodexAppServerSessionTests
{
    [Fact]
    public async Task SessionTimesOutWhenAppServerDoesNotRespond()
    {
        using var reader = new NeverEndingTextReader();
        using var writer = new StringWriter();
        await using var session = new CodexAppServerSession(reader, writer, TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAsync<TimeoutException>(
            () => session.InitializeAsync(CancellationToken.None));
    }

    [Fact]
    public async Task SessionRaisesRateLimitsChangedForRollingNotification()
    {
        const string responses = """
        {"id":1,"result":{"userAgent":"test"}}
        {"method":"account/rateLimits/updated","params":{"rateLimits":{"primary":{"usedPercent":42,"windowDurationMins":10080},"secondary":{"usedPercent":20,"windowDurationMins":300}}}}
        {"id":2,"result":{"rateLimits":{"primary":{"usedPercent":49,"windowDurationMins":300}}}}
        """;
        using var reader = new StringReader(responses);
        using var writer = new StringWriter();
        await using var session = new CodexAppServerSession(reader, writer);
        var update = new TaskCompletionSource<RateLimitState>(TaskCreationOptions.RunContinuationsAsynchronously);
        session.RateLimitsChanged += update.SetResult;

        await session.InitializeAsync(CancellationToken.None);
        var state = await update.Task.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Equal(80, state.FiveHour?.RemainingPercent);
        Assert.Equal(58, state.Weekly?.RemainingPercent);
        Assert.DoesNotContain("\"method\":\"account/rateLimits/read\"", writer.ToString());
    }

    [Fact]
    public async Task SessionInitializesThenReadsRateLimits()
    {
        const string responses = """
        {"id":1,"result":{"userAgent":"test"}}
        {"id":2,"result":{"rateLimits":{"primary":{"usedPercent":49,"windowDurationMins":300,"resetsAt":1783697624},"secondary":{"usedPercent":42,"windowDurationMins":10080,"resetsAt":1784247424}}}}
        """;
        using var reader = new StringReader(responses);
        using var writer = new StringWriter();
        await using var session = new CodexAppServerSession(reader, writer);

        await session.InitializeAsync(CancellationToken.None);
        var state = await session.ReadRateLimitsAsync(CancellationToken.None);

        Assert.Equal(51, state.FiveHour?.RemainingPercent);
        Assert.Equal(58, state.Weekly?.RemainingPercent);
        var output = writer.ToString();
        Assert.Contains("\"method\":\"initialize\"", output);
        Assert.Contains("\"version\":\"0.3.5\"", output);
        Assert.Contains("\"method\":\"initialized\"", output);
        Assert.Contains("\"method\":\"account/rateLimits/read\"", output);
    }

    private sealed class NeverEndingTextReader : TextReader
    {
        public override async ValueTask<string?> ReadLineAsync(CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return null;
        }
    }
}
