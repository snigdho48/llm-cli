using LLM.CLI.Configuration;
using LLM.CLI.Services;

namespace LLM.CLI.Commands;

public sealed class InitCommand : ICommand
{
    private readonly UserDataService _userData;
    private readonly WorkspaceService _workspace;
    private readonly ProfileService _profiles;
    private readonly GpuDetectionService _gpus;

    public InitCommand(
        UserDataService userData,
        WorkspaceService workspace,
        ProfileService profiles,
        GpuDetectionService gpus)
    {
        _userData = userData;
        _workspace = workspace;
        _profiles = profiles;
        _gpus = gpus;
    }

    public string Name => "init";

    public Task<int> ExecuteAsync(string[] args)
    {
        var options = ParseOptions(args);
        var config = _userData.Load();

        Console.WriteLine();
        Console.WriteLine("=====================================");
        Console.WriteLine("      Welcome to LLM CLI");
        Console.WriteLine("=====================================");
        Console.WriteLine();
        Console.WriteLine("This wizard will initialize your AI workspace,");
        Console.WriteLine("select GPU / CPU, and set runtime parameters.");
        Console.WriteLine("(Press Enter on any prompt to keep the default.)");
        Console.WriteLine();

        try
        {
            var workspacePath = ResolveWorkspacePath(config, options);
            config.RootDirectory = workspacePath;

            var createdDirectories = _workspace.Initialize(config.RootDirectory);
            _profiles.EnsureDefaults();
            _userData.Save(config);

            Console.WriteLine();
            Console.WriteLine($"[ OK ] Workspace: {config.RootDirectory}");
            foreach (var directory in createdDirectories)
            {
                Console.WriteLine($"       {directory}");
            }

            var recommendation = _gpus.Recommend();
            var selection = SelectCompute(options, recommendation);
            ApplyComputeSelection(selection);

            // Profile may have set context/layers/threads — prompt overrides after that.
            var tuning = SelectRuntimeTuning(options, recommendation, selection.IsCpuOnly);
            ApplyRuntimeTuning(tuning);

            config = _userData.Load();
            Console.WriteLine();
            Console.WriteLine("[ OK ] Compute + runtime parameters saved.");
            Console.WriteLine($"       Device  : {DescribeDevice(config)}");
            Console.WriteLine($"       Backend : {config.Runtime.GpuBackend}");
            Console.WriteLine($"       Layers  : {config.Runtime.GpuLayers}");
            Console.WriteLine($"       Context : {config.Runtime.ContextSize}");
            Console.WriteLine($"       Threads : {config.Runtime.Threads}");
            Console.WriteLine($"       Port    : {config.Runtime.Port}");
            Console.WriteLine($"       Profile : {config.ActiveProfileId ?? "-"}");
            Console.WriteLine();
            Console.WriteLine("Change later:");
            Console.WriteLine("  llm config set Runtime:Context 8192");
            Console.WriteLine("  llm config set Runtime:GpuLayers 29");
            Console.WriteLine("  llm config set Runtime:Threads 8");
            Console.WriteLine("  llm config set Runtime:Port 11434");
            Console.WriteLine("  llm config show");

            if (selection.BackendPreferredButUnavailable)
            {
                Console.WriteLine();
                Console.WriteLine(
                    $"[WARN] Backend '{selection.Backend}' DLL not in managed runtime yet.");
                Console.WriteLine("       Preference saved. Next:");
                Console.WriteLine("         llm runtime import <path>");
                Console.WriteLine("         llm runtime install");
            }

            Console.WriteLine();
            Console.WriteLine("Next steps:");
            Console.WriteLine("  llm runtime import <path>   # or: llm runtime install");
            Console.WriteLine("  llm model add <path.gguf>");
            Console.WriteLine("  llm serve");
            if (selection.IsCpuOnly
                || string.Equals(selection.Backend, "cpu", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("  llm gpu list                # switch to GPU later");
            }
            else
            {
                Console.WriteLine("  llm gpu doctor              # Intel Vulkan / oneAPI checks");
            }

            return Task.FromResult(CommandResults.Success);
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException
                                       or InvalidOperationException)
        {
            Console.WriteLine($"Unable to initialize workspace: {ex.Message}");
            return Task.FromResult(CommandResults.Failure);
        }
    }

    private static string ResolveWorkspacePath(UserConfiguration config, InitOptions options)
    {
        Console.WriteLine("Default workspace:");
        Console.WriteLine(config.RootDirectory);
        Console.WriteLine();

        var input = options.Workspace
                    ?? (options.NonInteractive ? null : ReadLine("Workspace path (Press Enter for default)"));

        if (string.IsNullOrWhiteSpace(input))
        {
            return Path.GetFullPath(config.RootDirectory);
        }

        return Path.GetFullPath(Environment.ExpandEnvironmentVariables(input.Trim()));
    }

    private ComputeSelection SelectCompute(InitOptions options, HardwareRecommendation recommendation)
    {
        var devices = recommendation.DetectedGpus;
        var backends = recommendation.AvailableBackends;

        Console.WriteLine();
        Console.WriteLine("Compute devices");
        Console.WriteLine("---------------");
        PrintDevices(devices, recommendation.RecommendedGpuIndex);
        Console.WriteLine();
        Console.WriteLine("  [c] CPU only");
        Console.WriteLine();
        Console.WriteLine("Available backends (managed runtime):");
        foreach (var backend in backends)
        {
            var mark = backend.Available ? "[ OK ]" : "[ -- ]";
            Console.WriteLine($"  {mark} {backend.Label}");
        }

        Console.WriteLine();
        Console.WriteLine("Recommendation:");
        Console.WriteLine(
            $"  [{recommendation.RecommendedGpuIndex}] {recommendation.RecommendedGpuName} / {recommendation.RecommendedBackend}");
        Console.WriteLine(
            $"  profile={recommendation.RecommendedProfileId}, layers={recommendation.RecommendedGpuLayers}, context={recommendation.RecommendedContextSize}, threads={recommendation.RecommendedThreads}");
        Console.WriteLine();

        if (options.CpuOnly)
        {
            return ComputeSelection.Cpu();
        }

        if (options.Auto || options.NonInteractive)
        {
            if (options.GpuIndex is null && options.Backend is null && !options.CpuOnly)
            {
                return new ComputeSelection(
                    IsCpuOnly: false,
                    DeviceIndex: recommendation.RecommendedGpuIndex,
                    Backend: recommendation.RecommendedBackend,
                    ProfileId: recommendation.RecommendedProfileId,
                    BackendPreferredButUnavailable: false);
            }
        }

        if (options.GpuIndex is not null || options.Backend is not null)
        {
            return ResolveFromFlags(options, recommendation, backends);
        }

        if (options.NonInteractive)
        {
            return new ComputeSelection(
                IsCpuOnly: false,
                DeviceIndex: recommendation.RecommendedGpuIndex,
                Backend: recommendation.RecommendedBackend,
                ProfileId: recommendation.RecommendedProfileId,
                BackendPreferredButUnavailable: false);
        }

        return PromptCompute(recommendation, devices, backends);
    }

    private static ComputeSelection ResolveFromFlags(
        InitOptions options,
        HardwareRecommendation recommendation,
        IReadOnlyList<GpuBackendInfo> backends)
    {
        var index = options.GpuIndex ?? recommendation.RecommendedGpuIndex;
        if (index < 0 || index >= recommendation.DetectedGpus.Count)
        {
            throw new InvalidOperationException(
                $"GPU index out of range. Valid: 0..{recommendation.DetectedGpus.Count - 1}");
        }

        var device = recommendation.DetectedGpus[index];
        var backend = options.Backend
                      ?? GpuDetectionService.RecommendBackend(device, backends)
                          .ToString()
                          .ToLowerInvariant();

        if (string.Equals(backend, "cpu", StringComparison.OrdinalIgnoreCase)
            || device.Vendor == GpuVendor.Cpu)
        {
            return ComputeSelection.Cpu();
        }

        return new ComputeSelection(
            IsCpuOnly: false,
            DeviceIndex: index,
            Backend: backend,
            ProfileId: GpuDetectionService.ResolveProfileId(device, backend),
            BackendPreferredButUnavailable: BackendUnavailable(backend, backends));
    }

    private ComputeSelection PromptCompute(
        HardwareRecommendation recommendation,
        IReadOnlyList<GpuDevice> devices,
        IReadOnlyList<GpuBackendInfo> backends)
    {
        var defaultDevice = recommendation.RecommendedGpuIndex.ToString(
            System.Globalization.CultureInfo.InvariantCulture);
        var deviceAnswer = ReadLine($"Select device index or 'c' for CPU [{defaultDevice}]");

        if (string.IsNullOrWhiteSpace(deviceAnswer))
        {
            deviceAnswer = defaultDevice;
        }

        if (deviceAnswer.Equals("c", StringComparison.OrdinalIgnoreCase)
            || deviceAnswer.Equals("cpu", StringComparison.OrdinalIgnoreCase))
        {
            return ComputeSelection.Cpu();
        }

        if (!int.TryParse(deviceAnswer, out var index)
            || index < 0
            || index >= devices.Count)
        {
            throw new InvalidOperationException(
                $"Invalid device selection '{deviceAnswer}'. Use 0..{devices.Count - 1} or 'c'.");
        }

        var device = devices[index];
        var recommendedBackend = GpuDetectionService
            .RecommendBackend(device, backends)
            .ToString()
            .ToLowerInvariant();

        var backendAnswer = ReadLine(
            $"Select backend (auto|cpu|vulkan|cuda|rocm) [{recommendedBackend}]");
        if (string.IsNullOrWhiteSpace(backendAnswer))
        {
            backendAnswer = recommendedBackend;
        }

        if (backendAnswer.Equals("auto", StringComparison.OrdinalIgnoreCase))
        {
            backendAnswer = recommendedBackend;
        }

        var backend = GpuDetectionService.ParseBackend(backendAnswer)
            .ToString()
            .ToLowerInvariant();

        if (string.Equals(backend, "cpu", StringComparison.OrdinalIgnoreCase))
        {
            return ComputeSelection.Cpu();
        }

        var defaultProfile = GpuDetectionService.ResolveProfileId(device, backend);
        var profileAnswer = ReadLine($"Apply profile [{defaultProfile}] (Enter=yes, n=skip)");
        var profileId = string.IsNullOrWhiteSpace(profileAnswer)
                        || !profileAnswer.Equals("n", StringComparison.OrdinalIgnoreCase)
            ? defaultProfile
            : null;

        return new ComputeSelection(
            IsCpuOnly: false,
            DeviceIndex: index,
            Backend: backend,
            ProfileId: profileId,
            BackendPreferredButUnavailable: BackendUnavailable(backend, backends));
    }

    private RuntimeTuning SelectRuntimeTuning(
        InitOptions options,
        HardwareRecommendation recommendation,
        bool isCpuOnly)
    {
        var config = _userData.Load();

        // Prefer values already applied by profile; fall back to hardware recommendation.
        var defaultContext = config.Runtime.ContextSize > 0
            ? config.Runtime.ContextSize
            : recommendation.RecommendedContextSize;
        var defaultLayers = isCpuOnly
            ? 0
            : (config.Runtime.GpuLayers > 0
                ? config.Runtime.GpuLayers
                : recommendation.RecommendedGpuLayers);
        var defaultThreads = config.Runtime.Threads > 0
            ? config.Runtime.Threads
            : recommendation.RecommendedThreads;
        var defaultPort = config.Runtime.Port > 0 ? config.Runtime.Port : 11434;

        if (options.NonInteractive)
        {
            return new RuntimeTuning(
                ContextSize: options.ContextSize ?? defaultContext,
                GpuLayers: options.GpuLayers ?? defaultLayers,
                Threads: options.Threads ?? defaultThreads,
                Port: options.Port ?? defaultPort);
        }

        Console.WriteLine();
        Console.WriteLine("Runtime parameters");
        Console.WriteLine("------------------");
        Console.WriteLine("Press Enter to keep each default.");
        Console.WriteLine();

        var context = PromptInt(
            "Context length (-c)",
            options.ContextSize ?? defaultContext,
            min: 512,
            max: 131072);
        var layers = PromptInt(
            "GPU layers (-ngl)",
            options.GpuLayers ?? defaultLayers,
            min: 0,
            max: 99);
        var threads = PromptInt(
            "CPU threads (-t)",
            options.Threads ?? defaultThreads,
            min: 1,
            max: Environment.ProcessorCount * 2);
        var port = PromptInt(
            "API port",
            options.Port ?? defaultPort,
            min: 1,
            max: 65535);

        return new RuntimeTuning(context, layers, threads, port);
    }

    private void ApplyComputeSelection(ComputeSelection selection)
    {
        _profiles.EnsureDefaults();

        if (selection.IsCpuOnly
            || string.Equals(selection.Backend, "cpu", StringComparison.OrdinalIgnoreCase))
        {
            _gpus.ApplyCpuOnly();
            try
            {
                _profiles.Apply("cpu-only");
            }
            catch (InvalidOperationException)
            {
                // Profile optional.
            }
        }
        else
        {
            _gpus.ApplySelection(
                selection.DeviceIndex,
                selection.Backend,
                requireBackendAvailable: false);

            if (!string.IsNullOrWhiteSpace(selection.ProfileId))
            {
                try
                {
                    _profiles.Apply(selection.ProfileId);
                    _gpus.ApplySelection(
                        selection.DeviceIndex,
                        selection.Backend,
                        requireBackendAvailable: false);
                }
                catch (InvalidOperationException)
                {
                    // Selection alone is enough.
                }
            }
        }
    }

    private void ApplyRuntimeTuning(RuntimeTuning tuning)
    {
        var config = _userData.Load();
        config.Runtime.ContextSize = tuning.ContextSize;
        config.Runtime.GpuLayers = tuning.GpuLayers;
        config.Runtime.Threads = tuning.Threads;
        config.Runtime.Port = tuning.Port;
        _userData.Save(config);
    }

    private static int PromptInt(string label, int defaultValue, int min, int max)
    {
        var answer = ReadLine($"{label} [{defaultValue}]");
        if (string.IsNullOrWhiteSpace(answer))
        {
            return defaultValue;
        }

        if (!int.TryParse(answer, out var value))
        {
            throw new InvalidOperationException($"{label} must be an integer.");
        }

        if (value < min || value > max)
        {
            throw new InvalidOperationException($"{label} must be between {min} and {max}.");
        }

        return value;
    }

    private static void PrintDevices(IReadOnlyList<GpuDevice> devices, int recommendedIndex)
    {
        Console.WriteLine($"{"#",-3} {"VENDOR",-8} {"TYPE",-10} {"VRAM",-14} NAME");
        Console.WriteLine(new string('-', 80));

        foreach (var device in devices)
        {
            var marker = device.Index == recommendedIndex ? "*" : " ";
            var type = device.IsLikelyDiscrete ? "discrete" : "integrated";
            Console.WriteLine(
                $"{marker}{device.Index,-2} {device.VendorLabel,-8} {type,-10} {device.AdapterRamLabel,-14} {device.Name}");
        }

        Console.WriteLine("* = recommended");
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

    private static bool BackendUnavailable(string backend, IReadOnlyList<GpuBackendInfo> backends)
    {
        if (string.Equals(backend, "cpu", StringComparison.OrdinalIgnoreCase)
            || string.Equals(backend, "auto", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var parsed = GpuDetectionService.ParseBackend(backend);
        return backends.All(info => info.Backend != parsed || !info.Available);
    }

    private static string? ReadLine(string prompt)
    {
        Console.Write($"{prompt}: ");
        return Console.ReadLine();
    }

    private static InitOptions ParseOptions(string[] args)
    {
        var options = new InitOptions();
        var positional = new List<string>();

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg.ToLowerInvariant())
            {
                case "--cpu":
                    options.CpuOnly = true;
                    options.NonInteractive = true;
                    break;

                case "--auto":
                    options.Auto = true;
                    options.NonInteractive = true;
                    break;

                case "--gpu" when i + 1 < args.Length:
                    if (!int.TryParse(args[++i], out var gpuIndex))
                    {
                        throw new ArgumentException("--gpu requires an integer device index.");
                    }

                    options.GpuIndex = gpuIndex;
                    options.NonInteractive = true;
                    break;

                case "--backend" when i + 1 < args.Length:
                    options.Backend = args[++i];
                    options.NonInteractive = true;
                    break;

                case "--context" when i + 1 < args.Length:
                    options.ContextSize = ParsePositiveInt(args[++i], "--context");
                    options.NonInteractive = true;
                    break;

                case "--ngl" or "--layers" or "--gpu-layers" when i + 1 < args.Length:
                    options.GpuLayers = ParseNonNegativeInt(args[++i], arg);
                    options.NonInteractive = true;
                    break;

                case "--threads" when i + 1 < args.Length:
                    options.Threads = ParsePositiveInt(args[++i], "--threads");
                    options.NonInteractive = true;
                    break;

                case "--port" when i + 1 < args.Length:
                    options.Port = ParsePositiveInt(args[++i], "--port");
                    options.NonInteractive = true;
                    break;

                case "--yes" or "-y":
                    options.Auto = true;
                    options.NonInteractive = true;
                    break;

                default:
                    if (arg.StartsWith('-'))
                    {
                        throw new ArgumentException(
                            $"Unknown option: {arg}. Use --gpu N | --backend X | --cpu | --auto | --context N | --ngl N | --threads N | --port N");
                    }

                    positional.Add(arg);
                    break;
            }
        }

        if (positional.Count > 0)
        {
            options.Workspace = string.Join(' ', positional);
        }

        return options;
    }

    private static int ParsePositiveInt(string value, string flag)
    {
        if (!int.TryParse(value, out var parsed) || parsed < 1)
        {
            throw new ArgumentException($"{flag} requires a positive integer.");
        }

        return parsed;
    }

    private static int ParseNonNegativeInt(string value, string flag)
    {
        if (!int.TryParse(value, out var parsed) || parsed < 0)
        {
            throw new ArgumentException($"{flag} requires a non-negative integer.");
        }

        return parsed;
    }

    private sealed class InitOptions
    {
        public string? Workspace { get; set; }

        public int? GpuIndex { get; set; }

        public string? Backend { get; set; }

        public int? ContextSize { get; set; }

        public int? GpuLayers { get; set; }

        public int? Threads { get; set; }

        public int? Port { get; set; }

        public bool CpuOnly { get; set; }

        public bool Auto { get; set; }

        public bool NonInteractive { get; set; }
    }

    private sealed record ComputeSelection(
        bool IsCpuOnly,
        int DeviceIndex,
        string Backend,
        string? ProfileId,
        bool BackendPreferredButUnavailable)
    {
        public static ComputeSelection Cpu() =>
            new(
                IsCpuOnly: true,
                DeviceIndex: -1,
                Backend: "cpu",
                ProfileId: "cpu-only",
                BackendPreferredButUnavailable: false);
    }

    private sealed record RuntimeTuning(
        int ContextSize,
        int GpuLayers,
        int Threads,
        int Port);
}
