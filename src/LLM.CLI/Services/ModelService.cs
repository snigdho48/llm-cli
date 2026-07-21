using System.Text.Json;
using LLM.CLI.Configuration;

namespace LLM.CLI.Services;

public sealed class ModelService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly UserDataService _userData;

    public ModelService(UserDataService userData)
    {
        _userData = userData;
    }

    public string RegistryFile =>
        Path.Combine(_userData.RootDirectory, "models.json");

    public ModelRegistry LoadRegistry()
    {
        _userData.EnsureCreated();

        if (!File.Exists(RegistryFile))
        {
            return new ModelRegistry();
        }

        var json = File.ReadAllText(RegistryFile);
        return JsonSerializer.Deserialize<ModelRegistry>(json) ?? new ModelRegistry();
    }

    public void SaveRegistry(ModelRegistry registry)
    {
        _userData.EnsureCreated();
        File.WriteAllText(RegistryFile, JsonSerializer.Serialize(registry, JsonOptions));
    }

    public IReadOnlyList<ModelEntry> List()
    {
        return LoadRegistry().Models;
    }

    public IReadOnlyList<string> Scan(string directory, bool register = false)
    {
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new ArgumentException("A directory path is required.", nameof(directory));
        }

        var normalized = Path.GetFullPath(Environment.ExpandEnvironmentVariables(directory.Trim()));

        if (!Directory.Exists(normalized))
        {
            throw new DirectoryNotFoundException($"Directory not found: {normalized}");
        }

        var discovered = Directory
            .EnumerateFiles(normalized, "*.gguf", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!register)
        {
            return discovered;
        }

        var registry = LoadRegistry();

        foreach (var filePath in discovered)
        {
            if (registry.Models.Any(model =>
                    string.Equals(model.Path, filePath, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            registry.Models.Add(CreateEntryFromPath(filePath));
        }

        SaveRegistry(registry);
        return discovered;
    }

    public ModelEntry Add(string filePath, string? id = null, string? name = null)
    {
        var normalized = Path.GetFullPath(Environment.ExpandEnvironmentVariables(filePath.Trim()));

        if (!File.Exists(normalized))
        {
            throw new FileNotFoundException($"Model file not found: {normalized}");
        }

        if (!normalized.EndsWith(".gguf", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only .gguf model files are supported.");
        }

        var registry = LoadRegistry();
        var entry = CreateEntryFromPath(normalized, id, name);

        if (registry.Models.Any(model =>
                string.Equals(model.Id, entry.Id, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Model id already exists: {entry.Id}");
        }

        registry.Models.Add(entry);
        SaveRegistry(registry);
        return entry;
    }

    public void Remove(string id)
    {
        var registry = LoadRegistry();
        var removed = registry.Models.RemoveAll(model =>
            string.Equals(model.Id, id, StringComparison.OrdinalIgnoreCase));

        if (removed == 0)
        {
            throw new InvalidOperationException($"Model not found: {id}");
        }

        if (string.Equals(registry.DefaultModelId, id, StringComparison.OrdinalIgnoreCase))
        {
            registry.DefaultModelId = null;
        }

        SaveRegistry(registry);
    }

    public void Use(string id)
    {
        var registry = LoadRegistry();
        var entry = registry.Models.FirstOrDefault(model =>
            string.Equals(model.Id, id, StringComparison.OrdinalIgnoreCase));

        if (entry is null)
        {
            throw new InvalidOperationException($"Model not found: {id}");
        }

        if (!File.Exists(entry.Path))
        {
            throw new FileNotFoundException($"Model file not found: {entry.Path}");
        }

        registry.DefaultModelId = entry.Id;
        SaveRegistry(registry);

        var config = _userData.Load();
        config.Runtime.ActiveModelPath = entry.Path;
        _userData.Save(config);
    }

    public string ResolveDefaultModelsDirectory()
    {
        var config = _userData.Load();
        return Path.IsPathRooted(config.Runtime.ModelsDirectory)
            ? config.Runtime.ModelsDirectory
            : Path.Combine(config.RootDirectory, config.Runtime.ModelsDirectory);
    }

    private static ModelEntry CreateEntryFromPath(
        string filePath,
        string? id = null,
        string? name = null)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var inferredId = id ?? SanitizeId(fileName);

        return new ModelEntry
        {
            Id = inferredId,
            Name = name ?? fileName,
            Path = filePath,
            SizeBytes = new FileInfo(filePath).Length,
            AddedAt = DateTimeOffset.UtcNow,
        };
    }

    private static string SanitizeId(string value)
    {
        var chars = value
            .Select(character => char.IsLetterOrDigit(character) ? character : '-')
            .ToArray();

        var sanitized = new string(chars).Trim('-');

        while (sanitized.Contains("--", StringComparison.Ordinal))
        {
            sanitized = sanitized.Replace("--", "-", StringComparison.Ordinal);
        }

        return string.IsNullOrWhiteSpace(sanitized) ? "model" : sanitized.ToLowerInvariant();
    }
}
