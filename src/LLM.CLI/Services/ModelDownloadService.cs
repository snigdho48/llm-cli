namespace LLM.CLI.Services;

public sealed class ModelDownloadService
{
    private readonly UserDataService _userData;

    public ModelDownloadService(UserDataService userData)
    {
        _userData = userData;
    }

    public string GetDownloadsDirectory()
    {
        var config = _userData.Load();
        return Path.Combine(config.RootDirectory, "downloads");
    }

    public async Task<string> DownloadAsync(
        string url,
        string? fileName = null,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("A download URL is required.", nameof(url));
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException($"Invalid URL: {url}", nameof(url));
        }

        fileName ??= Path.GetFileName(uri.LocalPath);
        if (string.IsNullOrWhiteSpace(fileName) || !fileName.EndsWith(".gguf", StringComparison.OrdinalIgnoreCase))
        {
            fileName = $"model-{DateTime.UtcNow:yyyyMMddHHmmss}.gguf";
        }

        var downloadsDirectory = GetDownloadsDirectory();
        Directory.CreateDirectory(downloadsDirectory);

        var destination = Path.Combine(downloadsDirectory, fileName);
        if (File.Exists(destination))
        {
            throw new InvalidOperationException($"File already exists: {destination}");
        }

        using var client = new HttpClient { Timeout = TimeSpan.FromHours(6) };
        using var response = await client.GetAsync(
            uri,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1;
        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var target = File.Create(destination);

        var buffer = new byte[81920];
        long downloaded = 0;
        int read;

        while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            downloaded += read;
            progress?.Report(new DownloadProgress(downloaded, totalBytes));
        }

        return destination;
    }
}

public readonly record struct DownloadProgress(long BytesDownloaded, long TotalBytes)
{
    public double? Percent =>
        TotalBytes > 0 ? (double)BytesDownloaded / TotalBytes * 100 : null;
}
