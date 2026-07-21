using LLM.CLI.Configuration;

namespace LLM.CLI.Services;

public sealed class HealthCheckService
{
    private readonly UserDataService _userData;
    private readonly GpuDetectionService _gpus;

    public HealthCheckService(UserDataService userData, GpuDetectionService gpus)
    {
        _userData = userData;
        _gpus = gpus;
    }

    public async Task<HealthReport> CheckAsync(CancellationToken cancellationToken = default)
    {
        var config = _userData.Load();
        var report = new HealthReport();

        report.Checks.Add(CheckDotNet());
        report.Checks.Add(CheckWorkspace(config));
        report.Checks.Add(CheckRuntime(config));
        report.Checks.Add(CheckModel(config));
        report.Checks.Add(CheckGpuBackend(config));
        report.Checks.Add(await CheckApiAsync(config, cancellationToken));

        return report;
    }

    private static HealthCheckResult CheckDotNet()
    {
        return new HealthCheckResult(
            ".NET Runtime",
            true,
            Environment.Version.ToString());
    }

    private static HealthCheckResult CheckWorkspace(UserConfiguration config)
    {
        var exists = Directory.Exists(config.RootDirectory);

        return new HealthCheckResult(
            "Workspace",
            exists,
            config.RootDirectory);
    }

    private static HealthCheckResult CheckRuntime(UserConfiguration config)
    {
        var installed = !string.IsNullOrWhiteSpace(config.Runtime.ExecutablePath)
                        && File.Exists(config.Runtime.ExecutablePath);

        return new HealthCheckResult(
            "Runtime Installed",
            installed,
            installed ? config.Runtime.ExecutablePath : "Run: llm runtime import <path>");
    }

    private static HealthCheckResult CheckModel(UserConfiguration config)
    {
        if (string.IsNullOrWhiteSpace(config.Runtime.ActiveModelPath))
        {
            return new HealthCheckResult(
                "Active Model",
                false,
                "Run: llm model add <path> && llm model use <id>");
        }

        var exists = File.Exists(config.Runtime.ActiveModelPath);

        return new HealthCheckResult(
            "Active Model",
            exists,
            config.Runtime.ActiveModelPath);
    }

    private HealthCheckResult CheckGpuBackend(UserConfiguration config)
    {
        var device = _gpus.ResolveSelectedDevice(config);
        var backend = _gpus.ResolveBackend(config, device);

        if (backend == GpuBackend.Cpu)
        {
            return new HealthCheckResult(
                "GPU Backend",
                true,
                $"cpu (device: {device?.Name ?? "none"})");
        }

        if (string.IsNullOrWhiteSpace(config.Runtime.ExecutablePath))
        {
            return new HealthCheckResult(
                "GPU Backend",
                false,
                "Runtime not installed");
        }

        var runtimeDirectory = Path.GetDirectoryName(config.Runtime.ExecutablePath);
        if (runtimeDirectory is null)
        {
            return new HealthCheckResult("GPU Backend", false, "Invalid runtime path");
        }

        var requiredDll = backend switch
        {
            GpuBackend.Cuda => "ggml-cuda.dll",
            GpuBackend.Rocm => File.Exists(Path.Combine(runtimeDirectory, "ggml-hip.dll"))
                ? "ggml-hip.dll"
                : "ggml-rocm.dll",
            GpuBackend.Vulkan => "ggml-vulkan.dll",
            _ => null,
        };

        if (requiredDll is null)
        {
            return new HealthCheckResult("GPU Backend", true, backend.ToString());
        }

        var path = Path.Combine(runtimeDirectory, requiredDll);
        var exists = File.Exists(path);

        return new HealthCheckResult(
            "GPU Backend",
            exists,
            exists
                ? $"{backend} [{device?.Index}] {device?.Name} -> {path}"
                : $"{requiredDll} missing for backend '{backend}'. Import matching llama.cpp build.");
    }

    private static async Task<HealthCheckResult> CheckApiAsync(
        UserConfiguration config,
        CancellationToken cancellationToken)
    {
        var url = $"http://{config.Runtime.Host}:{config.Runtime.Port}/health";

        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            using var response = await client.GetAsync(url, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var trimmed = body.Trim();
            if (trimmed.StartsWith("<!", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("<html", StringComparison.OrdinalIgnoreCase))
            {
                return new HealthCheckResult(
                    "OpenAI API",
                    false,
                    $"{url} -> unexpected HTML (port may be used by another app)");
            }

            var detail = trimmed.Length > 120 ? trimmed[..120] + "..." : trimmed;

            return new HealthCheckResult(
                "OpenAI API",
                response.IsSuccessStatusCode,
                $"{url} -> {detail}");
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(
                "OpenAI API",
                false,
                $"{url} -> {ex.Message}");
        }
    }
}

public sealed class HealthReport
{
    public List<HealthCheckResult> Checks { get; } = [];

    public bool IsHealthy => Checks.All(check => check.Ok);
}

public sealed class HealthCheckResult
{
    public HealthCheckResult(string name, bool ok, string detail)
    {
        Name = name;
        Ok = ok;
        Detail = detail;
    }

    public string Name { get; }

    public bool Ok { get; }

    public string Detail { get; }
}
