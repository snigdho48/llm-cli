using LLM.CLI.Configuration;
using LLM.CLI.Services;
using Xunit;

namespace LLM.Tests.Services;

public sealed class GpuDetectionServiceTests
{
    [Theory]
    [InlineData("NVIDIA GeForce RTX 4060", "PCI\\VEN_10DE&DEV_28A0", GpuVendor.Nvidia)]
    [InlineData("AMD Radeon RX 7600", "PCI\\VEN_1002&DEV_7480", GpuVendor.Amd)]
    [InlineData("Intel(R) Iris(R) Xe Graphics", "PCI\\VEN_8086&DEV_9A49", GpuVendor.Intel)]
    public void DetectVendor_FromPnpAndName(string name, string pnp, GpuVendor expected)
    {
        Assert.Equal(expected, GpuDetectionService.DetectVendor(name, pnp));
    }

    [Fact]
    public void ParseGpuCsv_SkipsBasicRenderAdapter()
    {
        var csv =
            """
            "Name","AdapterRAM","DriverVersion","PNPDeviceID"
            "Intel(R) Iris(R) Xe Graphics","0","31.0","PCI\VEN_8086&DEV_9A49"
            "Microsoft Basic Render Driver","0","10.0","ROOT\DISPLAY"
            "NVIDIA GeForce RTX 4060","8589934592","560.0","PCI\VEN_10DE&DEV_28A0"
            """;

        var devices = GpuDetectionService.ParseGpuCsv(csv);

        Assert.Equal(2, devices.Count);
        Assert.Equal(GpuVendor.Intel, devices[0].Vendor);
        Assert.False(devices[0].IsLikelyDiscrete);
        Assert.Equal(GpuVendor.Nvidia, devices[1].Vendor);
        Assert.True(devices[1].IsLikelyDiscrete);
    }

    [Fact]
    public void RecommendDevice_PrefersDiscreteNvidiaOverIntel()
    {
        var devices = new List<GpuDevice>
        {
            new() { Index = 0, Name = "Intel Iris Xe", Vendor = GpuVendor.Intel, IsLikelyDiscrete = false },
            new() { Index = 1, Name = "NVIDIA RTX 4060", Vendor = GpuVendor.Nvidia, IsLikelyDiscrete = true },
        };

        var selected = GpuDetectionService.RecommendDevice(devices);

        Assert.Equal(1, selected.Index);
        Assert.Equal(GpuVendor.Nvidia, selected.Vendor);
    }

    [Fact]
    public void RecommendBackend_UsesCudaForNvidiaWhenAvailable()
    {
        var device = new GpuDevice { Index = 0, Name = "RTX", Vendor = GpuVendor.Nvidia, IsLikelyDiscrete = true };
        var backends = new List<GpuBackendInfo>
        {
            new() { Backend = GpuBackend.Cpu, Available = true },
            new() { Backend = GpuBackend.Vulkan, Available = true },
            new() { Backend = GpuBackend.Cuda, Available = true },
        };

        Assert.Equal(GpuBackend.Cuda, GpuDetectionService.RecommendBackend(device, backends));
    }

    [Fact]
    public void RecommendBackend_UsesVulkanForIntel()
    {
        var device = new GpuDevice { Index = 0, Name = "Iris Xe", Vendor = GpuVendor.Intel };
        var backends = new List<GpuBackendInfo>
        {
            new() { Backend = GpuBackend.Cpu, Available = true },
            new() { Backend = GpuBackend.Vulkan, Available = true },
            new() { Backend = GpuBackend.Cuda, Available = false },
        };

        Assert.Equal(GpuBackend.Vulkan, GpuDetectionService.RecommendBackend(device, backends));
    }

    [Fact]
    public void ResolveVendorRelativeIndex_MapsWin32IndexToCudaIndex()
    {
        var devices = new List<GpuDevice>
        {
            new() { Index = 0, Name = "Intel Iris Xe", Vendor = GpuVendor.Intel },
            new() { Index = 1, Name = "NVIDIA RTX 4060", Vendor = GpuVendor.Nvidia },
        };

        // Win32 index 1 (NVIDIA) -> CUDA device 0
        Assert.Equal(0, GpuDetectionService.ResolveVendorRelativeIndex(1, GpuBackend.Cuda, devices));
    }

    [Theory]
    [InlineData(GpuVendor.Intel, null, "iris-xe-coding")]
    [InlineData(GpuVendor.Nvidia, null, "nvidia-cuda")]
    [InlineData(GpuVendor.Amd, null, "amd-rocm")]
    [InlineData(GpuVendor.Intel, "cpu", "cpu-only")]
    [InlineData(GpuVendor.Cpu, null, "cpu-only")]
    public void ResolveProfileId_MapsVendorAndBackend(GpuVendor vendor, string? backend, string expected)
    {
        var device = new GpuDevice { Index = 0, Name = "test", Vendor = vendor };
        Assert.Equal(expected, GpuDetectionService.ResolveProfileId(device, backend));
    }
}
