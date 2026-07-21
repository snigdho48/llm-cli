using System.IO.Compression;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using LLM.CLI.Configuration;

namespace LLM.CLI.Services;

public sealed class RuntimeInstallService
{
    private const string GitHubApiLatest =
        "https://api.github.com/repos/ggml-org/llama.cpp/releases/latest";

    private readonly UserDataService _userData;
    private readonly RuntimeImportService _importService;

    public RuntimeInstallService(
        UserDataService userData,
        RuntimeImportService importService)
    {
        _userData = userData;
        _importService = importService;
    }

    public async Task<IReadOnlyList<string>> InstallLatestAsync(
        CancellationToken cancellationToken = default)
    {
        using var client = CreateGitHubClient();
        var release = await client.GetFromJsonAsync<GitHubRelease>(
            GitHubApiLatest,
            cancellationToken);

        if (release is null)
        {
            throw new InvalidOperationException("Unable to read llama.cpp release metadata.");
        }

        var asset = FindWindowsVulkanAsset(release.Assets);
        if (asset is null)
        {
            throw new InvalidOperationException(
                "No Windows Vulkan binary found in latest release. " +
                "Use: llm runtime import <path-to-local-build>");
        }

        var config = _userData.Load();
        var downloadRoot = Path.Combine(config.RootDirectory, "downloads", "runtimes");
        Directory.CreateDirectory(downloadRoot);

        var zipPath = Path.Combine(downloadRoot, asset.Name);
        await DownloadAssetAsync(client, asset, zipPath, cancellationToken);

        var extractRoot = Path.Combine(downloadRoot, "extracted", release.TagName);
        if (Directory.Exists(extractRoot))
        {
            Directory.Delete(extractRoot, recursive: true);
        }

        Directory.CreateDirectory(extractRoot);
        ZipFile.ExtractToDirectory(zipPath, extractRoot);

        var buildDirectory = FindBuildDirectory(extractRoot);
        var copied = _importService.Import(config, buildDirectory);
        _userData.Save(config);

        return copied;
    }

    public static GitHubAsset? FindWindowsVulkanAsset(IReadOnlyList<GitHubAsset> assets)
    {
        return assets.FirstOrDefault(asset =>
                   asset.Name.Contains("win", StringComparison.OrdinalIgnoreCase)
                   && asset.Name.Contains("vulkan", StringComparison.OrdinalIgnoreCase)
                   && asset.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
               ?? assets.FirstOrDefault(asset =>
                   asset.Name.Contains("win", StringComparison.OrdinalIgnoreCase)
                   && asset.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));
    }

    public static string FindBuildDirectory(string extractRoot)
    {
        var releaseExe = Directory
            .EnumerateFiles(extractRoot, "llama-server.exe", SearchOption.AllDirectories)
            .FirstOrDefault(path =>
                path.Contains($"{Path.DirectorySeparatorChar}Release{Path.DirectorySeparatorChar}",
                    StringComparison.OrdinalIgnoreCase));

        if (releaseExe is not null)
        {
            return Path.GetDirectoryName(releaseExe)!;
        }

        var anyExe = Directory
            .EnumerateFiles(extractRoot, "llama-server.exe", SearchOption.AllDirectories)
            .FirstOrDefault();

        return anyExe is null
            ? extractRoot
            : Path.GetDirectoryName(anyExe)!;
    }

    private static HttpClient CreateGitHubClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(30),
        };

        client.DefaultRequestHeaders.UserAgent.ParseAdd("llm-cli/1.0");
        return client;
    }

    private static async Task DownloadAssetAsync(
        HttpClient client,
        GitHubAsset asset,
        string destination,
        CancellationToken cancellationToken)
    {
        using var response = await client.GetAsync(
            asset.BrowserDownloadUrl,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var target = File.Create(destination);
        await source.CopyToAsync(target, cancellationToken);
    }
}

public sealed class GitHubRelease
{
    [JsonPropertyName("tag_name")]
    public string TagName { get; set; } = "";

    [JsonPropertyName("assets")]
    public List<GitHubAsset> Assets { get; set; } = [];
}

public sealed class GitHubAsset
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("browser_download_url")]
    public string BrowserDownloadUrl { get; set; } = "";
}
