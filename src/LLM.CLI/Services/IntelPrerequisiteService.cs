using System.Diagnostics;
using Microsoft.Win32;
using LLM.CLI.Configuration;

namespace LLM.CLI.Services;

public sealed class IntelPrerequisiteService
{
    public const string VulkanWingetId = "KhronosGroup.VulkanRT";
    public const string OneApiWingetId = "Intel.OneAPI.BaseToolkit";

    private readonly UserDataService _userData;
    private readonly GpuDetectionService _gpus;
    private readonly WingetService _winget;

    public IntelPrerequisiteService(
        UserDataService userData,
        GpuDetectionService gpus,
        WingetService winget)
    {
        _userData = userData;
        _gpus = gpus;
        _winget = winget;
    }

    public bool HasIntelGpu()
    {
        return _gpus.DetectGpus().Any(device => device.Vendor == GpuVendor.Intel);
    }

    public IntelPrerequisiteReport Check()
    {
        var config = _userData.Load();
        var report = new IntelPrerequisiteReport
        {
            HasIntelGpu = HasIntelGpu(),
            IntelGpuName = _gpus.DetectGpus()
                .FirstOrDefault(device => device.Vendor == GpuVendor.Intel)?.Name,
        };

        report.Checks.Add(CheckVulkanRuntimeDll());
        report.Checks.Add(CheckVulkanIcd());
        report.Checks.Add(CheckManagedVulkanBackend(config));
        report.Checks.Add(CheckOneApiRoot());
        report.Checks.Add(CheckLevelZeroLoader());
        report.Checks.Add(CheckIntelGraphicsDriver());
        report.Checks.Add(CheckVulkaninfoIntelDevice());

        return report;
    }

    public async Task<IntelFixResult> FixAsync(
        bool installVulkan = true,
        bool installOneApi = false,
        bool openDriverPage = true,
        CancellationToken cancellationToken = default)
    {
        var report = Check();
        var actions = new List<string>();
        var failures = new List<string>();

        if (!report.HasIntelGpu)
        {
            return new IntelFixResult(
                false,
                ["No Intel GPU detected — Intel Vulkan/oneAPI setup skipped."],
                []);
        }

        if (installVulkan && !report.VulkanReady)
        {
            if (!_winget.IsAvailable())
            {
                failures.Add("winget is not available. Install Vulkan Runtime manually (see llm gpu doctor).");
            }
            else
            {
                actions.Add($"Installing Vulkan Runtime via winget ({VulkanWingetId})...");
                var result = await _winget.InstallAsync(VulkanWingetId, cancellationToken);
                if (result.Success)
                {
                    actions.Add("[ OK ] Vulkan Runtime installed (or already present).");
                }
                else
                {
                    failures.Add($"Vulkan winget install failed: {result.Detail}");
                    actions.Add("Manual: winget install -e --id KhronosGroup.VulkanRT");
                }
            }
        }

        if (installOneApi && !report.OneApiReady)
        {
            if (!_winget.IsAvailable())
            {
                failures.Add("winget is not available. Install Intel oneAPI Base Toolkit manually.");
            }
            else
            {
                actions.Add($"Installing Intel oneAPI Base Toolkit via winget ({OneApiWingetId})...");
                actions.Add("(This is a large download — may take a long time.)");
                var result = await _winget.InstallAsync(OneApiWingetId, cancellationToken);
                if (result.Success)
                {
                    actions.Add("[ OK ] oneAPI Base Toolkit installed (or already present).");
                }
                else
                {
                    failures.Add($"oneAPI winget install failed: {result.Detail}");
                    actions.Add("Manual download: https://www.intel.com/content/www/us/en/developer/tools/oneapi/base-toolkit-download.html");
                }
            }
        }

        if (openDriverPage && !report.IntelDriverLooksPresent)
        {
            actions.Add("Opening Intel Driver & Support Assistant download page...");
            TryOpenUrl("https://www.intel.com/content/www/us/en/support/detect.html");
            actions.Add("Install/update Intel Graphics drivers, then reboot if prompted.");
        }

        // Re-check after installs
        var after = Check();
        return new IntelFixResult(after.VulkanReady, actions, failures, after);
    }

