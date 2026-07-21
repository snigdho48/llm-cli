using LLM.CLI.Services;
using Xunit;

namespace LLM.Tests.Services;

public sealed class WorkspaceServiceTests : IDisposable
{
    private readonly string _rootDirectory = Path.Combine(
        Path.GetTempPath(),
        "LLM.Tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public void Initialize_CreatesTheManagedWorkspaceDirectories()
    {
        var service = new WorkspaceService();

        var createdDirectories = service.Initialize(_rootDirectory);

        Assert.Equal(8, createdDirectories.Count);
        Assert.All(createdDirectories, path => Assert.True(Directory.Exists(path)));
        Assert.True(Directory.Exists(Path.Combine(_rootDirectory, "runtimes")));
        Assert.True(Directory.Exists(Path.Combine(_rootDirectory, "models")));
        Assert.True(Directory.Exists(Path.Combine(_rootDirectory, "profiles")));
    }

    [Fact]
    public void Initialize_RejectsAnEmptyWorkspacePath()
    {
        var service = new WorkspaceService();

        Assert.Throws<ArgumentException>(() => service.Initialize("   "));
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootDirectory))
        {
            Directory.Delete(_rootDirectory, recursive: true);
        }
    }
}
