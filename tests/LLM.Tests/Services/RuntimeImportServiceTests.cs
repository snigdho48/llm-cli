using LLM.CLI.Configuration;
using LLM.CLI.Services;
using Xunit;

namespace LLM.Tests.Services;

public sealed class RuntimeImportServiceTests : IDisposable
{
    private readonly string _tempRoot = Path.Combine(
        Path.GetTempPath(),
        "LLM.Tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public void ResolveBuildDirectory_AcceptsReleaseFolderDirectly()
    {
        var releaseDirectory = CreateFakeReleaseDirectory();
        var service = new RuntimeImportService();

        var resolved = service.ResolveBuildDirectory(releaseDirectory);

        Assert.Equal(releaseDirectory, resolved);
    }

    [Fact]
    public void ResolveBuildDirectory_AcceptsLlamaCppRoot()
    {
        var releaseDirectory = CreateFakeReleaseDirectory();
        var llamaRoot = Directory.GetParent(releaseDirectory)!.Parent!.Parent!.FullName;
        var service = new RuntimeImportService();

        var resolved = service.ResolveBuildDirectory(llamaRoot);

        Assert.Equal(releaseDirectory, resolved);
    }

    [Fact]
    public void Import_CopiesRuntimeFilesIntoManagedWorkspace()
    {
        var releaseDirectory = CreateFakeReleaseDirectory();
        var service = new RuntimeImportService();
        var configuration = new UserConfiguration
        {
            RootDirectory = _tempRoot,
        };

        var copiedFiles = service.Import(configuration, releaseDirectory);
        var managedDirectory = service.GetManagedRuntimeDirectory(configuration);

        Assert.Equal(9, copiedFiles.Count);
        Assert.True(File.Exists(Path.Combine(managedDirectory, "llama-server.exe")));
        Assert.Equal(
            Path.Combine(managedDirectory, "llama-server.exe"),
            configuration.Runtime.ExecutablePath);
        Assert.Equal("imported", configuration.Runtime.Version);
    }

    private string CreateFakeReleaseDirectory()
    {
        var releaseDirectory = Path.Combine(_tempRoot, "build", "bin", "Release");
        Directory.CreateDirectory(releaseDirectory);

        foreach (var fileName in new[]
        {
            "llama-server.exe",
            "llama-server-impl.dll",
            "llama.dll",
            "llama-common.dll",
            "mtmd.dll",
            "ggml.dll",
            "ggml-base.dll",
            "ggml-cpu.dll",
            "ggml-vulkan.dll",
        })
        {
            File.WriteAllText(Path.Combine(releaseDirectory, fileName), "test");
        }

        return releaseDirectory;
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }
}
