using LLM.CLI.Services;

namespace LLM.CLI.Commands;

public sealed class ModelCommand : ICommand
{
    private readonly ModelService _models;
    private readonly ModelDownloadService _downloads;
    private readonly ModelCatalogService _catalog;
    private readonly HuggingFaceSearchService _hub;

    public ModelCommand(
        ModelService models,
        ModelDownloadService downloads,
        ModelCatalogService catalog,
        HuggingFaceSearchService hub)
    {
        _models = models;
        _downloads = downloads;
        _catalog = catalog;
        _hub = hub;
    }

    public string Name => "model";

    public async Task<int> ExecuteAsync(string[] args)
    {
        if (args.Length == 0)
        {
            PrintHelp();
            return CommandResults.Success;
        }

        try
        {
            switch (args[0].ToLowerInvariant())
            {
                case "list":
                    ListModels();
                    break;

                case "scan":
                    ScanModels(args);
                    break;

                case "add":
                    AddModel(args);
                    break;

                case "use":
                    UseModel(args);
                    break;

                case "remove":
                    RemoveModel(args);
                    break;

                case "download":
                    await DownloadModelAsync(args);
                    break;

                case "search":
                    await SearchModelsAsync(args);
                    break;

                case "files":
                    await ListHubFilesAsync(args);
                    break;

                case "pull":
                    await PullModelAsync(args);
                    break;

                default:
                    Console.WriteLine($"Unknown model command: {args[0]}");
                    PrintHelp();
                    break;
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or FileNotFoundException or DirectoryNotFoundException or ArgumentException or HttpRequestException)
        {
            Console.WriteLine($"[FAIL] {ex.Message}");
            return CommandResults.Failure;
        }

        return CommandResults.Success;
    }

    private void ListModels()
    {
        var registry = _models.List();
        var configDefault = _models.LoadRegistry().DefaultModelId;

        Console.WriteLine("Registered Models");
        Console.WriteLine("-----------------");
        Console.WriteLine();

        if (registry.Count == 0)
        {
            Console.WriteLine("No models registered.");
            Console.WriteLine();
            Console.WriteLine("Try:");
            Console.WriteLine("  llm model scan D:\\MODEL --register");
            Console.WriteLine("  llm model add D:\\MODEL\\your-model.gguf");
            return;
        }

        Console.WriteLine($"{"ID",-20} {"SIZE",-12} {"DEFAULT",-8} PATH");
        Console.WriteLine(new string('-', 90));

        foreach (var model in registry)
        {
            var isDefault = string.Equals(model.Id, configDefault, StringComparison.OrdinalIgnoreCase);
            var size = FormatSize(model.SizeBytes);

            Console.WriteLine(
                $"{model.Id,-20} {size,-12} {(isDefault ? "*" : ""),-8} {model.Path}");
        }
    }

    private void ScanModels(string[] args)
    {
        var register = args.Any(arg => string.Equals(arg, "--register", StringComparison.OrdinalIgnoreCase));
        var path = args.Skip(1).FirstOrDefault(arg => !arg.StartsWith('-'));

        path ??= _models.ResolveDefaultModelsDirectory();

        var discovered = _models.Scan(path, register);

        Console.WriteLine();
        Console.WriteLine($"Scanned: {path}");
        Console.WriteLine($"Found {discovered.Count} GGUF file(s).");

        foreach (var file in discovered)
        {
            Console.WriteLine($"  {file}");
        }

        if (register)
        {
            Console.WriteLine();
            Console.WriteLine("[ OK ] New models registered.");
        }
        else
        {
            Console.WriteLine();
            Console.WriteLine("To register them, run:");
            Console.WriteLine($"  llm model scan \"{path}\" --register");
        }
    }

    private void AddModel(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: llm model add <path-to-model.gguf> [id]");
            return;
        }

        var path = args[1];
        var id = args.Length > 2 ? args[2] : null;
        var entry = _models.Add(path, id);

        Console.WriteLine();
        Console.WriteLine("[ OK ] Model registered.");
        Console.WriteLine($"ID   : {entry.Id}");
        Console.WriteLine($"Path : {entry.Path}");
        Console.WriteLine();
        Console.WriteLine($"Run: llm model use {entry.Id}");
    }

    private void UseModel(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: llm model use <id>");
            return;
        }

        _models.Use(args[1]);

