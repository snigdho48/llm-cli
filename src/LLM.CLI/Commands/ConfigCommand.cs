using LLM.CLI.Configuration;
using LLM.CLI.Services;

namespace LLM.CLI.Commands;

public sealed class ConfigCommand : ICommand
{
    private readonly ConfigurationService _configuration;
    private readonly UserDataService _userData;

    public ConfigCommand(ConfigurationService configuration, UserDataService userData)
    {
        _configuration = configuration;
        _userData = userData;
    }

    public string Name => "config";

    public Task<int> ExecuteAsync(string[] args)
    {
        if (args.Length == 0)
        {
            PrintHelp();
            return Task.FromResult(CommandResults.Success);
        }

        switch (args[0].ToLowerInvariant())
        {
            case "show":
                ShowConfiguration();
                return Task.FromResult(CommandResults.Success);

            case "set":
                return Task.FromResult(SetConfiguration(args));

            default:
                Console.WriteLine($"Unknown config command: {args[0]}");
                PrintHelp();
                return Task.FromResult(CommandResults.Failure);
        }
    }

    private void ShowConfiguration()
    {
        var config = _userData.Load();

        Console.WriteLine("LLM CLI Configuration");
        Console.WriteLine("---------------------");
        Console.WriteLine();
        Console.WriteLine($"Root Directory     : {config.RootDirectory}");
        Console.WriteLine($"Active Profile     : {config.ActiveProfileId ?? "-"}");
        Console.WriteLine($"Runtime Provider   : {config.Runtime.Provider}");
        Console.WriteLine($"Runtime Version    : {config.Runtime.Version}");
        Console.WriteLine($"Executable Path    : {config.Runtime.ExecutablePath}");
        Console.WriteLine($"Active Model       : {config.Runtime.ActiveModelPath ?? "-"}");
        Console.WriteLine($"Host               : {config.Runtime.Host}");
        Console.WriteLine($"Port               : {config.Runtime.Port}");
        Console.WriteLine($"GPU Layers         : {config.Runtime.GpuLayers}");
        Console.WriteLine($"GPU Backend        : {config.Runtime.GpuBackend}");
        Console.WriteLine(
            $"GPU Device         : [{config.Runtime.GpuDeviceIndex?.ToString() ?? "-"}] {config.Runtime.GpuDeviceName ?? "-"}");
        Console.WriteLine($"Context Size       : {config.Runtime.ContextSize}");
        Console.WriteLine($"Threads            : {config.Runtime.Threads}");
        Console.WriteLine($"API Key            : {config.Runtime.ApiKey}");
        Console.WriteLine($"Models Directory   : {config.Runtime.ModelsDirectory}");
    }

    private int SetConfiguration(string[] args)
    {
        if (args.Length < 3)
        {
            PrintHelp();
            return CommandResults.Failure;
        }

        var config = _userData.Load();
        var key = args[1];
        var value = args[2];

        switch (key.ToLowerInvariant())
        {
            case "runtime:model":
                config.Runtime.ActiveModelPath = Path.GetFullPath(
                    Environment.ExpandEnvironmentVariables(value));
                break;

            case "runtime:gpulayers":
            case "runtime:ngl":
                if (!TryParseInt(value, out var gpuLayers) || gpuLayers < 0)
                {
                    Console.WriteLine("GPU layers must be a non-negative integer.");
                    return CommandResults.Failure;
                }

                config.Runtime.GpuLayers = gpuLayers;
                break;

            case "runtime:gpubackend":
                config.Runtime.GpuBackend = GpuDetectionService.ParseBackend(value)
                    .ToString()
                    .ToLowerInvariant();
                break;

            case "runtime:gpudevice":
                if (!TryParseInt(value, out var gpuDevice) || gpuDevice < 0)
                {
                    Console.WriteLine("GPU device must be an index number. Prefer: llm gpu use <index>");
                    return CommandResults.Failure;
                }

                config.Runtime.GpuDeviceIndex = gpuDevice;
                break;

            case "runtime:context":
            case "runtime:contextsize":
                if (!TryParseInt(value, out var contextSize) || contextSize < 512)
                {
                    Console.WriteLine("Context size must be an integer >= 512.");
                    return CommandResults.Failure;
                }

                config.Runtime.ContextSize = contextSize;
                break;

            case "runtime:threads":
                if (!TryParseInt(value, out var threads) || threads < 1)
                {
                    Console.WriteLine("Threads must be a positive integer.");
                    return CommandResults.Failure;
                }

                config.Runtime.Threads = Math.Clamp(threads, 1, Environment.ProcessorCount * 2);
                break;

            case "runtime:host":
                config.Runtime.Host = value;
                break;

            case "runtime:provider":
                config.Runtime.Provider = value;
                break;

            case "runtime:port":
                if (!TryParseInt(value, out var port) || port is < 1 or > 65535)
                {
                    Console.WriteLine("Port must be 1..65535.");
                    return CommandResults.Failure;
                }

                config.Runtime.Port = port;
                break;

            case "runtime:apikey":
                config.Runtime.ApiKey = value;
                break;

            case "runtime:modelsdirectory":
                config.Runtime.ModelsDirectory = value;
                break;

            case "rootdirectory":
                if (string.IsNullOrWhiteSpace(value))
                {
                    Console.WriteLine("Root directory cannot be empty.");
                    return CommandResults.Failure;
                }

                config.RootDirectory = Path.GetFullPath(
                    Environment.ExpandEnvironmentVariables(value));
                break;

            default:
                Console.WriteLine($"Unknown configuration key: {key}");
                PrintHelp();
                return CommandResults.Failure;
        }

        _userData.Save(config);

        Console.WriteLine();
        Console.WriteLine($"[ OK ] Saved {key} = {value}");
        Console.WriteLine("Restart server to apply: llm serve --restart");
        return CommandResults.Success;
    }

    private static bool TryParseInt(string value, out int result) =>
        int.TryParse(value, out result);

    private static void PrintHelp()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  llm config show");
        Console.WriteLine("  llm config set <key> <value>");
        Console.WriteLine();
        Console.WriteLine("Keys:");
        Console.WriteLine("  Runtime:Context       Context length (e.g. 8192, 16384)");
        Console.WriteLine("  Runtime:GpuLayers     GPU offload layers / -ngl (e.g. 29)");
        Console.WriteLine("  Runtime:Threads       CPU threads (e.g. 8)");
        Console.WriteLine("  Runtime:Port          API port (e.g. 11434)");
        Console.WriteLine("  Runtime:Host          Bind host (default 127.0.0.1)");
        Console.WriteLine("  Runtime:GpuBackend    auto|cpu|vulkan|cuda|rocm");
        Console.WriteLine("  Runtime:GpuDevice     GPU index from llm gpu list");
        Console.WriteLine("  Runtime:Model         Active .gguf path");
        Console.WriteLine("  Runtime:ApiKey        API key for OpenAI-compatible clients");
        Console.WriteLine("  RootDirectory         Workspace root");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  llm config set Runtime:Context 8192");
        Console.WriteLine("  llm config set Runtime:GpuLayers 29");
        Console.WriteLine("  llm config set Runtime:Threads 8");
        Console.WriteLine("  llm config set Runtime:Port 11434");
    }
}
