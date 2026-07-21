using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using LLM.CLI.Configuration;

namespace LLM.CLI.Services;

/// <summary>
/// Live Hugging Face Hub search for GGUF models (falls back to curated catalog offline).
/// </summary>
public sealed class HuggingFaceSearchService
{
    private const string HubSearchUrl =
        "https://huggingface.co/api/models?search={0}&filter=gguf&limit={1}&sort=downloads&direction=-1";

    private readonly ModelCatalogService _catalog;

    public HuggingFaceSearchService(ModelCatalogService catalog)
    {
        _catalog = catalog;
    }

    public async Task<IReadOnlyList<HuggingFaceSearchHit>> SearchAsync(
        string query,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return MapCatalog(_catalog.Search(null, recommendedOnly: true));
        }

        try
        {
            using var client = CreateClient();
            var url = string.Format(HubSearchUrl, Uri.EscapeDataString(query.Trim()), limit);
            var models = await client.GetFromJsonAsync<List<HfModel>>(url, cancellationToken);

            if (models is null || models.Count == 0)
            {
                return MapCatalog(_catalog.Search(query));
            }

            return models
                .Select(model => new HuggingFaceSearchHit
                {
                    Id = model.Id ?? "",
                    Downloads = model.Downloads,
                    Likes = model.Likes,
                    PipelineTag = model.PipelineTag ?? "",
                    Source = "huggingface",
                })
                .Where(hit => !string.IsNullOrWhiteSpace(hit.Id))
                .ToList();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            var fallback = MapCatalog(_catalog.Search(query));
            foreach (var hit in fallback)
            {
                hit.Notes = $"Offline catalog (Hub unavailable: {ex.Message})";
            }

            return fallback;
        }
    }

    public async Task<IReadOnlyList<string>> ListGgufFilesAsync(
        string repositoryId,
        CancellationToken cancellationToken = default)
    {
        using var client = CreateClient();
        var url = $"https://huggingface.co/api/models/{repositoryId}/tree/main";
        var nodes = await client.GetFromJsonAsync<List<HfTreeNode>>(url, cancellationToken);

        return nodes?
            .Where(node =>
                string.Equals(node.Type, "file", StringComparison.OrdinalIgnoreCase)
                && (node.Path?.EndsWith(".gguf", StringComparison.OrdinalIgnoreCase) ?? false))
            .Select(node => node.Path!)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList()
            ?? [];
    }

    private static List<HuggingFaceSearchHit> MapCatalog(IReadOnlyList<ModelCatalogEntry> entries)
    {
        return entries
            .Select(entry => new HuggingFaceSearchHit
            {
                Id = entry.Repository,
                Downloads = 0,
                Likes = 0,
                PipelineTag = "text-generation",
                Source = "catalog",
                Notes = entry.Notes,
                SuggestedFile = entry.FileName,
                CatalogId = entry.Id,
            })
            .ToList();
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("llm-cli/1.0");
        return client;
    }

    private sealed class HfModel
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("downloads")]
        public long Downloads { get; set; }

        [JsonPropertyName("likes")]
        public long Likes { get; set; }

        [JsonPropertyName("pipeline_tag")]
        public string? PipelineTag { get; set; }
    }

    private sealed class HfTreeNode
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("path")]
        public string? Path { get; set; }
    }
}

public sealed class HuggingFaceSearchHit
{
    public string Id { get; set; } = "";

    public long Downloads { get; set; }

    public long Likes { get; set; }

    public string PipelineTag { get; set; } = "";

    public string Source { get; set; } = "catalog";

    public string? Notes { get; set; }

    public string? SuggestedFile { get; set; }

    public string? CatalogId { get; set; }
}
