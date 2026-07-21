using LLM.CLI.Services;
using LLM.CLI.Commands;
using LLM.CLI.Configuration;
using LLM.CLI.Configuration.Options;

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

            .WriteTo.File("logs/llm.log", rollingInterval: RollingInterval.Day)

            .CreateLogger();



        try

        {

            var host = Host.CreateDefaultBuilder(args)

                .UseSerilog()

                .ConfigureServices((context, services) =>

                {

                    services.Configure<LLMOptions>(

                        context.Configuration.GetSection("LLM"));



                    services.AddSingleton<ConfigurationService>();

                    services.AddSingleton<UserDataService>();

                    services.AddSingleton<WorkspaceService>();

                    services.AddSingleton<ProcessService>();

                    services.AddSingleton<RuntimeImportService>();
                    services.AddSingleton<RuntimeInstallService>();
                    services.AddSingleton<RuntimeService>();

                    services.AddSingleton<ModelService>();
                    services.AddSingleton<ModelDownloadService>();
                    services.AddSingleton<ModelCatalogService>();
                    services.AddSingleton<HuggingFaceSearchService>();

                    services.AddSingleton<HealthCheckService>();

                    services.AddSingleton<HardwareService>();
                    services.AddSingleton<GpuDetectionService>();
                    services.AddSingleton<WingetService>();
                    services.AddSingleton<IntelPrerequisiteService>();

                    services.AddSingleton<ProfileService>();

                    services.AddSingleton<LogService>();

                    services.AddSingleton<OpenAiService>();
                    services.AddSingleton<CursorSettingsService>();
                    services.AddSingleton<BenchService>();
                    services.AddSingleton<DaemonService>();

                    services.AddSingleton<CommandDispatcher>();



                    services.AddSingleton<ICommand, VersionCommand>();

                    services.AddSingleton<ICommand, HelpCommand>();

                    services.AddSingleton<ICommand, SetupCommand>();

                    services.AddSingleton<ICommand, InitCommand>();

                    services.AddSingleton<ICommand, DoctorCommand>();

                    services.AddSingleton<ICommand, ConfigCommand>();

                    services.AddSingleton<ICommand, RuntimeCommand>();

                    services.AddSingleton<ICommand, ModelCommand>();

                    services.AddSingleton<ICommand, ProfileCommand>();

                    services.AddSingleton<ICommand, ChatCommand>();

                    services.AddSingleton<ICommand, CursorCommand>();

                    services.AddSingleton<ICommand, LogsCommand>();
                    services.AddSingleton<ICommand, ServeCommand>();
                    services.AddSingleton<ICommand, BenchCommand>();
                    services.AddSingleton<ICommand, DaemonCommand>();
                    services.AddSingleton<ICommand, GpuCommand>();

                })

                .Build();



            var dispatcher = host.Services.GetRequiredService<CommandDispatcher>();

            var exitCode = await dispatcher.ExecuteAsync(args);
            Environment.ExitCode = exitCode;
            return;

        }

        catch (Exception ex)
        {
            Log.Fatal(ex, "Application crashed");
            Environment.ExitCode = CommandResults.Failure;
        }

        finally

        {

            Log.CloseAndFlush();

        }

    }

}


