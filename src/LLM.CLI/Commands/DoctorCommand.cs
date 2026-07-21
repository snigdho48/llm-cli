using LLM.CLI.Configuration;
namespace LLM.CLI.Commands;

public sealed class DoctorCommand : ICommand
{
    private readonly ConfigurationService _configuration;
    public string Name => "doctor";
    public DoctorCommand(ConfigurationService configuration)
    {
         _configuration = configuration;
    }

    public Task ExecuteAsync(string[] args)
    {
        Console.WriteLine("LLM CLI Diagnostics");
        Console.WriteLine("-------------------");
        Console.WriteLine();

        Console.WriteLine($".NET Version : {Environment.Version}");
        Console.WriteLine($"OS           : {Environment.OSVersion}");
        Console.WriteLine($"64-bit OS    : {Environment.Is64BitOperatingSystem}");
        Console.WriteLine($"64-bit Proc  : {Environment.Is64BitProcess}");
	Console.WriteLine();
	Console.WriteLine("Configuration");
	Console.WriteLine("-------------");
	Console.WriteLine($"Provider      : {_configuration.Options.Runtime.Provider}");
	Console.WriteLine($"Port          : {_configuration.Options.Runtime.Port}");
	Console.WriteLine($"Models Folder : {_configuration.Options.Runtime.ModelsDirectory}");

        return Task.CompletedTask;
    }
}