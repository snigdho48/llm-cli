using LLM.Core.Commands;

namespace LLM.Core.Routing;

public sealed class CommandNode
{
    public string Name { get; }

    public Dictionary<string, CommandNode> Children { get; }
        = new(StringComparer.OrdinalIgnoreCase);

    public ICommand? Command { get; set; }

    public CommandNode(string name)
    {
        Name = name;
    }
}