    public static IReadOnlyList<string> GetManualInstructions(IntelPrerequisiteReport report)
    {
        var lines = new List<string>();

        if (!report.HasIntelGpu)
        {
            lines.Add("No Intel GPU detected. Use: llm gpu list");
            return lines;
        }

        lines.Add($"Intel GPU: {report.IntelGpuName}");
        lines.Add("");

        if (!report.VulkanReady)
        {
            lines.Add("=== Vulkan (required for llama.cpp Iris Xe / Arc) ===");
            lines.Add("1. Install Vulkan Runtime:");
            lines.Add("     winget install -e --id KhronosGroup.VulkanRT");
            lines.Add("2. Update Intel Graphics driver (includes Vulkan ICD):");
            lines.Add("     https://www.intel.com/content/www/us/en/support/detect.html");
            lines.Add("3. Ensure managed runtime has ggml-vulkan.dll:");
            lines.Add("     llm runtime import <path-to-vulkan-llama.cpp>");
            lines.Add("   or: llm runtime install");
            lines.Add("4. Select Vulkan backend:");
            lines.Add("     llm gpu use 0 --backend vulkan --apply-profile");
            lines.Add("     llm runtime restart");
            lines.Add("");
            lines.Add("Automate steps 1 (+ optional oneAPI):");
            lines.Add("     llm gpu fix --vulkan");
            lines.Add("");
        }
        else
        {
            lines.Add("[ OK ] Vulkan prerequisites look ready for Intel GPU.");
            lines.Add("");
        }

        if (!report.OneApiReady)
        {
            lines.Add("=== Intel oneAPI / SYCL (optional — for SYCL builds, not required for Vulkan) ===");
            lines.Add("Vulkan is enough for llama.cpp on Iris Xe. Install oneAPI only if you");
            lines.Add("build/run a SYCL/oneAPI llama.cpp backend.");
            lines.Add("");
            lines.Add("1. Install Intel oneAPI Base Toolkit:");
            lines.Add("     winget install -e --id Intel.OneAPI.BaseToolkit");
            lines.Add("   or download:");
            lines.Add("     https://www.intel.com/content/www/us/en/developer/tools/oneapi/base-toolkit-download.html");
            lines.Add("2. After install, open \"Intel oneAPI\" command prompt or run:");
            lines.Add("     \"C:\\Program Files (x86)\\Intel\\oneAPI\\setvars.bat\"");
            lines.Add("3. Verify Level Zero loader exists (ze_loader.dll under oneAPI).");
            lines.Add("");
            lines.Add("Automate:");
            lines.Add("     llm gpu fix --oneapi");
            lines.Add("");
        }
        else
        {
            lines.Add("[ OK ] Intel oneAPI appears installed.");
            lines.Add($"      Root: {report.OneApiRoot ?? "-"}");
            lines.Add("");
        }

        if (report.VulkanReady && report.ManagedVulkanBackendOk)
        {
            lines.Add("Recommended next steps:");
            lines.Add("  llm gpu use 0 --backend vulkan --apply-profile");
            lines.Add("  llm serve");
            lines.Add("  llm bench");
        }

        return lines;
    }

