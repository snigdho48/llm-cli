namespace LLM.CLI.Routing;
public sealed class CommandContext
{
    public required string[] Arguments { get; init; }
    public required IServiceProvider Services { get; init; }
    public CancellationToken CancellationToken { get; init; }
}
