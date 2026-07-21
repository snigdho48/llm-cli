using LLM.CLI.Services;

namespace LLM.CLI.Commands;

public sealed class DoctorCommand : ICommand
{
    private readonly HealthCheckService _health;
    private readonly RuntimeService _runtime;
    private readonly UserDataService _userData;
    private readonly HardwareService _hardware;
    private readonly GpuDetectionService _gpus;

    public DoctorCommand(
        HealthCheckService health,
        RuntimeService runtime,
        UserDataService userData,
        HardwareService hardware,
        GpuDetectionService gpus)
    {
        _health = health;
        _runtime = runtime;
        _userData = userData;
        _hardware = hardware;
        _gpus = gpus;
    }

    public string Name => "doctor";

    public async Task<int> ExecuteAsync(string[] args)
    {
        var config = _userData.Load();
        var status = _runtime.GetStatus();
        var report = await _health.CheckAsync();
        var recommendation = _hardware.Recommend();
        var selected = _gpus.ResolveSelectedDevice(config);
        var backend = _gpus.ResolveBackend(config, selected);

        Console.WriteLine("LLM CLI Diagnostics");
        Console.WriteLine("-------------------");
        Console.WriteLine();

        Console.WriteLine($"OS            : {Environment.OSVersion}");
        Console.WriteLine($".NET          : {Environment.Version}");
        Console.WriteLine($"CPU Threads   : {recommendation.LogicalCores} logical / {recommendation.RecommendedThreads} recommended");
        Console.WriteLine($"Active Profile: {config.ActiveProfileId ?? "-"}");
        Console.WriteLine($"GPU           : {(selected is null ? "-" : $"[{selected.Index}] {selected.Name}")}");
        Console.WriteLine($"GPU Backend   : {backend} (config: {config.Runtime.GpuBackend})");
        Console.WriteLine($"GPU Layers    : {config.Runtime.GpuLayers}");
        Console.WriteLine($"Workspace     : {config.RootDirectory}");
        Console.WriteLine($"Config File   : {_userData.ConfigFile}");
        Console.WriteLine($"Server Status : {(status.IsRunning ? "RUNNING" : "STOPPED")}");
        Console.WriteLine();

        if (recommendation.DetectedGpus.Count > 0)
        {
            Console.WriteLine("Detected GPUs");
            Console.WriteLine("-------------");
            foreach (var gpu in recommendation.DetectedGpus)
            {
                var mark = selected?.Index == gpu.Index ? "*" : " ";
                Console.WriteLine(
                    $"{mark}[{gpu.Index}] {gpu.VendorLabel,-7} {gpu.Name} ({(gpu.IsLikelyDiscrete ? "discrete" : "integrated")})");
            }

            Console.WriteLine();
            Console.WriteLine("Change GPU: llm gpu list  |  llm gpu use <index> [--backend vulkan|cuda|rocm|cpu]");
            if (selected?.Vendor == Configuration.GpuVendor.Intel)
            {
                Console.WriteLine("Intel deps: llm gpu doctor  |  llm gpu fix --vulkan");
            }

            Console.WriteLine();
        }

        Console.WriteLine("Health Checks");
        Console.WriteLine("-------------");

        foreach (var check in report.Checks)
        {
            var marker = check.Ok ? "[ OK ]" : "[FAIL]";
            Console.WriteLine($"{marker} {check.Name,-18} {check.Detail}");
        }

        Console.WriteLine();

        if (report.IsHealthy && status.IsRunning)
        {
            Console.WriteLine("Cursor connection:");
            Console.WriteLine($"  Base URL : http://{config.Runtime.Host}:{config.Runtime.Port}/v1");
            Console.WriteLine($"  API Key  : {config.Runtime.ApiKey}");
            Console.WriteLine();
            Console.WriteLine("Run: llm cursor");
        }
        else if (!status.IsRunning)
        {
            Console.WriteLine("Next step:");
            Console.WriteLine("  llm serve");
        }

        return CommandResults.Success;
    }
}
