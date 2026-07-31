using System.Diagnostics;
using System.Text.Json;
using System.Windows;
using CodexUsageCompanion.Configuration;
using CodexUsageCompanion.Diagnostics;
using CodexUsageCompanion.Lifecycle;
using CodexUsageCompanion.Localization;
using CodexUsageCompanion.RateLimits;
using CodexUsageCompanion.Ui;
using CodexUsageCompanion.Windows;

namespace CodexUsageCompanion;

public static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        return CommandModeParser.Parse(args) switch
        {
            CommandMode.SessionStart => HandleSessionStart(),
            CommandMode.Refresh => HandleRefresh(),
            CommandMode.Background => RunBackground(),
            CommandMode.Recovery => HandleRecovery(),
            CommandMode.Probe => RunProbe(),
            CommandMode.RenderPreview => RunRenderPreview(args),
            _ => RunBackground()
        };
    }

    private static int HandleSessionStart()
    {
        var coordinator = new InstanceCoordinator();
        if (coordinator.SignalRefresh())
        {
            return 0;
        }

        return DetachedLauncher.Start() ? 0 : 1;
    }

    private static int HandleRefresh()
    {
        var startupTimer = Stopwatch.StartNew();
        var coordinator = new InstanceCoordinator();
        if (coordinator.SignalRefresh())
        {
            return 0;
        }

        if (!DetachedLauncher.Start())
        {
            return 1;
        }

        while (startupTimer.Elapsed < ResidentStartupPolicy.SignalWaitTimeout)
        {
            Thread.Sleep(TimeSpan.FromMilliseconds(100));
            if (coordinator.SignalRefresh())
            {
                return 0;
            }
        }

        return 1;
    }

    private static int RunBackground()
    {
        var coordinator = new InstanceCoordinator();
        using var lease = coordinator.TryAcquireResident();
        if (lease is null)
        {
            return 0;
        }

        var application = new Application
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown
        };
        var settings = CompanionSettingsStore.Load();
        var text = UiText.For(UiLanguageResolver.Resolve(settings.Language, System.Globalization.CultureInfo.CurrentUICulture));
        var runtime = new CompanionRuntime(lease, settings, text);
        runtime.Start(application);
        var exitCode = application.Run();
        runtime.DisposeAsync().AsTask().GetAwaiter().GetResult();
        return exitCode;
    }

    private static int HandleRecovery()
    {
        if (!new CodexWindowLocator().IsCodexRunning())
        {
            return 0;
        }

        var coordinator = new InstanceCoordinator();
        if (coordinator.SignalRefresh())
        {
            return 0;
        }

        CompanionLog.Shared.Write("recovery", "scheduled-launch");
        return DetachedLauncher.Start() ? 0 : 1;
    }

    private static int RunProbe()
    {
        return RunProbeAsync().GetAwaiter().GetResult();
    }

    private static async Task<int> RunProbeAsync()
    {
        await using var client = new CodexAppServerClient();
        var state = await client.ReadRateLimitsAsync(CancellationToken.None);
        Console.WriteLine(JsonSerializer.Serialize(state));
        return 0;
    }

    private static int RunRenderPreview(IReadOnlyList<string> arguments)
    {
        if (arguments.Count < 2 || string.IsNullOrWhiteSpace(arguments[1]))
        {
            return 2;
        }

        UsagePreviewRenderer.Render(arguments[1], arguments.Count > 2 ? arguments[2] : null);
        return 0;
    }
}
