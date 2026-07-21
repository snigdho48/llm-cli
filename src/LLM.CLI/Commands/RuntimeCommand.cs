using LLM.CLI.Services;



namespace LLM.CLI.Commands;



public sealed class RuntimeCommand : ICommand

{

    private readonly RuntimeService _runtimeService;

    private readonly RuntimeInstallService _installService;



    public RuntimeCommand(

        RuntimeService runtimeService,

        RuntimeInstallService installService)

    {

        _runtimeService = runtimeService;

        _installService = installService;

    }



    public string Name => "runtime";



    public async Task<int> ExecuteAsync(string[] args)

    {

        if (args.Length == 0)

        {

            PrintHelp();

            return CommandResults.Success;

        }



        try

        {

            switch (args[0].ToLowerInvariant())

            {

                case "status":

                    ShowStatus();

                    break;



                case "import":

                    ImportRuntime(args);

                    break;



                case "install":

                    await InstallRuntimeAsync();

                    break;



                case "start":

                    _runtimeService.Start();

                    Console.WriteLine("llama-server started.");

                    break;



                case "stop":

                    _runtimeService.Stop();

                    Console.WriteLine("llama-server stopped.");

                    break;



                case "restart":

                    _runtimeService.Restart();

                    Console.WriteLine("llama-server restarted.");

                    break;



                default:

                    Console.WriteLine($"Unknown runtime command: {args[0]}");

                    PrintHelp();

                    break;

            }

        }

        catch (Exception ex) when (ex is InvalidOperationException or FileNotFoundException or DirectoryNotFoundException or HttpRequestException)

        {

            Console.WriteLine($"[FAIL] {ex.Message}");

            return CommandResults.Failure;

        }



        return CommandResults.Success;

    }



    private async Task InstallRuntimeAsync()

    {

        Console.WriteLine("Downloading latest llama.cpp Windows release from GitHub...");

        Console.WriteLine("(This may take several minutes.)");

        Console.WriteLine();



        var copied = await _installService.InstallLatestAsync();



        Console.WriteLine();

        Console.WriteLine("[ OK ] Runtime installed successfully.");

        Console.WriteLine($"Copied {copied.Count} files into managed workspace.");

        Console.WriteLine();

        Console.WriteLine("Next steps:");

        Console.WriteLine("  llm model pull qwen2.5-coder-7b --use");

        Console.WriteLine("  llm serve");

    }



    private void ShowStatus()

    {

        var status = _runtimeService.GetStatus();



        Console.WriteLine("Runtime Status");

        Console.WriteLine("--------------");

        Console.WriteLine();

        Console.WriteLine($"Installed : {status.IsInstalled}");

        Console.WriteLine($"Running   : {status.IsRunning}");

        Console.WriteLine($"Provider  : {status.Provider}");

        Console.WriteLine($"Host      : {status.Host}");

        Console.WriteLine($"Port      : {status.Port}");

        Console.WriteLine($"GPU Layers: {status.GpuLayers}");

        Console.WriteLine($"Context   : {status.ContextSize}");

        Console.WriteLine($"PID       : {(status.ProcessId?.ToString() ?? "-")}");

        Console.WriteLine($"Server    : {(string.IsNullOrWhiteSpace(status.ExecutablePath) ? "-" : status.ExecutablePath)}");

        Console.WriteLine($"Model     : {(string.IsNullOrWhiteSpace(status.ActiveModelPath) ? "-" : status.ActiveModelPath)}");

    }



    private void ImportRuntime(string[] args)

    {

        if (args.Length < 2)

        {

            Console.WriteLine("Usage:");

            Console.WriteLine("  llm runtime import <path-to-llama.cpp-or-Release-folder>");

            return;

        }



        var sourcePath = string.Join(" ", args.Skip(1));

        var copiedFiles = _runtimeService.Import(sourcePath);



        Console.WriteLine();

        Console.WriteLine("[ OK ] Runtime imported successfully.");

        Console.WriteLine($"Copied {copiedFiles.Count} files.");

        Console.WriteLine();

        Console.WriteLine("Next steps:");

        Console.WriteLine("  llm model pull qwen2.5-coder-7b --use");

        Console.WriteLine("  llm serve");

    }



    private static void PrintHelp()

    {

        Console.WriteLine("Usage:");

        Console.WriteLine();

        Console.WriteLine("  llm runtime install             Download llama.cpp from GitHub");

        Console.WriteLine("  llm runtime import <path>       Import local llama.cpp build");

        Console.WriteLine("  llm runtime start");

        Console.WriteLine("  llm runtime stop");

        Console.WriteLine("  llm runtime restart");

        Console.WriteLine("  llm runtime status");

    }

}


