using LLM.CLI.Configuration;

namespace LLM.CLI.Services;

public sealed class RuntimeImportService
{
    private static readonly string[] RequiredRuntimeFiles =
    [
        "llama-server.exe",
        "llama-server-impl.dll",
        "llama.dll",
        "llama-common.dll",
        "mtmd.dll",
        "ggml.dll",
        "ggml-base.dll",
        "ggml-cpu.dll",
        "ggml-vulkan.dll",
    ];

    public string ResolveBuildDirectory(string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            throw new ArgumentException("A source path is required.", nameof(sourcePath));
        }

        var normalized = Path.GetFullPath(Environment.ExpandEnvironmentVariables(sourcePath.Trim()));

        if (File.Exists(Path.Combine(normalized, "llama-server.exe")))
        {
            return normalized;
        }

        var releaseFromRoot = Path.Combine(normalized, "build", "bin", "Release");
        if (File.Exists(Path.Combine(releaseFromRoot, "llama-server.exe")))
        {
            return releaseFromRoot;
        }

        throw new DirectoryNotFoundException(
            $"Could not find llama-server.exe under '{normalized}' or '{releaseFromRoot}'.");
    }

    public string GetManagedRuntimeDirectory(UserConfiguration configuration)
    {
        return Path.Combine(
            configuration.RootDirectory,
            "runtimes",
            "llama.cpp",
            "current");
    }

    public IReadOnlyList<string> Import(UserConfiguration configuration, string sourcePath)
    {
        var buildDirectory = ResolveBuildDirectory(sourcePath);
        var targetDirectory = GetManagedRuntimeDirectory(configuration);

        Directory.CreateDirectory(targetDirectory);

        var copiedFiles = new List<string>();
        var missingFiles = new List<string>();

        foreach (var fileName in RequiredRuntimeFiles)
        {
            var sourceFile = Path.Combine(buildDirectory, fileName);
            if (!File.Exists(sourceFile))
            {
                missingFiles.Add(fileName);
                continue;
            }

            var targetFile = Path.Combine(targetDirectory, fileName);
            File.Copy(sourceFile, targetFile, overwrite: true);
            copiedFiles.Add(targetFile);
        }

        if (missingFiles.Count > 0)
        {
            throw new FileNotFoundException(
                $"Missing runtime files in '{buildDirectory}': {string.Join(", ", missingFiles)}");
        }

        configuration.Runtime.Provider = "llama.cpp";
        configuration.Runtime.ExecutablePath = Path.Combine(targetDirectory, "llama-server.exe");
        configuration.Runtime.Version = "imported";
        configuration.Runtime.SourceBuildPath = buildDirectory;

        return copiedFiles;
    }
}
