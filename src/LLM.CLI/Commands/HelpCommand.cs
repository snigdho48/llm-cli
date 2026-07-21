namespace LLM.CLI.Commands;

public sealed class HelpCommand : ICommand
{
    public string Name => "help";

    public Task ExecuteAsync(string[] args)
    {
        Console.WriteLine("Available commands:");
        Console.WriteLine();
        Console.WriteLine("version");
        Console.WriteLine("help");
        Console.WriteLine("doctor");
        Console.WriteLine("runtime");
        Console.WriteLine("model");
        Console.WriteLine("profile");

        return Task.CompletedTask;
    }
}
