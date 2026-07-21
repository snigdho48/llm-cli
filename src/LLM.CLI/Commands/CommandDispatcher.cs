namespace LLM.CLI.Commands;

public sealed class CommandDispatcher
{
    private readonly IEnumerable<ICommand> _commands;

    public CommandDispatcher(IEnumerable<ICommand> commands)
    {
        _commands = commands;
    }

    public async Task ExecuteAsync(string[] args)
    {
        var command = args.Length > 0 ? args[0] : "help";

        var handler = _commands
            .FirstOrDefault(x =>
                x.Name.Equals(command,
                    StringComparison.OrdinalIgnoreCase));

        if (handler == null)
        {
            Console.WriteLine($"Unknown command: {command}");
            return;
        }

        await handler.ExecuteAsync(args);
    }
}
