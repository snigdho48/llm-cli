namespace LLM.CLI.Commands;

public sealed class CommandDispatcher
{
    private readonly Dictionary<string, ICommand> _commands;

    public CommandDispatcher(IEnumerable<ICommand> commands)
    {
        _commands = commands.ToDictionary(
            c => c.Name,
            StringComparer.OrdinalIgnoreCase);
    }

    public async Task<int> ExecuteAsync(string[] args)
    {
        var commandName = args.Length > 0 ? args[0] : "help";

        if (!_commands.TryGetValue(commandName, out var command))
        {
            Console.WriteLine($"Unknown command: {commandName}");
            return CommandResults.Failure;
        }

        return await command.ExecuteAsync(args.Skip(1).ToArray());
    }
}
