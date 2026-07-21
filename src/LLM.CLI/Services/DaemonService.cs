using System.Diagnostics;

namespace LLM.CLI.Services;

/// <summary>
/// Optional Windows Task Scheduler keep-alive for llama-server (no admin required).
/// </summary>
public sealed class DaemonService
{
    public const string TaskName = "LLM-CLI-Serve";

    private readonly UserDataService _userData;
    private readonly RuntimeService _runtime;

    public DaemonService(UserDataService userData, RuntimeService runtime)
    {
        _userData = userData;
        _runtime = runtime;
    }

    public DaemonStatus GetStatus()
    {
        var config = _userData.Load();
        var runtime = _runtime.GetStatus();
        var taskRegistered = IsTaskRegistered();

        return new DaemonStatus
        {
            TaskRegistered = taskRegistered,
            RuntimeRunning = runtime.IsRunning,
            ExecutablePath = config.Runtime.ExecutablePath,
            Port = config.Runtime.Port,
        };
    }

    public void Install()
    {
        var config = _userData.Load();
        if (string.IsNullOrWhiteSpace(config.Runtime.ExecutablePath)
            || !File.Exists(config.Runtime.ExecutablePath))
        {
            throw new InvalidOperationException(
                "Runtime not installed. Run: llm runtime import <path>  OR  llm runtime install");
        }

        if (string.IsNullOrWhiteSpace(config.Runtime.ActiveModelPath)
            || !File.Exists(config.Runtime.ActiveModelPath))
        {
            throw new InvalidOperationException(
                "No active model. Run: llm model use <id>");
        }

        var llmShim = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LLM",
            "bin",
            "llm.cmd");

        if (!File.Exists(llmShim))
        {
            throw new FileNotFoundException(
                "Global llm shim not found. Run: .\\scripts\\install.ps1",
                llmShim);
        }

        UninstallQuiet();

        // At logon, ensure server is running (restarts if already up).
        var arguments =
            $"/Create /TN \"{TaskName}\" /TR \"\\\"{llmShim}\\\" serve --restart\" " +
            "/SC ONLOGON /RL LIMITED /F";

        RunSchtasks(arguments);

        // Also start now.
        _runtime.EnsureRunningAsync().GetAwaiter().GetResult();
    }

    public void Uninstall()
    {
        if (!IsTaskRegistered())
        {
            throw new InvalidOperationException($"Scheduled task '{TaskName}' is not registered.");
        }

        UninstallQuiet();
    }

    public bool IsTaskRegistered()
    {
        var result = RunSchtasks($"/Query /TN \"{TaskName}\"", throwOnError: false);
        return result.ExitCode == 0;
    }

    private void UninstallQuiet()
    {
        RunSchtasks($"/Delete /TN \"{TaskName}\" /F", throwOnError: false);
    }

    private static (int ExitCode, string Output) RunSchtasks(string arguments, bool throwOnError = true)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "schtasks.exe",
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start schtasks.exe");

        var output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (throwOnError && process.ExitCode != 0)
        {
            throw new InvalidOperationException($"schtasks failed: {output.Trim()}");
        }

        return (process.ExitCode, output);
    }
}

public sealed class DaemonStatus
{
    public bool TaskRegistered { get; init; }

    public bool RuntimeRunning { get; init; }

    public string ExecutablePath { get; init; } = "";

    public int Port { get; init; }
}
