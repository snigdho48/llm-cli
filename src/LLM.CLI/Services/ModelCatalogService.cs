using LLM.CLI.Configuration;

namespace LLM.CLI.Services;

public sealed class ModelCatalogService
{
    private static readonly IReadOnlyList<ModelCatalogEntry> Catalog =
    [
        new()
        {
            Id = "qwen2.5-coder-7b",
            Name = "Qwen2.5-Coder 7B Instruct Q4_K_M",
            Repository = "Qwen/Qwen2.5-Coder-7B-Instruct-GGUF",
            FileName = "qwen2.5-coder-7b-instruct-q4_k_m.gguf",
            SizeGb = 4.7,
            SpeedRating = "8-15 tok/s",
            CodingQuality = "Excellent",
            Tags = ["coding", "7b", "qwen", "recommended", "iris-xe"],
            Notes = "Best balance for HP EliteBook 840 G8 (32 GB). Primary recommendation.",
            RecommendedFor32Gb = true,
        },
        new()
        {
            Id = "qwen3-coder-8b",
            Name = "Qwen3-Coder 8B Q4_K_M",
            Repository = "Qwen/Qwen3-Coder-8B-GGUF",
            FileName = "Qwen3-Coder-8B-Q4_K_M.gguf",
            SizeGb = 5.2,
            SpeedRating = "7-12 tok/s",
            CodingQuality = "Excellent",
            Tags = ["coding", "8b", "qwen", "recommended"],
            Notes = "Newer coder model; slightly heavier than 7B.",
            RecommendedFor32Gb = true,
        },
        new()
        {
            Id = "codellama-7b",
            Name = "CodeLlama 7B Instruct Q4_K_M",
            Repository = "TheBloke/CodeLlama-7B-Instruct-GGUF",
            FileName = "codellama-7b-instruct.Q4_K_M.gguf",
            SizeGb = 4.1,
            SpeedRating = "8-15 tok/s",
            CodingQuality = "Good",
            Tags = ["coding", "7b", "meta"],
            Notes = "Solid alternative; Qwen2.5-Coder usually wins on code tasks.",
            RecommendedFor32Gb = true,
        },
        new()
        {
            Id = "mistral-7b",
            Name = "Mistral 7B Instruct v0.3 Q4_K_M",
            Repository = "TheBloke/Mistral-7B-Instruct-v0.3-GGUF",
            FileName = "mistral-7b-instruct-v0.3.Q4_K_M.gguf",
            SizeGb = 4.4,
            SpeedRating = "10-18 tok/s",
            CodingQuality = "Good",
            Tags = ["general", "7b", "fast"],
            Notes = "Fast general model; coding quality below Qwen-Coder series.",
            RecommendedFor32Gb = true,
        },
        new()
        {
            Id = "deepseek-coder-v2-lite",
            Name = "DeepSeek-Coder-V2-Lite 16B Q4_K_M",
            Repository = "bartowski/DeepSeek-Coder-V2-Lite-Instruct-GGUF",
            FileName = "DeepSeek-Coder-V2-Lite-Instruct-Q4_K_M.gguf",
            SizeGb = 10.0,
            SpeedRating = "2-5 tok/s",
            CodingQuality = "Excellent",
            Tags = ["coding", "16b", "slow"],
            Notes = "High quality but slow on i5-1145G7. Usable for hard questions, not fast iteration.",
            RecommendedFor32Gb = true,
        },
        new()
        {
            Id = "qwen2.5-coder-3b",
            Name = "Qwen2.5-Coder 3B Instruct Q4_K_M",
            Repository = "Qwen/Qwen2.5-Coder-3B-Instruct-GGUF",
            FileName = "qwen2.5-coder-3b-instruct-q4_k_m.gguf",
            SizeGb = 2.0,
            SpeedRating = "20-35 tok/s",
            CodingQuality = "Good",
            Tags = ["coding", "3b", "fast", "low-ram"],
            Notes = "Fastest option when responsiveness matters more than depth.",
            RecommendedFor32Gb = true,
        },
    ];

    public IReadOnlyList<ModelCatalogEntry> Search(string? query = null, bool recommendedOnly = false)
    {
        IEnumerable<ModelCatalogEntry> results = Catalog;

        if (recommendedOnly)
        {
            results = results.Where(entry => entry.RecommendedFor32Gb);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var terms = query
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            results = results.Where(entry =>
                terms.All(term =>
                    entry.Id.Contains(term, StringComparison.OrdinalIgnoreCase)
                    || entry.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                    || entry.Tags.Any(tag => tag.Contains(term, StringComparison.OrdinalIgnoreCase))));
        }

        return results.ToList();
    }

    public ModelCatalogEntry Resolve(string reference)
    {
        var normalized = reference.Trim();

        var byId = Catalog.FirstOrDefault(entry =>
            string.Equals(entry.Id, normalized, StringComparison.OrdinalIgnoreCase));

        if (byId is not null)
        {
            return byId;
        }

        if (TryParseRepositoryReference(normalized, out var repo, out var fileName))
        {
            return new ModelCatalogEntry
            {
                Id = SanitizeId(fileName),
                Name = fileName,
                Repository = repo,
                FileName = fileName,
            };
        }

        throw new InvalidOperationException(
            $"Unknown model '{reference}'. Run: llm model search");
    }

    public static bool TryParseRepositoryReference(
        string reference,
        out string repository,
        out string fileName)
    {
        repository = "";
        fileName = "";

        var slashIndex = reference.IndexOf('/');
        if (slashIndex <= 0)
        {
            return false;
        }

        var lastSlash = reference.LastIndexOf('/');
        if (lastSlash <= slashIndex)
        {
            return false;
        }

        repository = reference[..lastSlash];
        fileName = reference[(lastSlash + 1)..];

        return fileName.EndsWith(".gguf", StringComparison.OrdinalIgnoreCase);
    }

    public static string SanitizeId(string value)
    {
        return Path.GetFileNameWithoutExtension(value)
            .Replace('.', '-')
            .ToLowerInvariant();
    }
}
