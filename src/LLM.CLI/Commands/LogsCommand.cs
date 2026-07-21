using LLM.CLI.Services;

namespace LLM.CLI.Commands;

public sealed class LogsCommand : ICommand
{
    private readonly LogService _logs;

    public LogsCommand(LogService logs)
    {
        _logs = logs;
    }

    public string Name => "logs";

    public Task<int> ExecuteAsync(string[] args)
    {
        var lineCount = 50;

        for (var index = 0; index < args.Length; index++)
        {
            if (args[index].Equals("--lines", StringComparison.OrdinalIgnoreCase)
                && index + 1 < args.Length
                && int.TryParse(args[index + 1], out var parsed))
            {
                lineCount = Math.Clamp(parsed, 1, 500);
            }
        }

        var latestPath = _logs.GetLatestLogPath();

        Console.WriteLine("Runtime Logs");
        Console.WriteLine("------------");
        Console.WriteLine();

        if (latestPath is null)
        {
            Console.WriteLine("No llama-server logs found.");
            Console.WriteLine("Start the runtime first: llm runtime start");
            return Task.FromResult(CommandResults.Success);
        }

        Console.WriteLine($"File: {latestPath}");
        Console.WriteLine();

        foreach (var line in _logs.TailLatest(lineCount))
        {
            Console.WriteLine(line);
        }

        return Task.FromResult(CommandResults.Success);
    }
}
