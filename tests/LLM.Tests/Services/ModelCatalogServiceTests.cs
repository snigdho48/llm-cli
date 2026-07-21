using LLM.CLI.Services;
using Xunit;

namespace LLM.Tests.Services;

public sealed class ModelCatalogServiceTests
{
    private readonly ModelCatalogService _catalog = new();

    [Fact]
    public void Search_FindsCoderModels()
    {
        var results = _catalog.Search("coder");

        Assert.NotEmpty(results);
        Assert.Contains(results, entry => entry.Id == "qwen2.5-coder-7b");
    }

    [Fact]
    public void Resolve_ByCatalogId_ReturnsHuggingFaceUrl()
    {
        var entry = _catalog.Resolve("qwen2.5-coder-7b");

        Assert.Contains("Qwen/Qwen2.5-Coder-7B-Instruct-GGUF", entry.HuggingFaceUrl);
        Assert.Contains("qwen2.5-coder-7b-instruct-q4_k_m.gguf", entry.HuggingFaceUrl);
    }

    [Fact]
    public void Resolve_ByRepositoryPath_Works()
    {
        var entry = _catalog.Resolve(
            "Qwen/Qwen2.5-Coder-7B-Instruct-GGUF/qwen2.5-coder-7b-instruct-q4_k_m.gguf");

        Assert.Equal("Qwen/Qwen2.5-Coder-7B-Instruct-GGUF", entry.Repository);
        Assert.Equal("qwen2.5-coder-7b-instruct-q4_k_m.gguf", entry.FileName);
    }

    [Fact]
    public void TryParseRepositoryReference_ValidatesFormat()
    {
        var ok = ModelCatalogService.TryParseRepositoryReference(
            "Qwen/Qwen2.5-Coder-7B-Instruct-GGUF/model.gguf",
            out var repo,
            out var file);

        Assert.True(ok);
        Assert.Equal("Qwen/Qwen2.5-Coder-7B-Instruct-GGUF", repo);
        Assert.Equal("model.gguf", file);
    }
}
