namespace LLM.CLI.Commands;

public sealed class VersionCommand : ICommand
{
    public string Name => "version";

    public Task ExecuteAsync(string[] args)
    {
        Console.WriteLine("LLM CLI");
        Console.WriteLine("Version: 0.1.0");

        return Task.CompletedTask;
    }
}
