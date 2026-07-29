using System.IO;
using System.Text.Json;

namespace CodexUsageCompanion.RateLimits;

public sealed class CodexAppServerSession : IAsyncDisposable
{
    private static readonly string ClientVersion = typeof(CodexAppServerSession).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";
    private readonly JsonLineRpcConnection _connection;
    private readonly TimeSpan _requestTimeout;
    private long _nextId;

    public CodexAppServerSession(
        TextReader reader,
        TextWriter writer,
        TimeSpan? requestTimeout = null)
    {
        _connection = new JsonLineRpcConnection(reader, writer);
        _connection.NotificationReceived += HandleNotification;
        _requestTimeout = requestTimeout ?? TimeSpan.FromSeconds(8);
    }

    public event Action? RateLimitsChanged;

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var id = NextId();
        var request = JsonSerializer.Serialize(new
        {
            id,
            method = "initialize",
            @params = new
            {
                clientInfo = new
                {
                    name = "codex-usage-companion",
                    version = ClientVersion
                },
                capabilities = new
                {
                    experimentalApi = true
                }
            }
        });
        await SendRequestAsync(id, request, cancellationToken);
        await _connection.SendNotificationAsync("{\"method\":\"initialized\"}", cancellationToken);
    }

    public async Task<RateLimitState> ReadRateLimitsAsync(CancellationToken cancellationToken)
    {
        var id = NextId();
        var request = JsonSerializer.Serialize(new
        {
            id,
            method = "account/rateLimits/read"
        });
        var response = await SendRequestAsync(id, request, cancellationToken);
        return RateLimitParser.ParseResponse(response);
    }

    public async ValueTask DisposeAsync()
    {
        _connection.NotificationReceived -= HandleNotification;
        await _connection.DisposeAsync();
    }

    private long NextId()
    {
        return Interlocked.Increment(ref _nextId);
    }

    private async Task<string> SendRequestAsync(
        long id,
        string request,
        CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_requestTimeout);
        try
        {
            return await _connection.SendRequestAsync(id, request, timeout.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"Codex app-server request {id} timed out after {_requestTimeout.TotalSeconds:0.#} seconds.");
        }
    }

    private void HandleNotification(string method)
    {
        if (method == "account/rateLimits/updated")
        {
            RateLimitsChanged?.Invoke();
        }
    }
}
