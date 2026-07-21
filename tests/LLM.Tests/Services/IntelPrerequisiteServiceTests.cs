using LLM.CLI.Services;
using Xunit;

namespace LLM.Tests.Services;

public sealed class IntelPrerequisiteServiceTests
{
    [Fact]
    public void GetManualInstructions_WithoutIntel_SuggestsGpuList()
    {
        var report = new IntelPrerequisiteReport { HasIntelGpu = false };
        var lines = IntelPrerequisiteService.GetManualInstructions(report);

        Assert.Contains(lines, line => line.Contains("llm gpu list", StringComparison.Ordinal));
    }

    [Fact]
    public void GetManualInstructions_WhenVulkanMissing_IncludesWingetCommand()
    {
        var report = new IntelPrerequisiteReport
        {
            HasIntelGpu = true,
            IntelGpuName = "Intel(R) Iris(R) Xe Graphics",
        };

        // VulkanReady is derived from Checks — leave empty so VulkanReady is vacuously true
        // via All() on empty? Actually empty All returns true. Add failing checks.
        report.Checks.Add(new PrerequisiteCheck("Vulkan Runtime (vulkan-1.dll)", false, "Missing"));
        report.Checks.Add(new PrerequisiteCheck("Intel Vulkan ICD", false, "Missing"));
        report.Checks.Add(new PrerequisiteCheck("Managed ggml-vulkan.dll", false, "Missing"));

        Assert.False(report.VulkanReady);

        var lines = IntelPrerequisiteService.GetManualInstructions(report);
        var text = string.Join('\n', lines);

        Assert.Contains("KhronosGroup.VulkanRT", text);
        Assert.Contains("llm gpu fix --vulkan", text);
        Assert.Contains("oneAPI", text);
    }

    [Fact]
    public void VulkanReady_RequiresAllVulkanChecks()
    {
        var report = new IntelPrerequisiteReport { HasIntelGpu = true };
        report.Checks.Add(new PrerequisiteCheck("Vulkan Runtime (vulkan-1.dll)", true, "ok"));
        report.Checks.Add(new PrerequisiteCheck("Intel Vulkan ICD", true, "ok"));
        report.Checks.Add(new PrerequisiteCheck("Managed ggml-vulkan.dll", false, "missing"));

        Assert.False(report.VulkanReady);
    }
}
