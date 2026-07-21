using LLM.CLI.Services;

namespace LLM.CLI.Commands;

public sealed class BenchCommand : ICommand
{
    private readonly BenchService _bench;

    public BenchCommand(BenchService bench)
    {
        _bench = bench;
    }

    public string Name => "bench";

    public async Task<int> ExecuteAsync(string[] args)
    {
        var prompt = args.Length > 0
            ? string.Join(' ', args)
            : "Write a hello world function in Python.";

        try
        {
            Console.WriteLine("Running benchmark (requires running or startable server)...");
            Console.WriteLine();

            var result = await _bench.RunAsync(prompt);

            Console.WriteLine("Benchmark Results");
            Console.WriteLine("-----------------");
            Console.WriteLine($"Elapsed        : {result.Elapsed.TotalSeconds:0.00}s");
            Console.WriteLine($"Response chars : {result.ResponseChars}");
            Console.WriteLine($"Est. tokens    : {result.EstimatedTokens}");
            Console.WriteLine($"Tokens/sec     : {result.TokensPerSecond:0.0}");
            Console.WriteLine();
            Console.WriteLine($"Recommendation : {result.Recommendation}");
            Console.WriteLine();
            Console.WriteLine("Compare profiles:");
            Console.WriteLine("  llm profile use iris-xe-coding && llm runtime restart && llm bench");
            Console.WriteLine("  llm profile use cpu-only && llm runtime restart && llm bench");

            return CommandResults.Success;
        }
        catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException or TaskCanceledException)
        {
            Console.WriteLine($"[FAIL] {ex.Message}");
            return CommandResults.Failure;
        }
    }
}
