using LLM.CLI.Services;

namespace LLM.CLI.Commands;

public sealed class CursorCommand : ICommand
{
    private readonly CursorSettingsService _cursor;

    public CursorCommand(CursorSettingsService cursor)
    {
        _cursor = cursor;
    }

    public string Name => "cursor";

    public Task<int> ExecuteAsync(string[] args)
    {
        var info = _cursor.GetConnectionInfo();
        var asJson = args.Contains("--json", StringComparer.OrdinalIgnoreCase);
        var write = args.Contains("--write", StringComparer.OrdinalIgnoreCase);

        if (write)
        {
            var result = _cursor.TryWriteCursorSettings();
            Console.WriteLine();
            Console.WriteLine(result.Written ? "[ OK ]" : "[WARN]");
            Console.WriteLine(result.Message);
            Console.WriteLine($"Path: {result.Path}");
            return Task.FromResult(CommandResults.Success);
        }

        if (asJson)
        {
            Console.WriteLine("{");
            Console.WriteLine($"  \"openaiBaseUrl\": \"{info.BaseUrl}\",");
            Console.WriteLine($"  \"openaiApiKey\": \"{info.ApiKey}\",");
            Console.WriteLine($"  \"model\": \"{info.Model}\"");
            Console.WriteLine("}");
            return Task.FromResult(CommandResults.Success);
        }

        Console.WriteLine("Cursor — OpenAI Compatible Setup");
        Console.WriteLine("================================");
        Console.WriteLine();
        Console.WriteLine("1. Open Cursor Settings → Models");
        Console.WriteLine("2. Enable 'OpenAI API Key' override");
        Console.WriteLine("3. Set these values:");
        Console.WriteLine();
        Console.WriteLine($"   Base URL : {info.BaseUrl}");
        Console.WriteLine($"   API Key  : {info.ApiKey}");
        Console.WriteLine($"   Model    : {info.Model}");
        Console.WriteLine();
        Console.WriteLine("Auto-config:");
        Console.WriteLine("  llm cursor --write");
        Console.WriteLine("  llm cursor --json");
        Console.WriteLine();
        Console.WriteLine("Verify:");
        Console.WriteLine("  llm doctor");
        Console.WriteLine("  llm chat \"Say hello in one sentence\"");

        return Task.FromResult(CommandResults.Success);
    }
}
