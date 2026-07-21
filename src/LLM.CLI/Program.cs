using LLM.CLI.Commands;
using LLM.CLI.Configuration;
using LLM.CLI.Configuration.Options;
using Microsoft.Extensions.Configuration;
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
          	.ConfigureServices((context, services) =>
                {    	services.Configure<LLMOptions>(
        			context.Configuration.GetSection("LLM"));

    			services.AddSingleton<ConfigurationService>();
			services.AddSingleton<CommandDispatcher>();

			services.AddSingleton<ICommand, VersionCommand>();
			services.AddSingleton<ICommand, HelpCommand>();
			services.AddSingleton<ICommand, RuntimeCommand>();
			services.AddSingleton<ICommand, DoctorCommand>();
			services.AddSingleton<ICommand, ConfigCommand>();
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
