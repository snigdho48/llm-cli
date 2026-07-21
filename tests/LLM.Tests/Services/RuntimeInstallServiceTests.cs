using LLM.CLI.Services;
using Xunit;

namespace LLM.Tests.Services;

public sealed class RuntimeInstallServiceTests
{
    [Fact]
    public void FindWindowsVulkanAsset_PrefersVulkanZip()
    {
        var assets = new List<GitHubAsset>
        {
            new() { Name = "llama-b100-bin-win-cpu-x64.zip", BrowserDownloadUrl = "https://example/cpu" },
            new() { Name = "llama-b100-bin-win-vulkan-x64.zip", BrowserDownloadUrl = "https://example/vulkan" },
        };

        var selected = RuntimeInstallService.FindWindowsVulkanAsset(assets);

        Assert.NotNull(selected);
        Assert.Contains("vulkan", selected!.Name, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FindBuildDirectory_LocatesReleaseFolder()
    {
        var root = Path.Combine(Path.GetTempPath(), "LLM.Tests", Guid.NewGuid().ToString("N"));
        var releaseDir = Path.Combine(root, "bin", "Release");
        Directory.CreateDirectory(releaseDir);
        File.WriteAllText(Path.Combine(releaseDir, "llama-server.exe"), "stub");

        try
        {
            var found = RuntimeInstallService.FindBuildDirectory(root);
            Assert.EndsWith("Release", found, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
