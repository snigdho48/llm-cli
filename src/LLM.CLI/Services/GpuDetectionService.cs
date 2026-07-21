using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using LLM.CLI.Configuration;

namespace LLM.CLI.Services;

public sealed class GpuDetectionService
{
    private static readonly Regex VendorRegex = new(
        @"VEN_([0-9A-F]{4})",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly UserDataService _userData;

    public GpuDetectionService(UserDataService userData)
    {
        _userData = userData;
    }

    public IReadOnlyList<GpuDevice> DetectGpus()
    {
        if (OperatingSystem.IsWindows())
        {
            var fromWmi = DetectViaWmi();
            if (fromWmi.Count > 0)
            {
                return fromWmi;
            }
        }

        return
        [
            new GpuDevice
            {
                Index = 0,
                Name = "CPU (fallback — GPU detection unavailable)",
                Vendor = GpuVendor.Cpu,
                IsLikelyDiscrete = false,
            },
        ];
    }

    public IReadOnlyList<GpuBackendInfo> DetectBackends(string? runtimeDirectory = null)
    {
        runtimeDirectory ??= ResolveRuntimeDirectory();
        var backends = new List<GpuBackendInfo>
        {
            new() { Backend = GpuBackend.Cpu, Available = true },
        };

        if (string.IsNullOrWhiteSpace(runtimeDirectory) || !Directory.Exists(runtimeDirectory))
        {
            return backends;
        }

        AddIfPresent(backends, runtimeDirectory, GpuBackend.Vulkan, "ggml-vulkan.dll");
        AddIfPresent(backends, runtimeDirectory, GpuBackend.Cuda, "ggml-cuda.dll");
        AddIfPresent(backends, runtimeDirectory, GpuBackend.Rocm, "ggml-hip.dll");
        AddIfPresent(backends, runtimeDirectory, GpuBackend.Rocm, "ggml-rocm.dll");

        return backends
            .GroupBy(backend => backend.Backend)
            .Select(group => group.FirstOrDefault(backend => backend.Available) ?? group.First())
            .OrderBy(backend => backend.Backend)
            .ToList();
    }

    public GpuBackend ResolveBackend(UserConfiguration config, GpuDevice? device = null)
    {
        if (!Enum.TryParse<GpuBackend>(config.Runtime.GpuBackend, ignoreCase: true, out var configured)
            || configured == GpuBackend.Auto)
        {
            device ??= ResolveSelectedDevice(config);
            return RecommendBackend(device, DetectBackends());
        }

        return configured;
    }

    public GpuDevice? ResolveSelectedDevice(UserConfiguration config)
    {
        var devices = DetectGpus();
        if (devices.Count == 0)
        {
            return null;
        }

        if (config.Runtime.GpuDeviceIndex is >= 0 and var index && index < devices.Count)
        {
            return devices[index];
        }

        if (!string.IsNullOrWhiteSpace(config.Runtime.GpuDeviceName))
        {
            var byName = devices.FirstOrDefault(device =>
                device.Name.Contains(config.Runtime.GpuDeviceName, StringComparison.OrdinalIgnoreCase));
            if (byName is not null)
            {
                return byName;
            }
        }

        return RecommendDevice(devices);
    }

    public static GpuDevice RecommendDevice(IReadOnlyList<GpuDevice> devices)
    {
        // Prefer discrete NVIDIA/AMD, then Intel iGPU, then anything else.
        return devices
                   .OrderByDescending(device => device.IsLikelyDiscrete)
                   .ThenBy(device => device.Vendor switch
                   {
                       GpuVendor.Nvidia => 0,
                       GpuVendor.Amd => 1,
                       GpuVendor.Intel => 2,
                       _ => 3,
                   })
                   .ThenBy(device => device.Index)
                   .First();
    }

    public static GpuBackend RecommendBackend(
        GpuDevice? device,
        IReadOnlyList<GpuBackendInfo> backends)
    {
        bool Has(GpuBackend backend) =>
            backends.Any(info => info.Backend == backend && info.Available);

        if (device is null || device.Vendor == GpuVendor.Cpu)
        {
            return GpuBackend.Cpu;
        }

        return device.Vendor switch
        {
            GpuVendor.Nvidia when Has(GpuBackend.Cuda) => GpuBackend.Cuda,
            GpuVendor.Nvidia when Has(GpuBackend.Vulkan) => GpuBackend.Vulkan,
            GpuVendor.Amd when Has(GpuBackend.Rocm) => GpuBackend.Rocm,
            GpuVendor.Amd when Has(GpuBackend.Vulkan) => GpuBackend.Vulkan,
            GpuVendor.Intel when Has(GpuBackend.Vulkan) => GpuBackend.Vulkan,
            _ when Has(GpuBackend.Vulkan) => GpuBackend.Vulkan,
            _ => GpuBackend.Cpu,
        };
    }

    public HardwareRecommendation Recommend()
    {
        var devices = DetectGpus();
        var backends = DetectBackends();
        var selected = RecommendDevice(devices);
        var backend = RecommendBackend(selected, backends);
        var logicalCores = Environment.ProcessorCount;
        var threads = Math.Clamp(logicalCores, 4, 16);

        var (gpuLayers, context, profileId, notes) = selected.Vendor switch
        {
            GpuVendor.Nvidia when selected.IsLikelyDiscrete => (
                99,
                16384,
                "nvidia-cuda",
                new[]
                {
                    $"Detected NVIDIA GPU: {selected.Name}",
                    "Prefer CUDA if ggml-cuda.dll is present, else Vulkan",
                    "Use high layer offload (-ngl 99) on discrete NVIDIA",
                }),
            GpuVendor.Amd when selected.IsLikelyDiscrete => (
                99,
                16384,
                "amd-rocm",
                new[]
                {
                    $"Detected AMD GPU: {selected.Name}",
                    "Prefer ROCm/HIP if available, else Vulkan",
                    "High layer offload recommended on discrete AMD",
                }),
            GpuVendor.Intel => (
                29,
                16384,
                "iris-xe-coding",
                new[]
                {
                    $"Detected Intel GPU: {selected.Name}",
                    "Use Vulkan backend (ggml-vulkan.dll)",
                    "Shared memory — try balanced/cpu-only if unstable",
                }),
            _ => (
                0,
                8192,
                "cpu-only",
                new[]
                {
                    "No suitable GPU detected — using CPU",
                    "Install a Vulkan/CUDA/ROCm llama.cpp build for GPU offload",
                }),
        };

        if (backend == GpuBackend.Cpu)
        {
            gpuLayers = 0;
            profileId = "cpu-only";
        }

        return new HardwareRecommendation
        {
            LogicalCores = logicalCores,
            RecommendedThreads = threads,
            RecommendedGpuLayers = gpuLayers,
            RecommendedContextSize = context,
            RecommendedProfileId = profileId,
            RecommendedBackend = backend.ToString().ToLowerInvariant(),
            RecommendedGpuIndex = selected.Index,
            RecommendedGpuName = selected.Name,
            DetectedGpus = devices,
            AvailableBackends = backends,
            Notes = notes,
        };
    }

    public void ApplySelection(int deviceIndex, string? backendOverride = null, bool requireBackendAvailable = true)
    {
        var devices = DetectGpus();
        if (deviceIndex < 0 || deviceIndex >= devices.Count)
        {
            throw new InvalidOperationException(
                $"GPU index out of range. Valid: 0..{Math.Max(0, devices.Count - 1)}. Run: llm gpu list");
        }

        var device = devices[deviceIndex];
        var backends = DetectBackends();
        var backend = string.IsNullOrWhiteSpace(backendOverride)
            ? RecommendBackend(device, backends)
            : ParseBackend(backendOverride);

        if (requireBackendAvailable
            && backend != GpuBackend.Cpu
            && backends.All(info => info.Backend != backend || !info.Available))
        {
            throw new InvalidOperationException(
                $"Backend '{backend}' is not available in the managed runtime. " +
                "Import/install a build with the matching ggml backend DLL.");
        }

        var config = _userData.Load();
        config.Runtime.GpuDeviceIndex = device.Index;
        config.Runtime.GpuDeviceName = device.Name;
        config.Runtime.GpuBackend = backend.ToString().ToLowerInvariant();
        config.Runtime.GpuLayers = backend == GpuBackend.Cpu
            ? 0
            : device.Vendor == GpuVendor.Intel
                ? Math.Clamp(config.Runtime.GpuLayers <= 0 ? 29 : config.Runtime.GpuLayers, 1, 99)
                : Math.Max(config.Runtime.GpuLayers, 35);

        _userData.Save(config);
    }

    public void ApplyCpuOnly()
    {
        var config = _userData.Load();
        config.Runtime.GpuBackend = "cpu";
        config.Runtime.GpuLayers = 0;
        config.Runtime.GpuDeviceIndex = null;
        config.Runtime.GpuDeviceName = "CPU";
        _userData.Save(config);
    }

    public static string ResolveProfileId(GpuDevice? device, string? backend = null)
    {
        if (device is null
            || device.Vendor == GpuVendor.Cpu
            || string.Equals(backend, "cpu", StringComparison.OrdinalIgnoreCase))
        {
            return "cpu-only";
        }

        return device.Vendor switch
        {
            GpuVendor.Nvidia => "nvidia-cuda",
            GpuVendor.Amd => "amd-rocm",
            GpuVendor.Intel => "iris-xe-coding",
            _ => "balanced",
        };
    }

    public static void ApplyGpuEnvironment(
        ProcessStartInfo startInfo,
        RuntimeConfiguration runtime,
        IReadOnlyList<GpuDevice>? devices = null)
    {
        var backend = ParseBackend(runtime.GpuBackend);
        var absoluteIndex = runtime.GpuDeviceIndex ?? 0;
        var relativeIndex = ResolveVendorRelativeIndex(absoluteIndex, backend, devices);

        switch (backend)
        {
            case GpuBackend.Cuda:
                startInfo.Environment["CUDA_VISIBLE_DEVICES"] =
                    relativeIndex.ToString(CultureInfo.InvariantCulture);
                break;

            case GpuBackend.Rocm:
                startInfo.Environment["HIP_VISIBLE_DEVICES"] =
                    relativeIndex.ToString(CultureInfo.InvariantCulture);
                startInfo.Environment["ROCR_VISIBLE_DEVICES"] =
                    relativeIndex.ToString(CultureInfo.InvariantCulture);
                break;

            case GpuBackend.Vulkan:
                startInfo.Environment["GGML_VK_VISIBLE_DEVICES"] =
                    relativeIndex.ToString(CultureInfo.InvariantCulture);

                // Intel Iris Xe: F16 Vulkan paths can produce garbage output on some drivers.
                // Opt out with config Runtime:VulkanDisableF16 = false (via env already set externally).
                var selected = devices is null || absoluteIndex < 0 || absoluteIndex >= devices.Count
                    ? null
                    : devices[absoluteIndex];
                if (selected?.Vendor == GpuVendor.Intel
                    && !string.Equals(
                        Environment.GetEnvironmentVariable("LLM_ALLOW_VK_F16"),
                        "1",
                        StringComparison.Ordinal))
                {
                    startInfo.Environment["GGML_VK_DISABLE_F16"] = "1";
                }

                break;

            case GpuBackend.Cpu:
                break;
        }
    }

    public static int ResolveVendorRelativeIndex(
        int absoluteIndex,
        GpuBackend backend,
        IReadOnlyList<GpuDevice>? devices)
    {
        if (devices is null || devices.Count == 0)
        {
            return Math.Max(0, absoluteIndex);
        }

        var vendor = backend switch
        {
            GpuBackend.Cuda => GpuVendor.Nvidia,
            GpuBackend.Rocm => GpuVendor.Amd,
            _ => devices.FirstOrDefault(device => device.Index == absoluteIndex)?.Vendor
                 ?? GpuVendor.Unknown,
        };

        var sameVendor = devices
            .Where(device => vendor == GpuVendor.Unknown || device.Vendor == vendor)
            .OrderBy(device => device.Index)
            .ToList();

        if (sameVendor.Count == 0)
        {
            return 0;
        }

        var match = sameVendor.FindIndex(device => device.Index == absoluteIndex);
        return match >= 0 ? match : 0;
    }

    public static GpuBackend ParseBackend(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return GpuBackend.Auto;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "auto" => GpuBackend.Auto,
            "cpu" => GpuBackend.Cpu,
            "vulkan" or "vk" => GpuBackend.Vulkan,
            "cuda" or "nvidia" => GpuBackend.Cuda,
            "rocm" or "hip" or "amd" => GpuBackend.Rocm,
            _ when Enum.TryParse(value, ignoreCase: true, out GpuBackend parsed) => parsed,
            _ => throw new InvalidOperationException(
                $"Unknown backend '{value}'. Use: auto, cpu, vulkan, cuda, rocm"),
        };
    }

