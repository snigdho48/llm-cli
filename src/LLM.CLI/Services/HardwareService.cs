using LLM.CLI.Configuration;

namespace LLM.CLI.Services;

public sealed class HardwareService
{
    private readonly GpuDetectionService _gpus;

    public HardwareService(GpuDetectionService gpus)
    {
        _gpus = gpus;
    }

    public HardwareRecommendation Recommend() => _gpus.Recommend();

    public IReadOnlyList<GpuDevice> ListGpus() => _gpus.DetectGpus();
}

public sealed class HardwareRecommendation
{
    public int LogicalCores { get; init; }

    public int RecommendedThreads { get; init; }

    public int RecommendedGpuLayers { get; init; }

    public int RecommendedContextSize { get; init; }

    public string RecommendedProfileId { get; init; } = "cpu-only";

    public string RecommendedBackend { get; init; } = "cpu";

    public int RecommendedGpuIndex { get; init; }

    public string RecommendedGpuName { get; init; } = "CPU";

    public IReadOnlyList<GpuDevice> DetectedGpus { get; init; } = [];

    public IReadOnlyList<GpuBackendInfo> AvailableBackends { get; init; } = [];

    public IReadOnlyList<string> Notes { get; init; } = [];
}
