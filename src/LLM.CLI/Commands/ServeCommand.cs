using LLM.CLI.Services;

namespace LLM.CLI.Commands;

public sealed class ServeCommand : ICommand
{
    private readonly RuntimeService _runtime;
    private readonly UserDataService _userData;
    private readonly HealthCheckService _health;
    private readonly DaemonService _daemon;

    public ServeCommand(
        RuntimeService runtime,
        UserDataService userData,
        HealthCheckService health,
        DaemonService daemon)
    {
        _runtime = runtime;
        _userData = userData;
        _health = health;
        _daemon = daemon;
    }

    public string Name => "serve";

    public async Task<int> ExecuteAsync(string[] args)
    {
        var restart = args.Contains("--restart", StringComparer.OrdinalIgnoreCase);
        var installDaemon = args.Contains("--daemon", StringComparer.OrdinalIgnoreCase);
        var noInstall = args.Contains("--no-install", StringComparer.OrdinalIgnoreCase);

        try
        {
            if (installDaemon)
            {
                _daemon.Install();
                Console.WriteLine("[ OK ] Auto-start registered (Task Scheduler).");
            }

            await _runtime.EnsureInstalledAsync(downloadIfMissing: !noInstall);

            if (restart && _runtime.GetStatus().IsRunning)
            {
                _runtime.Restart();
            }

            await _runtime.EnsureRunningAsync(downloadIfMissing: !noInstall);

            var config = _userData.Load();
            var report = await _health.CheckAsync();
            var baseUrl = $"http://{config.Runtime.Host}:{config.Runtime.Port}/v1";

            Console.WriteLine();
            Console.WriteLine("LLM Server Ready");
            Console.WriteLine("================");
            Console.WriteLine();
            Console.WriteLine($"  OpenAI API : {baseUrl}");
            Console.WriteLine($"  Health     : http://{config.Runtime.Host}:{config.Runtime.Port}/health");
            Console.WriteLine($"  API Key    : {config.Runtime.ApiKey}");
            Console.WriteLine($"  Model      : {config.Runtime.ActiveModelPath ?? "-"}");
            Console.WriteLine($"  Profile    : {config.ActiveProfileId ?? "-"}");
            Console.WriteLine($"  GPU Layers : {config.Runtime.GpuLayers}");
            Console.WriteLine($"  Context    : {config.Runtime.ContextSize}");
            Console.WriteLine($"  Daemon     : {(_daemon.IsTaskRegistered() ? "ON (logon)" : "off")}");
            Console.WriteLine();

            if (report.IsHealthy)
            {
                Console.WriteLine("[ OK ] Server is healthy.");
                Console.WriteLine();
                Console.WriteLine("Cursor: llm cursor");
                Console.WriteLine("Test  : llm chat \"hello\"");
            }
            else
            {
                Console.WriteLine("[WARN] Server started but health check failed.");
                Console.WriteLine("       Run: llm doctor");
                Console.WriteLine("       Run: llm logs");
            }

            return CommandResults.Success;
        }
        catch (Exception ex) when (ex is InvalidOperationException or FileNotFoundException)
        {
            Console.WriteLine($"[FAIL] {ex.Message}");
            return CommandResults.Failure;
        }
    }
}
