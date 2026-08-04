using System.Text.Json;
using CodexUsageCompanion.RateLimits;
using Xunit;

namespace CodexUsageCompanion.Tests;

public sealed class JsonLineRpcClientTests
{
    [Fact]
    public async Task ConnectionPreservesRawNotificationPayload()
    {
        const string notification = "{\"method\":\"account/rateLimits/updated\",\"params\":{\"rateLimits\":{\"primary\":{\"usedPercent\":20}}}}";
        using var reader = new StringReader($"{notification}{Environment.NewLine}{{\"id\":1,\"result\":{{}}}}");
        using var writer = new StringWriter();
        await using var connection = new JsonLineRpcConnection(reader, writer);
        var received = new TaskCompletionSource<(string Method, string Json)>(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.NotificationReceived += (method, json) => received.SetResult((method, json));

        await connection.SendRequestAsync(1, "{}", CancellationToken.None);
        var value = await received.Task.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Equal("account/rateLimits/updated", value.Method);
        Assert.Equal(notification, value.Json);
    }

    [Fact]
    public async Task ConnectionThrowsMeaningfulErrorForJsonRpcFailure()
    {
        using var reader = new StringReader("{\"id\":1,\"error\":{\"code\":-32600,\"message\":\"invalid request\"}}");
        using var writer = new StringWriter();
        await using var connection = new JsonLineRpcConnection(reader, writer);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => connection.SendRequestAsync(1, "{}", CancellationToken.None));

        Assert.Contains("invalid request", exception.Message);
    }

    [Fact]
    public async Task ReadResponseAsyncIgnoresNotificationsAndOtherResponseIds()
    {
        const string lines = """
        {"method":"account/rateLimits/updated","params":{"rateLimits":{"primary":{"usedPercent":20}}}}
        {"id":1,"result":{"userAgent":"test"}}
        {"id":2,"result":{"rateLimits":{"primary":{"usedPercent":19}}}}
        """;
        using var reader = new StringReader(lines);

        var response = await JsonLineRpcClient.ReadResponseAsync(reader, 2, CancellationToken.None);

        using var document = JsonDocument.Parse(response);
        Assert.Equal(2, document.RootElement.GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task ReadResponseAsyncThrowsWhenStreamEndsBeforeResponse()
    {
        using var reader = new StringReader("{\"id\":1,\"result\":{}}");

        await Assert.ThrowsAsync<EndOfStreamException>(
            () => JsonLineRpcClient.ReadResponseAsync(reader, 2, CancellationToken.None));
    }

    [Fact]
    public async Task ReadResponseAsyncRejectsMalformedJsonLine()
    {
        using var reader = new StringReader("{");

        await Assert.ThrowsAnyAsync<JsonException>(
            () => JsonLineRpcClient.ReadResponseAsync(reader, 2, CancellationToken.None));
    }

    [Fact]
    public async Task ConnectionDropsUnknownResponseIds()
    {
        const string lines = """
        {"id":1,"result":{"stale":true}}
        {"id":2,"result":{"current":true}}
        """;
        using var reader = new StringReader(lines);
        using var writer = new StringWriter();
        await using var connection = new JsonLineRpcConnection(reader, writer);

        await connection.SendRequestAsync(2, "{}", CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => connection.SendRequestAsync(1, "{}", CancellationToken.None));
    }
}
