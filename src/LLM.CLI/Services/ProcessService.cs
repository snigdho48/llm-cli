using System.Diagnostics;

namespace LLM.CLI.Services;

public sealed class ProcessService
{
    public Process? Find(string processName)
    {
        return Process
            .GetProcessesByName(processName)
            .FirstOrDefault();
    }

    public bool IsRunning(string processName)
    {
        return Find(processName) != null;
    }

    public void Kill(string processName)
    {
        var process = Find(processName);

        process?.Kill(true);
    }
}