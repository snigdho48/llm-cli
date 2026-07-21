namespace LLM.CLI.Commands;

public sealed class RuntimeCommand : ICommand
{
    public string Name => "runtime";

    public Task ExecuteAsync(string[] args)
    {
        if (args.Length == 0)
        {
            PrintHelp();
            return Task.CompletedTask;
        }

        switch (args[0].ToLowerInvariant())
        {
            case "status":
                ShowStatus();
                break;

            default:
                Console.WriteLine($"Unknown runtime command: {args[0]}");
                break;
        }

        return Task.CompletedTask;
    }

    private static void ShowStatus()
    {
        Console.WriteLine("Runtime Status");
        Console.WriteLine("--------------");
        Console.WriteLine();
        Console.WriteLine("Provider : llama.cpp");
        Console.WriteLine("Status   : Stopped");
        Console.WriteLine("Model    : None");
        Console.WriteLine("Port     : 8080");
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine();
        Console.WriteLine("llm runtime status");
    }
}