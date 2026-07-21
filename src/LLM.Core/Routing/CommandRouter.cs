using LLM.Core.Commands;

namespace LLM.Core.Routing;

public sealed class CommandRouter
{
    private readonly CommandNode _root = new("");

    public void Register(ICommand command)
    {
        if (command is null)
            throw new ArgumentNullException(nameof(command));

        var parts = command.Path
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var current = _root;

        foreach (var part in parts)
        {
            if (!current.Children.TryGetValue(part, out var child))
            {
                child = new CommandNode(part);
                current.Children.Add(part, child);
            }

            current = child;
        }

        if (current.Command != null)
        {
            throw new InvalidOperationException(
                $"Command '{command.Path}' already registered.");
        }

        current.Command = command;
    }

public (ICommand? Command, IReadOnlyList<string> RemainingArgs) Resolve(IReadOnlyList<string> args)
{
    var current = _root;
    var index = 0;

    while (index < args.Count)
    {
        if (!current.Children.TryGetValue(args[index], out var child))
            break;

        current = child;
        index++;
    }

    return (
        current.Command,
        args.Skip(index).ToList()
    );
}

    public IEnumerable<ICommand> GetAllCommands()
    {
        return Traverse(_root);

        static IEnumerable<ICommand> Traverse(CommandNode node)
        {
            if (node.Command != null)
                yield return node.Command;

            foreach (var child in node.Children.Values)
            {
                foreach (var command in Traverse(child))
                    yield return command;
            }
        }
    }
}