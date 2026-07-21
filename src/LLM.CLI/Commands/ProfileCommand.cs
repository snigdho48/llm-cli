using LLM.CLI.Services;

namespace LLM.CLI.Commands;

public sealed class ProfileCommand : ICommand
{
    private readonly ProfileService _profiles;

    public ProfileCommand(ProfileService profiles)
    {
        _profiles = profiles;
    }

    public string Name => "profile";

    public Task<int> ExecuteAsync(string[] args)
    {
        if (args.Length == 0)
        {
            PrintHelp();
            return Task.FromResult(CommandResults.Success);
        }

        try
        {
            switch (args[0].ToLowerInvariant())
            {
                case "list":
                    ListProfiles();
                    break;

                case "show":
                    ShowProfile(args);
                    break;

                case "use":
                    UseProfile(args);
                    break;

                default:
                    Console.WriteLine($"Unknown profile command: {args[0]}");
                    PrintHelp();
                    break;
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            Console.WriteLine($"[FAIL] {ex.Message}");
            return Task.FromResult(CommandResults.Failure);
        }

        return Task.FromResult(CommandResults.Success);
    }

    private void ListProfiles()
    {
        var profiles = _profiles.List();

        Console.WriteLine("Runtime Profiles");
        Console.WriteLine("----------------");
        Console.WriteLine();

        if (profiles.Count == 0)
        {
            Console.WriteLine("No profiles found. Run: llm init");
            return;
        }

        Console.WriteLine($"{"ID",-18} {"GPU",-5} {"CTX",-7} {"THREADS",-8} NAME");
        Console.WriteLine(new string('-', 70));

        foreach (var profile in profiles)
        {
            Console.WriteLine(
                $"{profile.Id,-18} {profile.GpuLayers,-5} {profile.ContextSize,-7} {profile.Threads,-8} {profile.Name}");
        }
    }

    private void ShowProfile(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: llm profile show <id>");
            return;
        }

        var profile = _profiles.Get(args[1]);

        Console.WriteLine($"Profile : {profile.Name} ({profile.Id})");
        Console.WriteLine($"GPU     : {profile.GpuLayers} layers");
        Console.WriteLine($"Context : {profile.ContextSize}");
        Console.WriteLine($"Threads : {profile.Threads}");
        Console.WriteLine($"About   : {profile.Description}");
    }

    private void UseProfile(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: llm profile use <id>");
            return;
        }

        _profiles.Apply(args[1]);

        Console.WriteLine();
        Console.WriteLine($"[ OK ] Applied profile '{args[1]}'.");
        Console.WriteLine("Restart runtime to apply: llm runtime restart");
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine();
        Console.WriteLine("  llm profile list");
        Console.WriteLine("  llm profile show <id>");
        Console.WriteLine("  llm profile use <id>");
    }
}
