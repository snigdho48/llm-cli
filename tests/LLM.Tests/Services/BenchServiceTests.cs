using LLM.CLI.Services;
using Xunit;

namespace LLM.Tests.Services;

public sealed class BenchServiceTests
{
    [Fact]
    public void EstimateTokens_ApproximatesFromCharacterCount()
    {
        var tokens = BenchService.EstimateTokens("hello world test");
        Assert.Equal(4, tokens);
    }

    [Fact]
    public void EstimateTokens_ReturnsZeroForEmpty()
    {
        Assert.Equal(0, BenchService.EstimateTokens(""));
    }
}
