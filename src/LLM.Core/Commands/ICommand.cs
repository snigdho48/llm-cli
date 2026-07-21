namespace LLM.Core.Commands;

public interface ICommand
{
    /// <summary>
    /// Command path.
    /// Examples:
    /// version
    /// help
    /// runtime status
    /// runtime start
    /// </summary>
    string Path { get; }

    /// <summary>
    /// Short description shown in help.
    /// </summary>
    string Description { get; }

    Task<CommandResult> ExecuteAsync(CommandContext context);
}