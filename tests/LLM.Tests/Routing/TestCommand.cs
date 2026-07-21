using LLM.Core.Commands;

namespace LLM.Tests.Routing;

public sealed class TestCommand : ICommand
{
    public string Path { get; }

    public string Description => "Test";

    public TestCommand(string path)
    {
        Path = path;
    }

    public Task<CommandResult> ExecuteAsync(CommandContext context)
    {
        return Task.FromResult(CommandResult.Ok());
    }
}