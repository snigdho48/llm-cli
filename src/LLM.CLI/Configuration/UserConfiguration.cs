namespace LLM.CLI.Configuration;

public sealed class UserConfiguration
{
    public string RootDirectory { get; set; } = GetDefaultRootDirectory();

    public string? ActiveProfileId { get; set; }

    public RuntimeConfiguration Runtime { get; set; } = new();

    public static string GetDefaultRootDirectory()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "LLM");
    }
}

public sealed class RuntimeConfiguration
{
    public string Provider { get; set; } = "llama.cpp";

    public string Version { get; set; } = "";

    public string ExecutablePath { get; set; } = "";

    public string SourceBuildPath { get; set; } = "";

    public string? ActiveModelPath { get; set; }

    public int Port { get; set; } = 8080;

    public string Host { get; set; } = "127.0.0.1";

    public int GpuLayers { get; set; } = 29;

    public int ContextSize { get; set; } = 16384;

    public int Threads { get; set; } = 8;

    /// <summary>
    /// auto | cpu | vulkan | cuda | rocm
    /// </summary>
    public string GpuBackend { get; set; } = "auto";

    /// <summary>
    /// Zero-based index into detected GPU list (llm gpu list).
    /// </summary>
    public int? GpuDeviceIndex { get; set; }

    /// <summary>
    /// Friendly name of the selected GPU (informational).
    /// </summary>
    public string? GpuDeviceName { get; set; }

    public string ApiKey { get; set; } = "local-ai";

    public string ModelsDirectory { get; set; } = "models";
}