        Console.WriteLine();
        Console.WriteLine($"[ OK ] Active model set to '{args[1]}'.");
    }

    private void RemoveModel(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: llm model remove <id>");
            return;
        }

        _models.Remove(args[1]);
        Console.WriteLine();
        Console.WriteLine($"[ OK ] Removed model '{args[1]}'.");
    }

    private static string FormatSize(long bytes)
    {
        const double gb = 1024 * 1024 * 1024;
        return $"{bytes / gb:0.0} GB";
    }

    private async Task DownloadModelAsync(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: llm model download <url> [filename.gguf] [--register] [--use]");
            return;
        }

        var register = args.Contains("--register", StringComparer.OrdinalIgnoreCase);
        var useModel = args.Contains("--use", StringComparer.OrdinalIgnoreCase);
        var positional = args.Skip(1).Where(arg => !arg.StartsWith('-')).ToArray();

        if (positional.Length == 0)
        {
            Console.WriteLine("Usage: llm model download <url> [filename.gguf]");
            return;
        }

        var url = positional[0];
        var fileName = positional.Length > 1 ? positional[1] : null;

        Console.WriteLine($"Downloading from:");
        Console.WriteLine($"  {url}");
        Console.WriteLine();

        var lastPercent = -1;
        var path = await _downloads.DownloadAsync(
            url,
            fileName,
            new Progress<DownloadProgress>(progress =>
            {
                if (progress.Percent is null)
                {
                    Console.Write($"\rDownloaded {progress.BytesDownloaded / (1024 * 1024):0} MB...");
                }
                else
                {
                    var percent = (int)progress.Percent.Value;
                    if (percent != lastPercent)
                    {
                        lastPercent = percent;
                        Console.Write($"\rProgress: {percent,3}%");
                    }
                }
            }));

        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine("[ OK ] Download complete.");
        Console.WriteLine($"Path: {path}");

        if (register || useModel)
        {
            var entry = _models.Add(path);
            Console.WriteLine($"Registered as: {entry.Id}");

            if (useModel)
            {
                _models.Use(entry.Id);
                Console.WriteLine("[ OK ] Active model updated.");
            }
        }
        else
        {
            Console.WriteLine();
            Console.WriteLine("Register with:");
            Console.WriteLine($"  llm model add \"{path}\"");
        }
    }

    private async Task SearchModelsAsync(string[] args)
    {
        var recommendedOnly = args.Contains("--recommended", StringComparer.OrdinalIgnoreCase);
        var live = args.Contains("--live", StringComparer.OrdinalIgnoreCase)
                   || args.Contains("--hub", StringComparer.OrdinalIgnoreCase);
        var query = string.Join(' ',
            args.Skip(1).Where(arg => !arg.StartsWith('-')));

        if (live)
        {
            Console.WriteLine("Hugging Face Hub Search");
            Console.WriteLine("-----------------------");
            Console.WriteLine();

            var hits = await _hub.SearchAsync(
                string.IsNullOrWhiteSpace(query) ? "gguf coder" : query);

            if (hits.Count == 0)
            {
                Console.WriteLine("No results.");
                return;
            }

            Console.WriteLine($"{"REPO",-55} {"DL",-10} {"LIKES",-8} SRC");
            Console.WriteLine(new string('-', 90));

            foreach (var hit in hits)
            {
                Console.WriteLine(
                    $"{hit.Id,-55} {hit.Downloads,9:N0} {hit.Likes,7} {hit.Source}");

                if (!string.IsNullOrWhiteSpace(hit.SuggestedFile))
                {
                    Console.WriteLine($"  file: {hit.SuggestedFile}");
                }

                if (!string.IsNullOrWhiteSpace(hit.Notes))
                {
                    Console.WriteLine($"  note: {hit.Notes}");
                }
            }

            Console.WriteLine();
            Console.WriteLine("List GGUF files in a repo:");
            Console.WriteLine("  llm model files Qwen/Qwen2.5-Coder-7B-Instruct-GGUF");
            Console.WriteLine("Pull:");
            Console.WriteLine("  llm model pull <repo>/<file.gguf> --use");
            return;
        }

        var results = _catalog.Search(
            string.IsNullOrWhiteSpace(query) ? null : query,
            recommendedOnly);

        Console.WriteLine("Model Catalog");
        Console.WriteLine("-------------");
        Console.WriteLine();
        Console.WriteLine("Curated for 32 GB RAM / Intel i5-1145G7 / Iris Xe.");
        Console.WriteLine("Tip: llm model search coder --live  (Hugging Face Hub)");
        Console.WriteLine();

        if (results.Count == 0)
        {
            Console.WriteLine("No models matched your search.");
            Console.WriteLine("Try: llm model search coder");
            return;
        }

        Console.WriteLine($"{"ID",-26} {"SIZE",-7} {"SPEED",-14} {"QUALITY",-10} NAME");
        Console.WriteLine(new string('-', 100));

        foreach (var entry in results)
        {
            var marker = entry.RecommendedFor32Gb ? "*" : " ";
            Console.WriteLine(
                $"{marker}{entry.Id,-25} {entry.SizeGb,4:0.0}G {entry.SpeedRating,-14} {entry.CodingQuality,-10} {entry.Name}");
        }

        Console.WriteLine();
        Console.WriteLine("* = recommended for your hardware");
        Console.WriteLine();
        Console.WriteLine("Pull a model:");
        Console.WriteLine("  llm model pull qwen2.5-coder-7b");
        Console.WriteLine("  llm model pull Qwen/Qwen2.5-Coder-7B-Instruct-GGUF/qwen2.5-coder-7b-instruct-q4_k_m.gguf");
    }

    private async Task ListHubFilesAsync(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: llm model files <org/repo>");
            Console.WriteLine("Example: llm model files Qwen/Qwen2.5-Coder-7B-Instruct-GGUF");
            return;
        }

        var repo = args[1];
        Console.WriteLine($"GGUF files in {repo}");
        Console.WriteLine(new string('-', 60));

        var files = await _hub.ListGgufFilesAsync(repo);
        if (files.Count == 0)
        {
            Console.WriteLine("No .gguf files found (or Hub unavailable).");
            return;
        }

        foreach (var file in files)
        {
            Console.WriteLine($"  {file}");
        }

        Console.WriteLine();
        Console.WriteLine($"Pull: llm model pull {repo}/{files[0]} --use");
    }

    private async Task PullModelAsync(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: llm model pull <catalog-id|repo/file.gguf> [--use]");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  llm model pull qwen2.5-coder-7b --use");
            Console.WriteLine("  llm model pull Qwen/Qwen2.5-Coder-7B-Instruct-GGUF/qwen2.5-coder-7b-instruct-q4_k_m.gguf");
            return;
        }

        var useModel = args.Contains("--use", StringComparer.OrdinalIgnoreCase);
        var reference = args.Skip(1).First(arg => !arg.StartsWith('-'));

        var entry = _catalog.Resolve(reference);

        Console.WriteLine($"Pulling: {entry.Name}");
        Console.WriteLine($"  Repo : {entry.Repository}");
        Console.WriteLine($"  File : {entry.FileName}");
        Console.WriteLine($"  URL  : {entry.HuggingFaceUrl}");
        Console.WriteLine();

        var lastPercent = -1;
        var path = await _downloads.DownloadAsync(
            entry.HuggingFaceUrl,
            entry.FileName,
            new Progress<DownloadProgress>(progress =>
            {
                if (progress.Percent is null)
                {
                    Console.Write($"\rDownloaded {progress.BytesDownloaded / (1024 * 1024):0} MB...");
                }
                else
                {
                    var percent = (int)progress.Percent.Value;
                    if (percent != lastPercent)
                    {
                        lastPercent = percent;
                        Console.Write($"\rProgress: {percent,3}%");
                    }
                }
            }));

        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine("[ OK ] Pull complete.");

        var registered = _models.Add(path, entry.Id, entry.Name);
        Console.WriteLine($"Registered as: {registered.Id}");

        if (useModel)
        {
            _models.Use(registered.Id);
            Console.WriteLine("[ OK ] Active model updated.");
            Console.WriteLine();
            Console.WriteLine("Start server: llm serve");
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine();
        Console.WriteLine("  llm model list");
        Console.WriteLine("  llm model search [query] [--recommended] [--live]");
        Console.WriteLine("  llm model files <org/repo>");
        Console.WriteLine("  llm model pull <id|repo/file.gguf> [--use]");
        Console.WriteLine("  llm model scan [path] [--register]");
        Console.WriteLine("  llm model add <path> [id]");
        Console.WriteLine("  llm model download <url> [filename] [--register] [--use]");
        Console.WriteLine("  llm model use <id>");
        Console.WriteLine("  llm model remove <id>");
    }
}
