using LLM.CLI.Services;

namespace LLM.CLI.Commands;

public sealed class DaemonCommand : ICommand
{
    private readonly DaemonService _daemon;

    public DaemonCommand(DaemonService daemon)
    {
        _daemon = daemon;
    }

    public string Name => "daemon";

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
                case "status":
                    ShowStatus();
                    break;

                case "install":
                    _daemon.Install();
                    Console.WriteLine();
                    Console.WriteLine("[ OK ] Auto-start registered (Task Scheduler: LLM-CLI-Serve).");
                    Console.WriteLine("       llama-server will start at user logon via: llm serve --restart");
                    break;

                case "uninstall":
                    _daemon.Uninstall();
                    Console.WriteLine();
                    Console.WriteLine("[ OK ] Auto-start removed.");
                    break;

                default:
                    Console.WriteLine($"Unknown daemon command: {args[0]}");
                    PrintHelp();
                    break;
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or FileNotFoundException)
        {
            Console.WriteLine($"[FAIL] {ex.Message}");
            return Task.FromResult(CommandResults.Failure);
        }

        return Task.FromResult(CommandResults.Success);
    }

    private void ShowStatus()
    {
        var status = _daemon.GetStatus();

        Console.WriteLine("Daemon Status");
        Console.WriteLine("-------------");
        Console.WriteLine();
        Console.WriteLine($"Task registered : {status.TaskRegistered}");
        Console.WriteLine($"Runtime running : {status.RuntimeRunning}");
        Console.WriteLine($"Port            : {status.Port}");
        Console.WriteLine($"Server          : {(string.IsNullOrWhiteSpace(status.ExecutablePath) ? "-" : status.ExecutablePath)}");
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine();
        Console.WriteLine("  llm daemon status");
        Console.WriteLine("  llm daemon install     Register auto-start at logon (Task Scheduler)");
        Console.WriteLine("  llm daemon uninstall   Remove auto-start task");
        Console.WriteLine();
        Console.WriteLine("Alias: llm serve --daemon");
    }
}
