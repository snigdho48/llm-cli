using LLM.CLI.Services;
using Xunit;

namespace LLM.Tests.Services;

public sealed class ProfileServiceTests : IDisposable
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
    public void EnsureDefaults_CreatesBuiltInProfiles()
    {
        var service = CreateProfileService();

        service.EnsureDefaults();
        var profiles = service.List();

        Assert.Equal(5, profiles.Count);
        Assert.Contains(profiles, profile => profile.Id == "iris-xe-coding");
        Assert.Contains(profiles, profile => profile.Id == "nvidia-cuda");
        Assert.Contains(profiles, profile => profile.Id == "amd-rocm");
        Assert.Contains(profiles, profile => profile.Id == "balanced");
        Assert.Contains(profiles, profile => profile.Id == "cpu-only");
    }

    [Fact]
    public void Apply_UpdatesRuntimeConfiguration()
    {
        var userData = CreateUserDataService();
        var service = new ProfileService(userData);

        service.EnsureDefaults();
        service.Apply("balanced");

        var config = userData.Load();

        Assert.Equal("balanced", config.ActiveProfileId);
        Assert.Equal(20, config.Runtime.GpuLayers);
        Assert.Equal(12288, config.Runtime.ContextSize);
    }

    private ProfileService CreateProfileService()
    {
        return new ProfileService(CreateUserDataService());
    }

    private UserDataService CreateUserDataService()
    {
        Directory.CreateDirectory(_userDataRoot);
        Directory.CreateDirectory(_workspaceRoot);

        var service = new UserDataService
        {
            RootDirectoryOverride = _userDataRoot,
        };

        var config = service.Load();
        config.RootDirectory = _workspaceRoot;
        service.Save(config);

        return service;
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
