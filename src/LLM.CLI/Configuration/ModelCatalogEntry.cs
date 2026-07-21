namespace LLM.CLI.Configuration;

public sealed class ModelCatalogEntry
{
    public string Id { get; init; } = "";

    public string Name { get; init; } = "";

    public string Repository { get; init; } = "";

    public string FileName { get; init; } = "";

    public double SizeGb { get; init; }

    public string SpeedRating { get; init; } = "";

    public string CodingQuality { get; init; } = "";

    public IReadOnlyList<string> Tags { get; init; } = [];

    public string Notes { get; init; } = "";

    public bool RecommendedFor32Gb { get; init; }

    public string HuggingFaceUrl =>
        $"https://huggingface.co/{Repository.Trim('/')}/resolve/main/{FileName}";
}
