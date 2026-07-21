using LLM.Core.Commands;
using LLM.Core.Routing;
using Xunit;

namespace LLM.Tests.Routing;

public class CommandRouterTests
{
    private sealed class TestCommand : ICommand
    {
        public string Path { get; }

        public string Description => "Test Command";

        public TestCommand(string path)
        {
            Path = path;
        }

        public Task<CommandResult> ExecuteAsync(CommandContext context)
        {
            return Task.FromResult(CommandResult.Ok());
        }
    }

    [Fact]
    public void Resolve_Should_Return_Remaining_Arguments()
    {
        var router = new CommandRouter();

        router.Register(new TestCommand("runtime status"));

        var result = router.Resolve(new[]
        {
            "runtime",
            "status",
            "--verbose"
        });

        Assert.NotNull(result.Command);
        Assert.Single(result.RemainingArgs);
        Assert.Equal("--verbose", result.RemainingArgs[0]);
    }
}