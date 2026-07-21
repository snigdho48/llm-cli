using LLM.CLI.Services;

namespace LLM.CLI.Commands;

public sealed class ChatCommand : ICommand
{
    private readonly OpenAiService _openAi;

    public ChatCommand(OpenAiService openAi)
    {
        _openAi = openAi;
    }

    public string Name => "chat";

    public async Task<int> ExecuteAsync(string[] args)
    {
        var ensureRuntime = !args.Contains("--no-start", StringComparer.OrdinalIgnoreCase);
        var promptParts = args.Where(arg => !arg.StartsWith('-')).ToArray();

        try
        {
            if (promptParts.Length > 0)
            {
                var prompt = string.Join(' ', promptParts);
                await WriteResponseAsync(prompt, ensureRuntime);
                return CommandResults.Success;
            }

            Console.WriteLine("Interactive chat (empty line to exit).");
            Console.WriteLine();

            while (true)
            {
                Console.Write("You> ");
                var line = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(line))
                {
                    break;
                }

                await WriteResponseAsync(line, ensureRuntime);
                Console.WriteLine();
            }

            return CommandResults.Success;
        }
        catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException or TaskCanceledException)
        {
            Console.WriteLine($"[FAIL] {ex.Message}");
            return CommandResults.Failure;
        }
    }

    private async Task WriteResponseAsync(string prompt, bool ensureRuntime)
    {
        Console.WriteLine();
        Console.Write("AI>  ");
        var response = await _openAi.ChatAsync(prompt, ensureRuntime);
        Console.WriteLine(response);
    }
}