    private static PrerequisiteCheck CheckVulkanRuntimeDll()
    {
        var system32 = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.System),
            "vulkan-1.dll");
        var exists = File.Exists(system32);

        return new PrerequisiteCheck(
            "Vulkan Runtime (vulkan-1.dll)",
            exists,
            exists ? system32 : "Missing — install Khronos Vulkan Runtime",
            canAutoFix: true,
            autoFixHint: "llm gpu fix --vulkan");
    }

    private static PrerequisiteCheck CheckVulkanIcd()
    {
        var intelIcd = FindIntelVulkanIcd();
        if (intelIcd is not null)
        {
            return new PrerequisiteCheck(
                "Intel Vulkan ICD",
                true,
                intelIcd,
                canAutoFix: false);
        }

        if (!OperatingSystem.IsWindows())
        {
            return new PrerequisiteCheck(
                "Intel Vulkan ICD",
                true,
                "Skipped (non-Windows)",
                canAutoFix: false);
        }

        try
        {
#pragma warning disable CA1416
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Khronos\Vulkan\Drivers");
            using var keyWow = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Khronos\Vulkan\Drivers");
            var count = (key?.ValueCount ?? 0) + (keyWow?.ValueCount ?? 0);
#pragma warning restore CA1416

            if (count > 0)
            {
                return new PrerequisiteCheck(
                    "Intel Vulkan ICD",
                    true,
                    $"{count} ICD entry(ies) in registry",
                    canAutoFix: false);
            }
        }
        catch
        {
            // fall through
        }

        return new PrerequisiteCheck(
            "Intel Vulkan ICD",
            false,
            "Intel igvk64.json / Vulkan ICD not found — update Intel Graphics driver",
            canAutoFix: false,
            autoFixHint: "https://www.intel.com/content/www/us/en/support/detect.html");
    }

    private static string? FindIntelVulkanIcd()
    {
        var driverStore = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.System),
            "DriverStore",
            "FileRepository");

        if (!Directory.Exists(driverStore))
        {
            return null;
        }

        try
        {
            return Directory
                .EnumerateFiles(driverStore, "igvk64.json", SearchOption.AllDirectories)
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault()
                ?? Directory
                    .EnumerateFiles(driverStore, "igvk32.json", SearchOption.AllDirectories)
                    .OrderByDescending(File.GetLastWriteTimeUtc)
                    .FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    private static PrerequisiteCheck CheckVulkaninfoIntelDevice()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "vulkaninfo",
                Arguments = "--summary",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            using var process = Process.Start(psi);
            if (process is null)
            {
                return new PrerequisiteCheck(
                    "vulkaninfo (Intel device)",
                    false,
                    "Unable to start vulkaninfo",
                    canAutoFix: true,
                    autoFixHint: "llm gpu fix --vulkan");
            }

            var output = process.StandardOutput.ReadToEnd();
            if (!process.WaitForExit(8000))
            {
                try { process.Kill(true); } catch { /* ignore */ }
                return new PrerequisiteCheck(
                    "vulkaninfo (Intel device)",
                    false,
                    "vulkaninfo timed out",
                    canAutoFix: false);
            }

            var hasIntel = output.Contains("Intel", StringComparison.OrdinalIgnoreCase)
                           || output.Contains("Iris", StringComparison.OrdinalIgnoreCase)
                           || output.Contains("Arc", StringComparison.OrdinalIgnoreCase);

            if (process.ExitCode != 0 && string.IsNullOrWhiteSpace(output))
            {
                return new PrerequisiteCheck(
                    "vulkaninfo (Intel device)",
                    false,
                    "vulkaninfo failed — install Vulkan Runtime + Intel driver",
                    canAutoFix: true,
                    autoFixHint: "llm gpu fix --vulkan");
            }

            // Extract first GPU deviceName line if present
            var detail = hasIntel
                ? "Intel GPU visible to Vulkan"
                : "vulkaninfo ran but no Intel GPU listed";

            var deviceLine = output
                .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault(line =>
                    line.Contains("deviceName", StringComparison.OrdinalIgnoreCase)
                    || line.Contains("Intel", StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(deviceLine))
            {
                detail = deviceLine.Trim();
            }

            return new PrerequisiteCheck(
                "vulkaninfo (Intel device)",
                hasIntel || process.ExitCode == 0,
                detail,
                canAutoFix: !hasIntel,
                autoFixHint: hasIntel ? null : "Update Intel Graphics driver via Intel DSA");
        }
        catch (Exception ex)
        {
            return new PrerequisiteCheck(
                "vulkaninfo (Intel device)",
                false,
                $"vulkaninfo not available ({ex.Message})",
                canAutoFix: true,
                autoFixHint: "llm gpu fix --vulkan");
        }
    }

    private static PrerequisiteCheck CheckManagedVulkanBackend(UserConfiguration config)
    {
        if (string.IsNullOrWhiteSpace(config.Runtime.ExecutablePath))
        {
            return new PrerequisiteCheck(
                "Managed ggml-vulkan.dll",
                false,
                "Runtime not installed — llm runtime import / install",
                canAutoFix: false,
                autoFixHint: "llm runtime install");
        }

        var dir = Path.GetDirectoryName(config.Runtime.ExecutablePath);
        if (dir is null)
        {
            return new PrerequisiteCheck("Managed ggml-vulkan.dll", false, "Invalid runtime path");
        }

        var dll = Path.Combine(dir, "ggml-vulkan.dll");
        var ok = File.Exists(dll);

        return new PrerequisiteCheck(
            "Managed ggml-vulkan.dll",
            ok,
            ok ? dll : "Missing — import a Vulkan-enabled llama.cpp build",
            canAutoFix: false,
            autoFixHint: "llm runtime import C:\\llama.cpp");
    }

    private static PrerequisiteCheck CheckOneApiRoot()
    {
        var roots = new[]
        {
            Environment.GetEnvironmentVariable("ONEAPI_ROOT"),
            Environment.GetEnvironmentVariable("SETVARS_CALL"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Intel", "oneAPI"),
            @"C:\Program Files (x86)\Intel\oneAPI",
        };

        foreach (var root in roots.Where(path => !string.IsNullOrWhiteSpace(path)))
        {
            var setvars = Path.Combine(root!, "setvars.bat");
            if (File.Exists(setvars) || Directory.Exists(root))
            {
                var ok = File.Exists(setvars) || Directory.Exists(root);
                return new PrerequisiteCheck(
                    "Intel oneAPI root",
                    File.Exists(setvars),
                    File.Exists(setvars) ? setvars : $"Found folder but no setvars.bat: {root}",
                    canAutoFix: true,
                    autoFixHint: "llm gpu fix --oneapi");
            }
        }

        return new PrerequisiteCheck(
            "Intel oneAPI root",
            false,
            "Not found (optional for Vulkan llama.cpp)",
            canAutoFix: true,
            autoFixHint: "llm gpu fix --oneapi");
    }

    private static PrerequisiteCheck CheckLevelZeroLoader()
    {
        var candidates = new List<string>();
        var oneApi = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            "Intel",
            "oneAPI");

        if (Directory.Exists(oneApi))
        {
            try
            {
                candidates.AddRange(
                    Directory.EnumerateFiles(oneApi, "ze_loader.dll", SearchOption.AllDirectories)
                        .Take(3));
            }
            catch
            {
                // ignore permission issues
            }
        }

        var system = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.System),
            "ze_loader.dll");
        if (File.Exists(system))
        {
            candidates.Add(system);
        }

        var ok = candidates.Count > 0;
        return new PrerequisiteCheck(
            "Level Zero loader (ze_loader.dll)",
            ok,
            ok ? candidates[0] : "Not found (needed for SYCL/oneAPI GPU, optional for Vulkan)",
            canAutoFix: true,
            autoFixHint: "llm gpu fix --oneapi");
    }

    private PrerequisiteCheck CheckIntelGraphicsDriver()
    {
        var intel = _gpus.DetectGpus().FirstOrDefault(device => device.Vendor == GpuVendor.Intel);
        if (intel is null)
        {
            return new PrerequisiteCheck("Intel Graphics driver", false, "No Intel GPU");
        }

        var ok = !string.IsNullOrWhiteSpace(intel.DriverVersion);
        return new PrerequisiteCheck(
            "Intel Graphics driver",
            ok,
            ok
                ? $"{intel.Name} — driver {intel.DriverVersion}"
                : "Driver version unknown — run Intel Driver & Support Assistant",
            canAutoFix: false,
            autoFixHint: "https://www.intel.com/content/www/us/en/support/detect.html");
    }

    private static void TryOpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true,
            });
        }
        catch
        {
            // ignore
        }
    }
}

