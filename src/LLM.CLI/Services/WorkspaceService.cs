namespace LLM.CLI.Services;

public sealed class WorkspaceService
{
    private static readonly string[] DirectoryNames =
    [
        "runtimes",
        "models",
        "downloads",
        "cache",
        "logs",
        "plugins",
        "profiles",
        "temp"
    ];

    public IReadOnlyList<string> Initialize(string rootDirectory)
    {
        if (string.IsNullOrWhiteSpace(rootDirectory))
        {
            throw new ArgumentException("A workspace directory is required.", nameof(rootDirectory));
        }

        var normalizedRoot = Path.GetFullPath(
            Environment.ExpandEnvironmentVariables(rootDirectory.Trim()));

        var createdDirectories = new List<string>();

        Directory.CreateDirectory(normalizedRoot);

        foreach (var directoryName in DirectoryNames)
        {
            var path = Path.Combine(normalizedRoot, directoryName);
            Directory.CreateDirectory(path);
            createdDirectories.Add(path);
        }

        return createdDirectories;
    }
}
