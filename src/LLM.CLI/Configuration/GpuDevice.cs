namespace LLM.CLI.Configuration;

public enum GpuVendor
{
    Unknown = 0,
    Intel = 1,
    Nvidia = 2,
    Amd = 3,
    Cpu = 4,
}

public enum GpuBackend
{
    Auto = 0,
    Cpu = 1,
    Vulkan = 2,
    Cuda = 3,
    Rocm = 4,
}

public sealed class GpuDevice
{
    public int Index { get; init; }

    public string Name { get; init; } = "";

    public GpuVendor Vendor { get; init; }

    public long AdapterRamBytes { get; init; }

    public string DriverVersion { get; init; } = "";

    public string PnpDeviceId { get; init; } = "";

    public bool IsLikelyDiscrete { get; init; }

    public string VendorLabel => Vendor switch
    {
        GpuVendor.Intel => "Intel",
        GpuVendor.Nvidia => "NVIDIA",
        GpuVendor.Amd => "AMD",
        GpuVendor.Cpu => "CPU",
        _ => "Unknown",
    };

    public string AdapterRamLabel =>
        AdapterRamBytes <= 0
            ? "shared/unknown"
            : $"{AdapterRamBytes / (1024.0 * 1024 * 1024):0.0} GB";
}

public sealed class GpuBackendInfo
{
    public GpuBackend Backend { get; init; }

    public bool Available { get; init; }

    public string? LibraryPath { get; init; }

    public string Label => Backend switch
    {
        GpuBackend.Cuda => "CUDA (NVIDIA)",
        GpuBackend.Rocm => "ROCm/HIP (AMD)",
        GpuBackend.Vulkan => "Vulkan (Intel/AMD/NVIDIA)",
        GpuBackend.Cpu => "CPU",
        GpuBackend.Auto => "Auto",
        _ => Backend.ToString(),
    };
}
