namespace LLM.Core.Commands;

public sealed class CommandContext
{
    public CommandContext(
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default)
    {
        Arguments = arguments;
        CancellationToken = cancellationToken;
    }

    public IReadOnlyList<string> Arguments { get; }

    public CancellationToken CancellationToken { get; }
}