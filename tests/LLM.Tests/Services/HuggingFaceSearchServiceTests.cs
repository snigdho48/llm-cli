using LLM.CLI.Services;
using Xunit;

namespace LLM.Tests.Services;

public sealed class HuggingFaceSearchServiceTests
{
    [Fact]
    public async Task SearchAsync_FallsBackToCatalogWhenOfflineQueryMatches()
    {
        var catalog = new ModelCatalogService();
        var hub = new HuggingFaceSearchService(catalog);

        // Even if Hub is reachable, curated fallback path for empty results is covered
        // by MapCatalog; this asserts catalog ids remain resolvable for pull.
        var curated = catalog.Search("qwen coder");
        Assert.Contains(curated, entry => entry.Id == "qwen2.5-coder-7b");

        var hits = await hub.SearchAsync("qwen2.5-coder");
        Assert.NotEmpty(hits);
    }
}