public sealed class IntelPrerequisiteReport
{
    public bool HasIntelGpu { get; init; }

    public string? IntelGpuName { get; init; }

    public List<PrerequisiteCheck> Checks { get; } = [];

    public bool VulkanReady =>
        Checks.Where(check =>
                check.Name.Contains("Vulkan Runtime", StringComparison.OrdinalIgnoreCase)
                || check.Name.Contains("Intel Vulkan ICD", StringComparison.OrdinalIgnoreCase)
                || check.Name.Contains("ggml-vulkan", StringComparison.OrdinalIgnoreCase))
            .All(check => check.Ok);

    public bool ManagedVulkanBackendOk =>
        Checks.Any(check =>
            check.Name.Contains("ggml-vulkan", StringComparison.OrdinalIgnoreCase) && check.Ok);

    public bool OneApiReady =>
        Checks.Any(check =>
            check.Name.Contains("oneAPI root", StringComparison.OrdinalIgnoreCase) && check.Ok);

    public bool IntelDriverLooksPresent =>
        Checks.Any(check =>
            check.Name.Contains("Intel Graphics driver", StringComparison.OrdinalIgnoreCase) && check.Ok);

    public string? OneApiRoot =>
        Checks.FirstOrDefault(check =>
                check.Name.Contains("oneAPI root", StringComparison.OrdinalIgnoreCase) && check.Ok)
            ?.Detail;

    public bool AllCriticalOk => VulkanReady;
}

public sealed class PrerequisiteCheck
{
    public PrerequisiteCheck(
        string name,
        bool ok,
        string detail,
        bool canAutoFix = false,
        string? autoFixHint = null)
    {
        Name = name;
        Ok = ok;
        Detail = detail;
        CanAutoFix = canAutoFix;
        AutoFixHint = autoFixHint;
    }

    public string Name { get; }

    public bool Ok { get; }

    public string Detail { get; }

    public bool CanAutoFix { get; }

    public string? AutoFixHint { get; }
}

public sealed class IntelFixResult
{
    public IntelFixResult(
        bool vulkanReady,
        IReadOnlyList<string> actions,
        IReadOnlyList<string> failures,
        IntelPrerequisiteReport? report = null)
    {
        VulkanReady = vulkanReady;
        Actions = actions;
        Failures = failures;
        Report = report;
    }

    public bool VulkanReady { get; }

    public IReadOnlyList<string> Actions { get; }

    public IReadOnlyList<string> Failures { get; }

    public IntelPrerequisiteReport? Report { get; }
}
