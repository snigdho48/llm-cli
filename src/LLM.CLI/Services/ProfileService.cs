using System.Text.Json;
using LLM.CLI.Configuration;

namespace LLM.CLI.Services;

public sealed class ProfileService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    private readonly UserDataService _userData;

    public ProfileService(UserDataService userData)
    {
        _userData = userData;
    }

    public string GetProfilesDirectory()
    {
        var config = _userData.Load();
        return Path.Combine(config.RootDirectory, "profiles");
    }

    public IReadOnlyList<RuntimeProfile> List()
    {
        EnsureDefaults();

        var directory = GetProfilesDirectory();
        if (!Directory.Exists(directory))
        {
            return [];
        }

        return Directory
            .EnumerateFiles(directory, "*.json")
            .Select(LoadFromFile)
            .OrderBy(profile => profile.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public RuntimeProfile Get(string id)
    {
        var path = GetProfilePath(id);
        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"Profile not found: {id}");
        }

        return LoadFromFile(path);
    }

    public void Apply(string id)
    {
        var profile = Get(id);
        var config = _userData.Load();

        config.Runtime.GpuLayers = profile.GpuLayers;
        config.Runtime.ContextSize = profile.ContextSize;
        config.Runtime.Threads = profile.Threads;
        if (!string.IsNullOrWhiteSpace(profile.GpuBackend))
        {
            config.Runtime.GpuBackend = profile.GpuBackend;
        }

        config.ActiveProfileId = profile.Id;

        _userData.Save(config);
    }

    public void EnsureDefaults()
    {
        var directory = GetProfilesDirectory();
        Directory.CreateDirectory(directory);

        WriteIfMissing(
            directory,
            "iris-xe-coding.json",
            new RuntimeProfile
            {
                Id = "iris-xe-coding",
                Name = "Iris Xe Coding",
                Description = "Tuned for Intel Iris Xe + Qwen2.5-Coder 7B on 32 GB RAM (Vulkan).",
                GpuLayers = 29,
                ContextSize = 16384,
                Threads = 8,
                GpuBackend = "vulkan",
                IsDefault = true,
            });

        WriteIfMissing(
            directory,
            "nvidia-cuda.json",
            new RuntimeProfile
            {
                Id = "nvidia-cuda",
                Name = "NVIDIA CUDA",
                Description = "Discrete NVIDIA GPU via CUDA (ggml-cuda.dll) or Vulkan fallback.",
                GpuLayers = 99,
                ContextSize = 16384,
                Threads = 8,
                GpuBackend = "cuda",
            });

        WriteIfMissing(
            directory,
            "amd-rocm.json",
            new RuntimeProfile
            {
                Id = "amd-rocm",
                Name = "AMD ROCm / Vulkan",
                Description = "Discrete AMD GPU via ROCm/HIP or Vulkan.",
                GpuLayers = 99,
                ContextSize = 16384,
                Threads = 8,
                GpuBackend = "rocm",
            });

        WriteIfMissing(
            directory,
            "balanced.json",
            new RuntimeProfile
            {
                Id = "balanced",
                Name = "Balanced",
                Description = "Lower GPU offload for stability when memory is tight.",
                GpuLayers = 20,
                ContextSize = 12288,
                Threads = 8,
                GpuBackend = "auto",
            });

        WriteIfMissing(
            directory,
            "cpu-only.json",
            new RuntimeProfile
            {
                Id = "cpu-only",
                Name = "CPU Only",
                Description = "No GPU offload — slower but most compatible.",
                GpuLayers = 0,
                ContextSize = 8192,
                Threads = 8,
                GpuBackend = "cpu",
            });
    }

    private string GetProfilePath(string id)
    {
        return Path.Combine(GetProfilesDirectory(), $"{SanitizeFileName(id)}.json");
    }

    private static void WriteIfMissing(string directory, string fileName, RuntimeProfile profile)
    {
        var path = Path.Combine(directory, fileName);
        if (File.Exists(path))
        {
            return;
        }

        File.WriteAllText(path, JsonSerializer.Serialize(profile, JsonOptions));
    }

    private static RuntimeProfile LoadFromFile(string path)
    {
        var json = File.ReadAllText(path);
        var profile = JsonSerializer.Deserialize<RuntimeProfile>(json)
                      ?? throw new InvalidOperationException($"Invalid profile file: {path}");

        if (string.IsNullOrWhiteSpace(profile.Id))
        {
            profile.Id = Path.GetFileNameWithoutExtension(path);
        }

        return profile;
    }

    private static string SanitizeFileName(string value)
    {
        foreach (var invalid in Path.GetInvalidFileNameChars())
        {
            value = value.Replace(invalid, '-');
        }

        return value.Trim().ToLowerInvariant();
    }
}
