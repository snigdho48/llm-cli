using System.Diagnostics;
using LLM.CLI.Configuration;
using LLM.CLI.Runtime;

namespace LLM.CLI.Services;

public sealed class RuntimeService
{
    private readonly ProcessService _processService;
    private readonly UserDataService _userData;
    private readonly RuntimeImportService _importService;
    private readonly RuntimeInstallService _installService;
    private readonly GpuDetectionService _gpus;

    public RuntimeService(
        ProcessService processService,
        UserDataService userData,
        RuntimeImportService importService,
        RuntimeInstallService installService,
        GpuDetectionService gpus)
    {
        _processService = processService;
        _userData = userData;
        _importService = importService;
        _installService = installService;
        _gpus = gpus;
    }

    public RuntimeStatus GetStatus()
    {
        var config = _userData.Load();
        var process = _processService.Find("llama-server");

        return new RuntimeStatus
        {
            IsRunning = process != null,
            ProcessId = process?.Id,
            Provider = config.Runtime.Provider,
            Port = config.Runtime.Port,
            Host = config.Runtime.Host,
            IsInstalled = IsInstalled(config),
            ExecutablePath = config.Runtime.ExecutablePath,
            ActiveModelPath = config.Runtime.ActiveModelPath,
            GpuLayers = config.Runtime.GpuLayers,
            ContextSize = config.Runtime.ContextSize,
        };
    }

    public IReadOnlyList<string> Import(string sourcePath)
    {
        var config = _userData.Load();
        var copiedFiles = _importService.Import(config, sourcePath);
        _userData.Save(config);
        return copiedFiles;
    }

    public void Start()
    {
        var config = _userData.Load();

        if (_processService.IsRunning("llama-server"))
        {
            throw new InvalidOperationException("llama-server is already running.");
        }

        if (!IsInstalled(config))
        {
            throw new InvalidOperationException(
                "Runtime is not installed. Run: llm runtime install  OR  llm runtime import <path>");
        }

        if (string.IsNullOrWhiteSpace(config.Runtime.ActiveModelPath))
        {
            throw new InvalidOperationException(
                "No model configured. Run: llm model add <path> && llm model use <id>");
        }

        if (!File.Exists(config.Runtime.ActiveModelPath))
        {
            throw new FileNotFoundException(
                $"Model file not found: {config.Runtime.ActiveModelPath}");
        }

        if (IsPortInUse(config.Runtime.Host, config.Runtime.Port))
        {
            throw new InvalidOperationException(
                $"Port {config.Runtime.Port} is already in use. " +
                $"Run: llm config set Runtime:Port 11434");
        }

        var executable = config.Runtime.ExecutablePath;
        var workingDirectory = Path.GetDirectoryName(executable)
                               ?? throw new InvalidOperationException("Invalid executable path.");

        var arguments = string.Join(
            " ",
            [
                "-m", Quote(config.Runtime.ActiveModelPath),
                "-ngl", config.Runtime.GpuLayers.ToString(),
                "-c", config.Runtime.ContextSize.ToString(),
                "-t", config.Runtime.Threads.ToString(),
                "--host", Quote(config.Runtime.Host),
                "--port", config.Runtime.Port.ToString(),
                "--api-key", Quote(config.Runtime.ApiKey),
                "--metrics",
            ]);

        var logsDirectory = Path.Combine(config.RootDirectory, "logs");
        Directory.CreateDirectory(logsDirectory);
        var logPath = Path.Combine(logsDirectory, $"llama-server-{DateTime.UtcNow:yyyyMMdd-HHmmss}.log");

        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        GpuDetectionService.ApplyGpuEnvironment(
            startInfo,
            config.Runtime,
            _gpus.DetectGpus());

        var process = Process.Start(startInfo);
        if (process is null)
        {
            throw new InvalidOperationException("Failed to start llama-server.");
        }

        AttachLogWriter(process, logPath);
    }

    public async Task EnsureInstalledAsync(
        bool downloadIfMissing = true,
        CancellationToken cancellationToken = default)
    {
        var config = _userData.Load();
        if (IsInstalled(config))
        {
            return;
        }

        if (!downloadIfMissing)
        {
            throw new InvalidOperationException(
                "Runtime is not installed. Run: llm runtime install  OR  llm runtime import <path>");
        }

        Console.WriteLine("Runtime not found — downloading llama.cpp (Windows Vulkan) from GitHub...");
        var copied = await _installService.InstallLatestAsync(cancellationToken);
        Console.WriteLine($"[ OK ] Runtime installed ({copied.Count} files).");
    }

    public async Task EnsureRunningAsync(
        bool downloadIfMissing = true,
        CancellationToken cancellationToken = default)
    {
        await EnsureInstalledAsync(downloadIfMissing, cancellationToken);

        if (!_processService.IsRunning("llama-server"))
        {
            Start();
        }

        await WaitForHealthyAsync(cancellationToken);
    }

    public void Stop()
    {
        if (!_processService.IsRunning("llama-server"))
        {
            throw new InvalidOperationException("llama-server is not running.");
        }

        _processService.Kill("llama-server");
    }

    public void Restart()
    {
        if (_processService.IsRunning("llama-server"))
        {
            Stop();
            Thread.Sleep(TimeSpan.FromSeconds(2));
        }

        Start();
    }

    private async Task WaitForHealthyAsync(CancellationToken cancellationToken)
    {
        var config = _userData.Load();
        var url = $"http://{config.Runtime.Host}:{config.Runtime.Port}/health";

        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };

        for (var attempt = 0; attempt < 90; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using var response = await client.GetAsync(url, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(cancellationToken);
                    if (!body.TrimStart().StartsWith('<'))
                    {
                        return;
                    }
                }
            }
            catch (HttpRequestException)
            {
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
            }

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }

        throw new InvalidOperationException(
            "llama-server started but did not become healthy within 90 seconds. Check: llm logs");
    }

    private static void AttachLogWriter(Process process, string logPath)
    {
        var writer = new StreamWriter(logPath, append: true) { AutoFlush = true };
        writer.WriteLine($"[{DateTime.UtcNow:O}] llama-server started (PID {process.Id})");

        process.OutputDataReceived += (_, args) =>
        {
            if (args.Data is not null)
            {
                writer.WriteLine(args.Data);
            }
        };

        process.ErrorDataReceived += (_, args) =>
        {
            if (args.Data is not null)
            {
                writer.WriteLine(args.Data);
            }
        };

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        process.Exited += (_, _) =>
        {
            writer.WriteLine($"[{DateTime.UtcNow:O}] llama-server exited");
            writer.Dispose();
        };

        process.EnableRaisingEvents = true;
    }

    private static bool IsInstalled(UserConfiguration config)
    {
        return !string.IsNullOrWhiteSpace(config.Runtime.ExecutablePath)
               && File.Exists(config.Runtime.ExecutablePath);
    }

    private static bool IsPortInUse(string host, int port)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromMilliseconds(400) };
            using var response = client.GetAsync($"http://{host}:{port}/").GetAwaiter().GetResult();
            return true;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (TaskCanceledException)
        {
            // Timed out — something is listening but not answering quickly.
            return true;
        }
    }

    private static string Quote(string value)
    {
        return value.Contains(' ') ? $"\"{value}\"" : value;
    }
}
