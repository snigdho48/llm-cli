namespace LLM.CLI.Configuration;

public sealed class ModelRegistry
{
    public string? DefaultModelId { get; set; }

    public List<ModelEntry> Models { get; set; } = [];
}

public sealed class ModelEntry
{
    public string Id { get; set; } = "";

    public string Name { get; set; } = "";

    public string Path { get; set; } = "";

    public long SizeBytes { get; set; }

    public DateTimeOffset AddedAt { get; set; } = DateTimeOffset.UtcNow;
}
