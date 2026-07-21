using LLM.CLI.Configuration;
using LLM.CLI.Services;

namespace LLM.CLI.Commands;

public sealed class GpuCommand : ICommand
{
    private readonly GpuDetectionService _gpus;
    private readonly UserDataService _userData;
    private readonly ProfileService _profiles;
    private readonly IntelPrerequisiteService _intel;

    public GpuCommand(
        GpuDetectionService gpus,
        UserDataService userData,
        ProfileService profiles,
        IntelPrerequisiteService intel)
    {
        _gpus = gpus;
        _userData = userData;
        _profiles = profiles;
        _intel = intel;
    }

    public string Name => "gpu";

    public async Task<int> ExecuteAsync(string[] args)
    {
        if (args.Length == 0)
        {
            PrintHelp();
            return CommandResults.Success;
        }

        try
        {
            switch (args[0].ToLowerInvariant())
            {
                case "list":
                    ListGpus();
                    break;

                case "status":
                    ShowStatus();
                    break;

                case "use":
                    UseGpu(args);
                    break;

                case "auto":
                    AutoSelect();
                    break;

                case "doctor":
                    ShowIntelDoctor();
                    break;

                case "fix":
                    await FixIntelAsync(args);
                    break;

                default:
                    Console.WriteLine($"Unknown gpu command: {args[0]}");
                    PrintHelp();
                    break;
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            Console.WriteLine($"[FAIL] {ex.Message}");
            return CommandResults.Failure;
        }

        return CommandResults.Success;
    }

    private void ListGpus()
    {
        var devices = _gpus.DetectGpus();
        var backends = _gpus.DetectBackends();
        var config = _userData.Load();
        var selectedIndex = config.Runtime.GpuDeviceIndex;

        Console.WriteLine("Detected GPUs");
        Console.WriteLine("-------------");
        Console.WriteLine();

        if (devices.Count == 0)
        {
            Console.WriteLine("No GPUs detected.");
            return;
        }

        Console.WriteLine($"{"#",-3} {"VENDOR",-8} {"TYPE",-10} {"VRAM",-14} NAME");
        Console.WriteLine(new string('-', 90));

        foreach (var device in devices)
        {
            var marker = selectedIndex == device.Index ? "*" : " ";
            var type = device.IsLikelyDiscrete ? "discrete" : "integrated";
            Console.WriteLine(
                $"{marker}{device.Index,-2} {device.VendorLabel,-8} {type,-10} {device.AdapterRamLabel,-14} {device.Name}");
        }

        Console.WriteLine();
        Console.WriteLine("* = currently selected");
        Console.WriteLine();
        Console.WriteLine("Available backends in managed runtime:");
        foreach (var backend in backends)
        {
            var mark = backend.Available ? "[ OK ]" : "[ -- ]";
            Console.WriteLine($"  {mark} {backend.Label}");
            if (backend.LibraryPath is not null)
            {
                Console.WriteLine($"         {backend.LibraryPath}");
            }
        }

        Console.WriteLine();
        Console.WriteLine("Select a GPU:");
        Console.WriteLine("  llm gpu use 0");
        Console.WriteLine("  llm gpu use 0 --backend vulkan");
        Console.WriteLine("  llm gpu use 0 --backend cuda");
        Console.WriteLine("  llm gpu auto");
    }

    private void ShowStatus()
    {
        var config = _userData.Load();
        var device = _gpus.ResolveSelectedDevice(config);
        var backend = _gpus.ResolveBackend(config, device);

        Console.WriteLine("GPU Selection");
        Console.WriteLine("-------------");
        Console.WriteLine();
        Console.WriteLine($"Selected GPU : {(device is null ? "-" : $"[{device.Index}] {device.Name}")}");
        Console.WriteLine($"Vendor       : {device?.VendorLabel ?? "-"}");
        Console.WriteLine($"Backend      : {backend} (config: {config.Runtime.GpuBackend})");
        Console.WriteLine($"GPU Layers   : {config.Runtime.GpuLayers}");
        Console.WriteLine($"Profile      : {config.ActiveProfileId ?? "-"}");
    }

    private void UseGpu(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: llm gpu use <index> [--backend auto|cpu|vulkan|cuda|rocm] [--apply-profile]");
            return;
        }

        if (!int.TryParse(args[1], out var index))
        {
            // Allow matching by name fragment
            var devices = _gpus.DetectGpus();
            var name = string.Join(' ', args.Skip(1).Where(arg => !arg.StartsWith('-')));
            var match = devices.FirstOrDefault(device =>
                device.Name.Contains(name, StringComparison.OrdinalIgnoreCase));

            if (match is null)
            {
                throw new InvalidOperationException(
                    $"GPU not found: '{args[1]}'. Run: llm gpu list");
            }

            index = match.Index;
        }

        string? backend = null;
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i].Equals("--backend", StringComparison.OrdinalIgnoreCase)
                && i + 1 < args.Length)
            {
                backend = args[i + 1];
            }
        }

        var applyProfile = args.Contains("--apply-profile", StringComparer.OrdinalIgnoreCase);

        _gpus.ApplySelection(index, backend);

        if (applyProfile)
        {
            var config = _userData.Load();
            var device = _gpus.ResolveSelectedDevice(config);
            var profileId = GpuDetectionService.ResolveProfileId(device, backend)
                            ?? _gpus.Recommend().RecommendedProfileId;

            _profiles.EnsureDefaults();
            try
            {
                _profiles.Apply(profileId);
                // Re-apply GPU selection after profile (profile may reset backend).
                _gpus.ApplySelection(index, backend);
            }
            catch (InvalidOperationException)
            {
                // Profile missing — selection alone is enough.
            }
        }

        var updated = _userData.Load();
        Console.WriteLine();
        Console.WriteLine("[ OK ] GPU selected.");
        Console.WriteLine($"GPU     : [{updated.Runtime.GpuDeviceIndex}] {updated.Runtime.GpuDeviceName}");
        Console.WriteLine($"Backend : {updated.Runtime.GpuBackend}");
        Console.WriteLine($"Layers  : {updated.Runtime.GpuLayers}");
        Console.WriteLine();
        Console.WriteLine("Restart runtime to apply: llm runtime restart");
    }

    private void AutoSelect()
    {
        var recommendation = _gpus.Recommend();
        _gpus.ApplySelection(recommendation.RecommendedGpuIndex, recommendation.RecommendedBackend);

        _profiles.EnsureDefaults();
        try
        {
            _profiles.Apply(recommendation.RecommendedProfileId);
            _gpus.ApplySelection(recommendation.RecommendedGpuIndex, recommendation.RecommendedBackend);
        }
        catch (InvalidOperationException)
        {
        }

        Console.WriteLine();
        Console.WriteLine("[ OK ] Auto-selected GPU.");
        Console.WriteLine($"GPU     : [{recommendation.RecommendedGpuIndex}] {recommendation.RecommendedGpuName}");
        Console.WriteLine($"Backend : {recommendation.RecommendedBackend}");
        Console.WriteLine($"Profile : {recommendation.RecommendedProfileId}");
        Console.WriteLine($"Layers  : {recommendation.RecommendedGpuLayers}");
        Console.WriteLine();
        foreach (var note in recommendation.Notes)
        {
            Console.WriteLine($"  - {note}");
        }

        Console.WriteLine();
        Console.WriteLine("Restart runtime to apply: llm runtime restart");
    }

    private void ShowIntelDoctor()
    {
        var report = _intel.Check();

        Console.WriteLine("Intel GPU Prerequisites");
        Console.WriteLine("=======================");
        Console.WriteLine();

        if (!report.HasIntelGpu)
        {
            Console.WriteLine("No Intel GPU detected on this machine.");
            Console.WriteLine("Run: llm gpu list");
            return;
        }

        Console.WriteLine($"GPU: {report.IntelGpuName}");
        Console.WriteLine();
        Console.WriteLine("Checks");
        Console.WriteLine("------");

        foreach (var check in report.Checks)
        {
            var marker = check.Ok ? "[ OK ]" : "[FAIL]";
            Console.WriteLine($"{marker} {check.Name}");
            Console.WriteLine($"       {check.Detail}");
            if (!check.Ok && !string.IsNullOrWhiteSpace(check.AutoFixHint))
            {
                Console.WriteLine($"       Fix: {check.AutoFixHint}");
            }
        }

        Console.WriteLine();
        Console.WriteLine($"Vulkan ready for llama.cpp : {(report.VulkanReady ? "YES" : "NO")}");
        Console.WriteLine($"oneAPI installed (optional): {(report.OneApiReady ? "YES" : "NO")}");
        Console.WriteLine();

        foreach (var line in IntelPrerequisiteService.GetManualInstructions(report))
        {
            Console.WriteLine(line);
        }
    }

    private async Task FixIntelAsync(string[] args)
    {
        var hasVulkanFlag = args.Contains("--vulkan", StringComparer.OrdinalIgnoreCase);
        var hasOneApiFlag = args.Contains("--oneapi", StringComparer.OrdinalIgnoreCase);
        var hasAll = args.Contains("--all", StringComparer.OrdinalIgnoreCase);

        // Default (no flags): install Vulkan only. --oneapi alone skips Vulkan install.
        var installVulkan = hasAll || hasVulkanFlag || (!hasVulkanFlag && !hasOneApiFlag);
        var installOneApi = hasAll || hasOneApiFlag;

        Console.WriteLine("Intel GPU auto-fix");
        Console.WriteLine("-------------------");
        Console.WriteLine();
        Console.WriteLine($"Install Vulkan Runtime : {installVulkan}");
        Console.WriteLine($"Install oneAPI toolkit : {installOneApi}");
        Console.WriteLine();

        var result = await _intel.FixAsync(
            installVulkan: installVulkan,
            installOneApi: installOneApi,
            openDriverPage: true);

        foreach (var action in result.Actions)
        {
            Console.WriteLine(action);
        }

        Console.WriteLine();

        if (result.Failures.Count > 0)
        {
            Console.WriteLine("Failures:");
            foreach (var failure in result.Failures)
            {
                Console.WriteLine($"  [FAIL] {failure}");
            }

            Console.WriteLine();
        }

        if (result.Report is not null)
        {
            Console.WriteLine($"Vulkan ready : {(result.Report.VulkanReady ? "YES" : "NO")}");
            Console.WriteLine($"oneAPI ready : {(result.Report.OneApiReady ? "YES" : "NO")}");
            Console.WriteLine();
            foreach (var line in IntelPrerequisiteService.GetManualInstructions(result.Report))
            {
                Console.WriteLine(line);
            }
        }

        if (installVulkan && !result.VulkanReady)
        {
            throw new InvalidOperationException(
                "Vulkan is still not ready. Follow the instructions above, then re-run: llm gpu doctor");
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine();
        Console.WriteLine("  llm gpu list");
        Console.WriteLine("  llm gpu status");
        Console.WriteLine("  llm gpu auto");
        Console.WriteLine("  llm gpu use <index> [--backend auto|cpu|vulkan|cuda|rocm] [--apply-profile]");
        Console.WriteLine("  llm gpu doctor                 Check Intel Vulkan + oneAPI prerequisites");
        Console.WriteLine("  llm gpu fix [--vulkan] [--oneapi] [--all]");
        Console.WriteLine("                                 Auto-install missing deps via winget");
        Console.WriteLine();
        Console.WriteLine("Backends:");
        Console.WriteLine("  cuda    NVIDIA (requires ggml-cuda.dll)");
        Console.WriteLine("  rocm    AMD ROCm/HIP (requires ggml-hip.dll / ggml-rocm.dll)");
        Console.WriteLine("  vulkan  Intel / AMD / NVIDIA via Vulkan (ggml-vulkan.dll)");
        Console.WriteLine("  cpu     No GPU offload");
        Console.WriteLine("  auto    Pick best for selected GPU + installed runtime");
        Console.WriteLine();
        Console.WriteLine("Intel Iris Xe / Arc:");
        Console.WriteLine("  llm gpu doctor");
        Console.WriteLine("  llm gpu fix --vulkan          # install Vulkan Runtime (recommended)");
        Console.WriteLine("  llm gpu fix --oneapi          # optional SYCL/oneAPI toolkit");
    }
}
