namespace LLM.CLI.Services;

public sealed class LogService
{
    private readonly UserDataService _userData;

    public LogService(UserDataService userData)
    {
        _userData = userData;
    }

    public string GetLogsDirectory()
    {
        var config = _userData.Load();
        return Path.Combine(config.RootDirectory, "logs");
    }

    public string? GetLatestLogPath()
    {
        var logsDirectory = GetLogsDirectory();
        if (!Directory.Exists(logsDirectory))
        {
            return null;
        }

        return Directory
            .EnumerateFiles(logsDirectory, "llama-server-*.log")
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
    }

    public IReadOnlyList<string> TailLatest(int lineCount = 50)
    {
        var path = GetLatestLogPath();
        if (path is null || !File.Exists(path))
        {
            return [];
        }

        var queue = new Queue<string>(lineCount);

        foreach (var line in File.ReadLines(path))
        {
            if (queue.Count == lineCount)
            {
                queue.Dequeue();
            }

            queue.Enqueue(line);
        }

        return queue.ToList();
    }
}
