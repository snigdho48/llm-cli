using System.Diagnostics;

namespace LLM.CLI.Services;

public sealed class BenchService
{
    private readonly OpenAiService _openAi;
    private readonly ProfileService _profiles;

    public BenchService(OpenAiService openAi, ProfileService profiles)
    {
        _openAi = openAi;
        _profiles = profiles;
    }

    public async Task<BenchResult> RunAsync(
        string prompt = "Write a hello world function in Python.",
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = await _openAi.ChatAsync(prompt, ensureRuntime: true, cancellationToken);
        stopwatch.Stop();

        var estimatedTokens = EstimateTokens(response);
        var seconds = Math.Max(stopwatch.Elapsed.TotalSeconds, 0.001);
        var tokensPerSecond = estimatedTokens / seconds;

        var recommendation = tokensPerSecond >= 12
            ? "iris-xe-coding or balanced profile"
            : tokensPerSecond >= 6
                ? "balanced profile"
                : "cpu-only profile or smaller model (qwen2.5-coder-3b)";

        return new BenchResult
        {
            Prompt = prompt,
            Elapsed = stopwatch.Elapsed,
            ResponseChars = response.Length,
            EstimatedTokens = estimatedTokens,
            TokensPerSecond = tokensPerSecond,
            Recommendation = recommendation,
        };
    }

    public static int EstimateTokens(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        return Math.Max(1, text.Length / 4);
    }
}

public sealed class BenchResult
{
    public string Prompt { get; init; } = "";

    public TimeSpan Elapsed { get; init; }

    public int ResponseChars { get; init; }

    public int EstimatedTokens { get; init; }

    public double TokensPerSecond { get; init; }

    public string Recommendation { get; init; } = "";
}
