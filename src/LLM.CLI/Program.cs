using LLM.CLI.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace LLM.CLI;

internal static class Program
{
    public static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("logs/llm.log",
                rollingInterval: RollingInterval.Day)
            .CreateLogger();

        try
        {
            var host = Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureServices(services =>
                {
			services.AddSingleton<CommandDispatcher>();

			services.AddSingleton<ICommand, VersionCommand>();
			services.AddSingleton<ICommand, HelpCommand>();
			services.AddSingleton<ICommand, DoctorCommand>();
                })
                .Build();

            var dispatcher = host.Services
                .GetRequiredService<CommandDispatcher>();

            await dispatcher.ExecuteAsync(args);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application crashed");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
