using System.Diagnostics;
using System.IO;
using CodexUsageCompanion.Diagnostics;

namespace CodexUsageCompanion.RateLimits;

public sealed class CodexAppServerClient : IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Func<string?> _findExecutable;
    private Process? _process;
    private CodexAppServerSession? _session;
    private Task? _stderrDrain;

    public CodexAppServerClient(Func<string?>? findExecutable = null)
    {
        _findExecutable = findExecutable ?? CodexExecutableLocator.Find;
    }

    public event Action? RateLimitsChanged;

    public async Task<RateLimitState> ReadRateLimitsAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            for (var attempt = 0; attempt < 2; attempt++)
            {
                try
                {
                    await EnsureConnectedAsync(cancellationToken);
                    return await _session!.ReadRateLimitsAsync(cancellationToken);
                }
                catch (Exception exception) when (attempt == 0 && !cancellationToken.IsCancellationRequested)
                {
                    CompanionLog.Shared.Write("rate-limits-retry", exception);
                    await ResetConnectionAsync();
                    await Task.Delay(250, cancellationToken);
                }
            }

            throw new InvalidOperationException("Codex app-server did not return rate limits.");
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _gate.WaitAsync();
        try
        {
            await ResetConnectionAsync();
        }
        finally
        {
            _gate.Release();
            _gate.Dispose();
        }
    }

    private async Task EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        if (_process is { HasExited: false } && _session is not null)
        {
            return;
        }

        await ResetConnectionAsync();
        var executable = _findExecutable();
        if (string.IsNullOrWhiteSpace(executable))
        {
            throw new FileNotFoundException("Codex CLI was not found. Install Codex Desktop or set CODEX_CLI_PATH.");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            Arguments = "app-server --stdio",
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };
        _process = Process.Start(startInfo) ?? throw new InvalidOperationException("Unable to start Codex app-server.");
        _stderrDrain = _process.StandardError.ReadToEndAsync();
        _session = new CodexAppServerSession(_process.StandardOutput, _process.StandardInput);
        _session.RateLimitsChanged += HandleRateLimitsChanged;
        await _session.InitializeAsync(cancellationToken);
    }

    private async Task ResetConnectionAsync()
    {
        var session = _session;
        _session = null;
        if (session is not null)
        {
            session.RateLimitsChanged -= HandleRateLimitsChanged;
        }

        if (_process is null)
        {
            if (session is not null)
            {
                await session.DisposeAsync();
            }

            return;
        }

        var process = _process;
        _process = null;
        try
        {
            if (!process.HasExited)
            {
                process.Kill(true);
            }
        }
        catch (InvalidOperationException)
        {
        }
        catch (System.ComponentModel.Win32Exception) when (process.HasExited)
        {
        }

        try
        {
            await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(3));
        }
        catch (TimeoutException exception)
        {
            CompanionLog.Shared.Write("app-server-exit-timeout", exception);
        }
        if (session is not null)
        {
            await session.DisposeAsync();
        }

        process.Dispose();
        if (_stderrDrain is not null)
        {
            try
            {
                await _stderrDrain;
            }
            catch (Exception exception)
            {
                CompanionLog.Shared.Write("app-server-stderr", exception);
            }
        }

        _stderrDrain = null;
    }

    private void HandleRateLimitsChanged()
    {
        RateLimitsChanged?.Invoke();
    }
}
