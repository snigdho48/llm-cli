using System.Text.Json;
using LLM.CLI.Configuration;

namespace LLM.CLI.Services;

public sealed class UserDataService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// Optional override for tests; when null, uses %LOCALAPPDATA%\LLM.
    /// </summary>
    public string? RootDirectoryOverride { get; set; }

    public string RootDirectory =>
        RootDirectoryOverride ??
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LLM");

    public string ConfigFile =>
        Path.Combine(RootDirectory, "config.json");

    public void EnsureCreated()
    {
        Directory.CreateDirectory(RootDirectory);

        if (!File.Exists(ConfigFile))
        {
            WriteConfiguration(new UserConfiguration());
        }
    }

    public UserConfiguration Load()
    {
        EnsureCreated();

        var json = File.ReadAllText(ConfigFile);

        var configuration = JsonSerializer.Deserialize<UserConfiguration>(json)
                            ?? new UserConfiguration();

        configuration.RootDirectory = string.IsNullOrWhiteSpace(configuration.RootDirectory)
            ? UserConfiguration.GetDefaultRootDirectory()
            : configuration.RootDirectory;

        configuration.Runtime ??= new RuntimeConfiguration();
        MigrateRuntimeDefaults(configuration.Runtime);

        return configuration;
    }

    public void Save(UserConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        Directory.CreateDirectory(RootDirectory);
        MigrateRuntimeDefaults(configuration.Runtime);

        WriteConfiguration(configuration);
    }

    private static void MigrateRuntimeDefaults(RuntimeConfiguration runtime)
    {
        if (string.IsNullOrWhiteSpace(runtime.Provider))
        {
            runtime.Provider = "llama.cpp";
        }

        if (string.IsNullOrWhiteSpace(runtime.Host))
        {
            runtime.Host = "127.0.0.1";
        }

        if (runtime.Port <= 0)
        {
            runtime.Port = 8080;
        }

        if (runtime.GpuLayers < 0)
        {
            runtime.GpuLayers = 29;
        }

        if (runtime.ContextSize <= 0)
        {
            runtime.ContextSize = 16384;
        }

        if (runtime.Threads <= 0)
        {
            runtime.Threads = Math.Clamp(Environment.ProcessorCount, 4, 8);
        }

        if (string.IsNullOrWhiteSpace(runtime.ApiKey))
        {
            runtime.ApiKey = "local-ai";
        }

        if (string.IsNullOrWhiteSpace(runtime.GpuBackend))
        {
            runtime.GpuBackend = "auto";
        }

        if (string.IsNullOrWhiteSpace(runtime.ModelsDirectory))
        {
            runtime.ModelsDirectory = "models";
        }
    }

    private void WriteConfiguration(UserConfiguration configuration)
    {
        File.WriteAllText(
            ConfigFile,
            JsonSerializer.Serialize(configuration, JsonOptions));
    }
}