    private string? ResolveRuntimeDirectory()
    {
        var config = _userData.Load();
        if (string.IsNullOrWhiteSpace(config.Runtime.ExecutablePath))
        {
            return null;
        }

        return Path.GetDirectoryName(config.Runtime.ExecutablePath);
    }

    private static void AddIfPresent(
        List<GpuBackendInfo> backends,
        string runtimeDirectory,
        GpuBackend backend,
        string fileName)
    {
        var path = Path.Combine(runtimeDirectory, fileName);
        if (File.Exists(path))
        {
            backends.Add(new GpuBackendInfo
            {
                Backend = backend,
                Available = true,
                LibraryPath = path,
            });
        }
    }

    private static IReadOnlyList<GpuDevice> DetectViaWmi()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments =
                    "-NoProfile -Command \"Get-CimInstance Win32_VideoController | " +
                    "Select-Object Name,AdapterRAM,DriverVersion,PNPDeviceID | " +
                    "ConvertTo-Csv -NoTypeInformation\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            using var process = Process.Start(psi);
            if (process is null)
            {
                return [];
            }

            var csv = process.StandardOutput.ReadToEnd();
            process.WaitForExit(8000);

            return ParseGpuCsv(csv);
        }
        catch
        {
            return [];
        }
    }

    public static IReadOnlyList<GpuDevice> ParseGpuCsv(string csv)
    {
        var lines = csv
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .ToList();

        if (lines.Count < 2)
        {
            return [];
        }

        var devices = new List<GpuDevice>();
        var index = 0;

        foreach (var line in lines.Skip(1))
        {
            var fields = SplitCsvLine(line);
            if (fields.Count < 1 || string.IsNullOrWhiteSpace(fields[0]))
            {
                continue;
            }

            var name = fields[0].Trim();
            if (ShouldIgnoreAdapter(name))
            {
                continue;
            }

            long adapterRam = 0;
            if (fields.Count > 1)
            {
                long.TryParse(fields[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out adapterRam);
            }

            var driver = fields.Count > 2 ? fields[2].Trim() : "";
            var pnp = fields.Count > 3 ? fields[3].Trim() : "";
            var vendor = DetectVendor(name, pnp);
            var discrete = IsLikelyDiscrete(name, vendor, adapterRam);

            devices.Add(new GpuDevice
            {
                Index = index++,
                Name = name,
                Vendor = vendor,
                AdapterRamBytes = adapterRam > 0 ? adapterRam : 0,
                DriverVersion = driver,
                PnpDeviceId = pnp,
                IsLikelyDiscrete = discrete,
            });
        }

        return devices;
    }

    public static bool ShouldIgnoreAdapter(string name)
    {
        ReadOnlySpan<string> ignored =
        [
            "Microsoft Basic Render",
            "Remote Desktop",
            "Virtual Adapter",
            "Virtual Display",
            "SuperDisplay",
            "Parsec Virtual",
            "VMware",
            "VirtualBox",
            "Hyper-V",
            "Citrix",
        ];

        foreach (var token in ignored)
        {
            if (name.Contains(token, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public static GpuVendor DetectVendor(string name, string pnpDeviceId)
    {
        var venMatch = VendorRegex.Match(pnpDeviceId);
        if (venMatch.Success)
        {
            return venMatch.Groups[1].Value.ToUpperInvariant() switch
            {
                "10DE" => GpuVendor.Nvidia,
                "1002" or "1022" => GpuVendor.Amd,
                "8086" => GpuVendor.Intel,
                _ => InferVendorFromName(name),
            };
        }

        return InferVendorFromName(name);
    }

    public static bool IsLikelyDiscrete(string name, GpuVendor vendor, long adapterRamBytes)
    {
        if (vendor == GpuVendor.Intel)
        {
            // Arc is discrete; Iris/UHD are integrated.
            return name.Contains("Arc", StringComparison.OrdinalIgnoreCase);
        }

        if (name.Contains("GeForce", StringComparison.OrdinalIgnoreCase)
            || name.Contains("RTX", StringComparison.OrdinalIgnoreCase)
            || name.Contains("GTX", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Quadro", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Radeon RX", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Radeon Pro", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (vendor is GpuVendor.Nvidia or GpuVendor.Amd)
        {
            return adapterRamBytes >= 2L * 1024 * 1024 * 1024
                   || !name.Contains("Graphics", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private static GpuVendor InferVendorFromName(string name)
    {
        if (name.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase)
            || name.Contains("GeForce", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Quadro", StringComparison.OrdinalIgnoreCase)
            || name.Contains("RTX", StringComparison.OrdinalIgnoreCase)
            || name.Contains("GTX", StringComparison.OrdinalIgnoreCase))
        {
            return GpuVendor.Nvidia;
        }

        if (name.Contains("AMD", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Radeon", StringComparison.OrdinalIgnoreCase)
            || name.Contains("ATI", StringComparison.OrdinalIgnoreCase))
        {
            return GpuVendor.Amd;
        }

        if (name.Contains("Intel", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Iris", StringComparison.OrdinalIgnoreCase)
            || name.Contains("UHD", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Arc", StringComparison.OrdinalIgnoreCase))
        {
            return GpuVendor.Intel;
        }

        return GpuVendor.Unknown;
    }

    private static List<string> SplitCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        foreach (var ch in line)
        {
            if (ch == '"')
            {
                inQuotes = !inQuotes;
                continue;
            }

            if (ch == ',' && !inQuotes)
            {
                fields.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(ch);
        }

        fields.Add(current.ToString());
        return fields;
    }
}
