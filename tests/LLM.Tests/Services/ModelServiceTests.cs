using LLM.CLI.Services;
using Xunit;

namespace LLM.Tests.Services;

public sealed class ModelServiceTests : IDisposable
{
    private readonly string _userDataRoot = Path.Combine(
        Path.GetTempPath(),
        "LLM.Tests",
        Guid.NewGuid().ToString("N"));

    private readonly string _workspaceRoot = Path.Combine(
        Path.GetTempPath(),
        "LLM.Tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public void Add_RegisterAndUseModel_UpdatesActiveModelPath()
    {
        var modelPath = CreateModelFile("qwen2.5-coder-7b-instruct-q4_k_m.gguf");
        var userData = CreateUserDataService();
        var service = new ModelService(userData);

        var entry = service.Add(modelPath, "qwen");
        service.Use(entry.Id);

        var config = userData.Load();

        Assert.Equal(modelPath, config.Runtime.ActiveModelPath);
        Assert.Equal("qwen", service.LoadRegistry().DefaultModelId);
    }

    [Fact]
    public void Scan_FindsGgufFilesRecursively()
    {
        var nestedDirectory = Path.Combine(_workspaceRoot, "nested");
        Directory.CreateDirectory(nestedDirectory);
        CreateModelFile("alpha.gguf", _workspaceRoot);
        CreateModelFile("beta.gguf", nestedDirectory);

        var userData = CreateUserDataService();
        var service = new ModelService(userData);

        var discovered = service.Scan(_workspaceRoot);

        Assert.Equal(2, discovered.Count);
    }

    private UserDataService CreateUserDataService()
    {
        Directory.CreateDirectory(_userDataRoot);

        var service = new UserDataService
        {
            RootDirectoryOverride = _userDataRoot
        };

        var config = service.Load();
        config.RootDirectory = _workspaceRoot;
        Directory.CreateDirectory(_workspaceRoot);
        service.Save(config);

        return service;
    }

    private string CreateModelFile(string fileName, string? directory = null)
    {
        directory ??= _workspaceRoot;
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, fileName);
        File.WriteAllText(path, "gguf-test-data");
        return path;
    }

    public void Dispose()
    {
        if (Directory.Exists(_userDataRoot))
        {
            Directory.Delete(_userDataRoot, recursive: true);
        }

        if (Directory.Exists(_workspaceRoot))
        {
            Directory.Delete(_workspaceRoot, recursive: true);
        }
    }
}
