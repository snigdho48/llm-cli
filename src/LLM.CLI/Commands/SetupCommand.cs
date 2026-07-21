using LLM.CLI.Configuration;
using LLM.CLI.Services;

namespace LLM.CLI.Commands;

public sealed class SetupCommand : ICommand
{
    private readonly UserDataService _userData;
    private readonly WorkspaceService _workspace;
    private readonly RuntimeService _runtime;
    private readonly RuntimeInstallService _runtimeInstall;
    private readonly ModelService _models;
    private readonly ProfileService _profiles;
    private readonly HardwareService _hardware;
    private readonly GpuDetectionService _gpus;

    public SetupCommand(
        UserDataService userData,
        WorkspaceService workspace,
        RuntimeService runtime,
        RuntimeInstallService runtimeInstall,
        ModelService models,
        ProfileService profiles,
        HardwareService hardware,
        GpuDetectionService gpus)
    {
        _userData = userData;
        _workspace = workspace;
        _runtime = runtime;
        _runtimeInstall = runtimeInstall;
        _models = models;
        _profiles = profiles;
        _hardware = hardware;
        _gpus = gpus;
    }

    public string Name => "setup";

    public async Task<int> ExecuteAsync(string[] args)
    {
        var options = ParseOptions(args);
        var config = _userData.Load();
        var recommendation = _hardware.Recommend();

        Console.WriteLine();
        Console.WriteLine("=====================================");
        Console.WriteLine("   LLM CLI — First-Time Setup");
        Console.WriteLine("=====================================");
        Console.WriteLine();
        PrintHardware(recommendation);

        try
        {
            // Step 1: Workspace
            var workspacePath = options.Workspace
                                ?? Prompt("Workspace path", config.RootDirectory);
            config.RootDirectory = Path.GetFullPath(
                Environment.ExpandEnvironmentVariables(workspacePath.Trim()));

            var created = _workspace.Initialize(config.RootDirectory);
            _userData.Save(config);
            _profiles.EnsureDefaults();

            var selection = SelectCompute(options, recommendation);
            ApplyComputeSelection(selection);

            config = _userData.Load();

            Console.WriteLine();
            Console.WriteLine($"[ OK ] Workspace: {config.RootDirectory}");
            Console.WriteLine($"       Created {created.Count} directories.");
            Console.WriteLine($"       Device  : {DescribeDevice(config)}");
            Console.WriteLine($"       Backend : {config.Runtime.GpuBackend}");
            Console.WriteLine($"       Profile : {config.ActiveProfileId ?? "-"}");

            // Step 2: Runtime import / download
            if (!string.IsNullOrWhiteSpace(options.RuntimePath))
            {
                var copied = _runtime.Import(options.RuntimePath);
                Console.WriteLine($"[ OK ] Runtime imported ({copied.Count} files).");
            }
            else if (!_runtime.GetStatus().IsInstalled)
            {
                if (options.SkipRuntimeDownload)
                {
                    Console.WriteLine("[ -- ] Runtime skipped (--no-runtime).");
                }
                else if (options.AutoInstallRuntime || options.Workspace is not null)
                {
                    Console.WriteLine("Downloading llama.cpp (Windows Vulkan) from GitHub...");
                    var copied = await _runtimeInstall.InstallLatestAsync();
                    Console.WriteLine($"[ OK ] Runtime installed ({copied.Count} files).");
                }
                else
                {
                    var runtimePath = Prompt(
                        "Path to llama.cpp build (Enter = download from GitHub, 'skip' = later)",
                        "");
                    if (string.Equals(runtimePath, "skip", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("[ -- ] Runtime skipped.");
                    }
                    else if (!string.IsNullOrWhiteSpace(runtimePath))
                    {
                        var copied = _runtime.Import(runtimePath);
                        Console.WriteLine($"[ OK ] Runtime imported ({copied.Count} files).");
                    }
                    else
                    {
                        Console.WriteLine("Downloading llama.cpp (Windows Vulkan) from GitHub...");
                        var copied = await _runtimeInstall.InstallLatestAsync();
                        Console.WriteLine($"[ OK ] Runtime installed ({copied.Count} files).");
                    }
                }
            }

            // Step 3: Model
            if (!string.IsNullOrWhiteSpace(options.ModelPath))
            {
                RegisterModel(options.ModelPath, options.ModelId);
            }
            else if (string.IsNullOrWhiteSpace(_userData.Load().Runtime.ActiveModelPath))
            {
                var modelPath = Prompt("Path to .gguf model (skip to configure later)", "");
                if (!string.IsNullOrWhiteSpace(modelPath))
                {
                    RegisterModel(modelPath, null);
                }
            }

            // Step 4: Start runtime
            if (options.StartRuntime && _runtime.GetStatus().IsInstalled)
            {
                if (!_runtime.GetStatus().IsRunning)
                {
                    _runtime.Start();
                    await WaitBrieflyForHealthAsync();
                    Console.WriteLine("[ OK ] Runtime started.");
                }
            }

            // Step 5: Cursor instructions
            Console.WriteLine();
            Console.WriteLine("Setup complete. Next steps:");
            Console.WriteLine("  llm gpu list         — review / change GPU");
            Console.WriteLine("  llm cursor           — Cursor connection settings");
            Console.WriteLine("  llm doctor           — verify everything");
            Console.WriteLine("  llm chat \"hello\"     — test inference");
            Console.WriteLine("  llm serve            — start OpenAI-compatible server");

            return CommandResults.Success;
        }
        catch (Exception ex) when (ex is InvalidOperationException or FileNotFoundException or DirectoryNotFoundException or ArgumentException)
        {
            Console.WriteLine($"[FAIL] {ex.Message}");
            return CommandResults.Failure;
        }
    }

    private void RegisterModel(string modelPath, string? modelId)
    {
        var entry = _models.Add(modelPath, modelId);
        _models.Use(entry.Id);
        Console.WriteLine($"[ OK ] Model registered and activated: {entry.Id}");
    }

    private static async Task WaitBrieflyForHealthAsync()
    {
        await Task.Delay(TimeSpan.FromSeconds(3));
    }

    private static void PrintHardware(HardwareRecommendation recommendation)
    {
        Console.WriteLine("Detected GPUs:");
        foreach (var gpu in recommendation.DetectedGpus)
        {
            var mark = gpu.Index == recommendation.RecommendedGpuIndex ? "*" : " ";
            Console.WriteLine(
                $" {mark}[{gpu.Index}] {gpu.VendorLabel,-7} {gpu.Name}");
        }

        Console.WriteLine();
        Console.WriteLine("Recommendation:");
        Console.WriteLine($"  GPU         : [{recommendation.RecommendedGpuIndex}] {recommendation.RecommendedGpuName}");
        Console.WriteLine($"  Backend     : {recommendation.RecommendedBackend}");
        Console.WriteLine($"  CPU threads : {recommendation.RecommendedThreads}");
        Console.WriteLine($"  GPU layers  : {recommendation.RecommendedGpuLayers}");
        Console.WriteLine($"  Context     : {recommendation.RecommendedContextSize}");
        Console.WriteLine($"  Profile     : {recommendation.RecommendedProfileId}");
        Console.WriteLine();
    }

    private ComputeSelection SelectCompute(SetupOptions options, HardwareRecommendation recommendation)
    {
        if (options.CpuOnly)
        {
            return ComputeSelection.Cpu(recommendation.RecommendedThreads);
        }

        // Non-interactive setup scripts that pass --workspace should not hang on prompts.
        var useAuto = options.Auto
                      || (options.Workspace is not null
                          && options.GpuIndex is null
                          && string.IsNullOrWhiteSpace(options.Backend));

        if (useAuto)
        {
            return new ComputeSelection(
                false,
                recommendation.RecommendedGpuIndex,
                recommendation.RecommendedBackend,
                recommendation.RecommendedProfileId,
                recommendation.RecommendedThreads);
        }

        if (options.GpuIndex is not null || !string.IsNullOrWhiteSpace(options.Backend))
        {
            var index = options.GpuIndex ?? recommendation.RecommendedGpuIndex;
            if (index < 0 || index >= recommendation.DetectedGpus.Count)
            {
                throw new InvalidOperationException(
                    $"GPU index out of range. Valid: 0..{recommendation.DetectedGpus.Count - 1}");
            }

            var device = recommendation.DetectedGpus[index];
            var backend = options.Backend
                          ?? GpuDetectionService.RecommendBackend(device, recommendation.AvailableBackends)
                              .ToString()
                              .ToLowerInvariant();

            if (string.Equals(backend, "cpu", StringComparison.OrdinalIgnoreCase)
                || device.Vendor == GpuVendor.Cpu)
            {
                return ComputeSelection.Cpu(recommendation.RecommendedThreads);
            }

            return new ComputeSelection(
                false,
                index,
                backend,
                GpuDetectionService.ResolveProfileId(device, backend),
                recommendation.RecommendedThreads);
        }

        // Interactive prompt
        Console.WriteLine("Compute selection");
        Console.WriteLine("-----------------");
        foreach (var gpu in recommendation.DetectedGpus)
        {
            var mark = gpu.Index == recommendation.RecommendedGpuIndex ? "*" : " ";
            Console.WriteLine(
                $" {mark}[{gpu.Index}] {gpu.VendorLabel,-7} {gpu.Name}");
        }

        Console.WriteLine("  [c] CPU only");
        Console.WriteLine();

        var defaultDevice = recommendation.RecommendedGpuIndex.ToString();
        var deviceAnswer = Prompt("Select device index or 'c' for CPU", defaultDevice);

        if (deviceAnswer.Equals("c", StringComparison.OrdinalIgnoreCase)
            || deviceAnswer.Equals("cpu", StringComparison.OrdinalIgnoreCase))
        {
            return ComputeSelection.Cpu(recommendation.RecommendedThreads);
        }

        if (!int.TryParse(deviceAnswer, out var picked)
            || picked < 0
            || picked >= recommendation.DetectedGpus.Count)
        {
            throw new InvalidOperationException($"Invalid device selection: {deviceAnswer}");
        }

        var selected = recommendation.DetectedGpus[picked];
        var recommendedBackend = GpuDetectionService
            .RecommendBackend(selected, recommendation.AvailableBackends)
            .ToString()
            .ToLowerInvariant();

        var backendAnswer = Prompt("Select backend (cpu|vulkan|cuda|rocm)", recommendedBackend);
        if (backendAnswer.Equals("auto", StringComparison.OrdinalIgnoreCase))
        {
            backendAnswer = recommendedBackend;
        }

        var parsedBackend = GpuDetectionService.ParseBackend(backendAnswer)
            .ToString()
            .ToLowerInvariant();

        if (string.Equals(parsedBackend, "cpu", StringComparison.OrdinalIgnoreCase))
        {
            return ComputeSelection.Cpu(recommendation.RecommendedThreads);
        }

        return new ComputeSelection(
            false,
            picked,
            parsedBackend,
            GpuDetectionService.ResolveProfileId(selected, parsedBackend),
            recommendation.RecommendedThreads);
    }

    private void ApplyComputeSelection(ComputeSelection selection)
    {
        if (selection.IsCpuOnly)
        {
            _gpus.ApplyCpuOnly();
            try { _profiles.Apply("cpu-only"); } catch (InvalidOperationException) { }
        }
        else
        {
            _gpus.ApplySelection(selection.DeviceIndex, selection.Backend, requireBackendAvailable: false);
            if (!string.IsNullOrWhiteSpace(selection.ProfileId))
            {
                try
                {
                    _profiles.Apply(selection.ProfileId);
                    _gpus.ApplySelection(selection.DeviceIndex, selection.Backend, requireBackendAvailable: false);
                }
                catch (InvalidOperationException)
                {
                }
            }
        }

        var config = _userData.Load();
        config.Runtime.Threads = selection.Threads;
        if (selection.IsCpuOnly)
        {
            config.Runtime.GpuLayers = 0;
            config.Runtime.GpuBackend = "cpu";
        }

        _userData.Save(config);
    }

    private static string DescribeDevice(UserConfiguration config)
    {
        if (string.Equals(config.Runtime.GpuBackend, "cpu", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(config.Runtime.GpuDeviceName))
        {
            return "CPU";
        }

        return config.Runtime.GpuDeviceIndex is int index
            ? $"[{index}] {config.Runtime.GpuDeviceName}"
            : config.Runtime.GpuDeviceName;
    }

    private static string Prompt(string label, string defaultValue)
    {
        Console.Write($"{label}");
        if (!string.IsNullOrWhiteSpace(defaultValue))
        {
            Console.Write($" [{defaultValue}]");
        }

        Console.Write(": ");
        var input = Console.ReadLine();

        return string.IsNullOrWhiteSpace(input) ? defaultValue : input.Trim();
    }

    private static SetupOptions ParseOptions(string[] args)
    {
        var options = new SetupOptions { StartRuntime = true };

        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index].ToLowerInvariant())
            {
                case "--workspace" when index + 1 < args.Length:
                    options.Workspace = args[++index];
                    break;

                case "--runtime" when index + 1 < args.Length:
                    options.RuntimePath = args[++index];
                    break;

                case "--model" when index + 1 < args.Length:
                    options.ModelPath = args[++index];
                    break;

                case "--model-id" when index + 1 < args.Length:
                    options.ModelId = args[++index];
                    break;

                case "--no-start":
                    options.StartRuntime = false;
                    break;

                case "--no-runtime":
                    options.SkipRuntimeDownload = true;
                    break;

                case "--install-runtime":
                    options.AutoInstallRuntime = true;
                    break;

                case "--cpu":
                    options.CpuOnly = true;
                    options.NonInteractiveGpu = true;
                    break;

                case "--auto":
                    options.Auto = true;
                    options.NonInteractiveGpu = true;
                    break;

                case "--gpu" when index + 1 < args.Length:
                    if (!int.TryParse(args[++index], out var gpuIndex))
                    {
                        throw new ArgumentException("--gpu requires an integer device index.");
                    }

                    options.GpuIndex = gpuIndex;
                    options.NonInteractiveGpu = true;
                    break;

                case "--backend" when index + 1 < args.Length:
                    options.Backend = args[++index];
                    options.NonInteractiveGpu = true;
                    break;
            }
        }

        return options;
    }

    private sealed class SetupOptions
    {
        public string? Workspace { get; set; }

        public string? RuntimePath { get; set; }

        public string? ModelPath { get; set; }

        public string? ModelId { get; set; }

        public bool StartRuntime { get; set; }

        public bool AutoInstallRuntime { get; set; }

        public bool SkipRuntimeDownload { get; set; }

        public int? GpuIndex { get; set; }

        public string? Backend { get; set; }

        public bool CpuOnly { get; set; }

        public bool Auto { get; set; }

        public bool NonInteractiveGpu { get; set; }
    }

    private sealed record ComputeSelection(
        bool IsCpuOnly,
        int DeviceIndex,
        string Backend,
        string? ProfileId,
        int Threads)
    {
        public static ComputeSelection Cpu(int threads) =>
            new(true, -1, "cpu", "cpu-only", threads);
    }
}
