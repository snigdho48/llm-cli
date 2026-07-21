namespace LLM.CLI.Commands;

public sealed class HelpCommand : ICommand
{
    public string Name => "help";

    public Task<int> ExecuteAsync(string[] args)
    {
        Console.WriteLine("LLM CLI — Local AI coding workstation");
        Console.WriteLine();
        Console.WriteLine("Getting started:");
        Console.WriteLine("  setup                           First-time guided setup");
        Console.WriteLine("  serve [--restart] [--daemon] [--no-install]");
        Console.WriteLine("                                 Start server (auto-downloads runtime)");
        Console.WriteLine("  init [path] [--gpu N|--cpu|--auto] [--backend X]");
        Console.WriteLine("      [--context N] [--ngl N] [--threads N] [--port N]");
        Console.WriteLine("                                 Init workspace + GPU/CPU + runtime params");
        Console.WriteLine("  config show|set                 View / change context, layers, port, ...");
        Console.WriteLine();
        Console.WriteLine("Runtime:");
        Console.WriteLine("  runtime import <path>           Import local llama.cpp build");
        Console.WriteLine("  runtime install                 Download llama.cpp from GitHub");
        Console.WriteLine("  runtime start|stop|restart      Manage llama-server");
        Console.WriteLine("  runtime status                  Show runtime status");
        Console.WriteLine("  logs [--lines N]                Tail llama-server logs");
        Console.WriteLine();
        Console.WriteLine("Models:");
        Console.WriteLine("  model search [query] [--live]   Catalog or Hugging Face Hub");
        Console.WriteLine("  model files <org/repo>          List GGUF files on Hub");
        Console.WriteLine("  model pull <id|repo/file>       Download from Hugging Face");
        Console.WriteLine("  model list|scan|add|use|remove  Model registry");
        Console.WriteLine();
        Console.WriteLine("Profiles:");
        Console.WriteLine("  profile list|show|use           Hardware tuning profiles");
        Console.WriteLine("  gpu list|status|use|auto        Detect and select GPU (Intel/NVIDIA/AMD)");
        Console.WriteLine("  gpu doctor|fix                 Intel Vulkan + oneAPI checks / auto-install");
        Console.WriteLine();
        Console.WriteLine("Other:");
        Console.WriteLine("  serve [--restart] [--daemon] [--no-install]  Start OpenAI-compatible server");
        Console.WriteLine("  daemon status|install|uninstall Auto-start at Windows logon");
        Console.WriteLine("  bench [prompt]                  Measure inference speed");
        Console.WriteLine("  chat [prompt] [--no-start]      Test inference / interactive chat");
        Console.WriteLine("  doctor                          Environment and API diagnostics");
        Console.WriteLine("  config show|set                 User configuration");
        Console.WriteLine("  version                         Show CLI version");

        return Task.FromResult(CommandResults.Success);
    }
}
