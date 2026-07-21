using System.Reflection;
using LLM.CLI.Services;

namespace LLM.CLI.Commands;

public sealed class VersionCommand : ICommand
{
    public string Name => "version";

    public Task<int> ExecuteAsync(string[] args)
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        var versionText = version is null ? "1.0.0" : $"{version.Major}.{version.Minor}.{version.Build}";

        Console.WriteLine("LLM CLI");
        Console.WriteLine($"Version: {versionText}");
        Console.WriteLine($"Runtime: .NET {Environment.Version}");

        return Task.FromResult(CommandResults.Success);
    }
}
