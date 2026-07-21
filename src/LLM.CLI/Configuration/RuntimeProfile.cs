namespace LLM.CLI.Configuration;

public sealed class RuntimeProfile
{
    public string Id { get; set; } = "";

    public string Name { get; set; } = "";

    public string Description { get; set; } = "";

    public int GpuLayers { get; set; } = 29;

    public int ContextSize { get; set; } = 16384;

    public int Threads { get; set; } = 8;

    /// <summary>
    /// auto | cpu | vulkan | cuda | rocm
    /// </summary>
    public string GpuBackend { get; set; } = "auto";

    public bool IsDefault { get; set; }
}
