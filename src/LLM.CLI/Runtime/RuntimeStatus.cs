namespace LLM.CLI.Runtime;

public sealed class RuntimeStatus
{
    public bool IsRunning { get; set; }

    public bool IsInstalled { get; set; }

    public int? ProcessId { get; set; }

    public string? Provider { get; set; }

    public string Host { get; set; } = "127.0.0.1";

    public int Port { get; set; }

    public string ExecutablePath { get; set; } = "";

    public string? ActiveModelPath { get; set; }

    public int GpuLayers { get; set; }

    public int ContextSize { get; set; }
}