using LLM.CLI.Configuration;

namespace LLM.CLI.Commands;

public sealed class ConfigCommand : ICommand
{
    private readonly ConfigurationService _configuration;

    public ConfigCommand(ConfigurationService configuration)
    {
        _configuration = configuration;
    }

    public string Name => "config";

    public Task ExecuteAsync(string[] args)
    {
        if (args.Length == 0)
        {
            PrintHelp();
            return Task.CompletedTask;
        }

        switch (args[0].ToLowerInvariant())
        {
            case "show":
                ShowConfiguration();
                break;

            case "set":
                SetConfiguration(args);
                break;

            default:
                Console.WriteLine($"Unknown config command: {args[0]}");
                break;
        }

        return Task.CompletedTask;
    }

    private void ShowConfiguration()
    {
        Console.WriteLine("LLM CLI Configuration");
        Console.WriteLine("---------------------");
        Console.WriteLine();

        Console.WriteLine($"Runtime Provider : {_configuration.Options.Runtime.Provider}");
        Console.WriteLine($"Port             : {_configuration.Options.Runtime.Port}");
        Console.WriteLine($"Models Directory : {_configuration.Options.Runtime.ModelsDirectory}");
    }

    private static void SetConfiguration(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  llm config set <key> <value>");
            return;
        }

        Console.WriteLine("Configuration update");
        Console.WriteLine("--------------------");
        Console.WriteLine();

        Console.WriteLine($"Key   : {args[1]}");
        Console.WriteLine($"Value : {args[2]}");
        Console.WriteLine();
        Console.WriteLine("Saving configuration is not implemented yet.");
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine();
        Console.WriteLine("llm config show");
        Console.WriteLine("llm config set <key> <value>");
    }
}