namespace LLM.CLI.Commands;

public sealed class DoctorCommand : ICommand
{
    public string Name => "doctor";

    public Task ExecuteAsync(string[] args)
    {
        Console.WriteLine("LLM CLI Diagnostics");
        Console.WriteLine("-------------------");
        Console.WriteLine();

        Console.WriteLine($".NET Version : {Environment.Version}");
        Console.WriteLine($"OS           : {Environment.OSVersion}");
        Console.WriteLine($"64-bit OS    : {Environment.Is64BitOperatingSystem}");
        Console.WriteLine($"64-bit Proc  : {Environment.Is64BitProcess}");

        return Task.CompletedTask;
    }
}