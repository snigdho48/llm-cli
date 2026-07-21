using System.Text.Json;
using System.Text.Json.Nodes;

namespace LLM.CLI.Services;

public sealed class CursorSettingsService
{
    private readonly UserDataService _userData;

    public CursorSettingsService(UserDataService userData)
    {
        _userData = userData;
    }

    public CursorConnectionInfo GetConnectionInfo()
    {
        var config = _userData.Load();
        return new CursorConnectionInfo
        {
            BaseUrl = $"http://{config.Runtime.Host}:{config.Runtime.Port}/v1",
            ApiKey = config.Runtime.ApiKey,
            Model = "local",
        };
    }

    public string GetSnippetPath()
    {
        return Path.Combine(_userData.RootDirectory, "cursor-settings-snippet.json");
    }

    public string WriteSnippet()
    {
        var info = GetConnectionInfo();
        var snippet = new JsonObject
        {
            ["openaiBaseUrl"] = info.BaseUrl,
            ["openaiApiKey"] = info.ApiKey,
            ["model"] = info.Model,
            ["instructions"] = "Paste into Cursor Settings → Models → OpenAI API override",
        };

        var path = GetSnippetPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, snippet.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        return path;
    }

    public CursorWriteResult TryWriteCursorSettings()
    {
        var info = GetConnectionInfo();
        var settingsPath = ResolveCursorSettingsPath();

        if (settingsPath is null)
        {
            var snippetPath = WriteSnippet();
            return new CursorWriteResult(
                false,
                snippetPath,
                "Cursor settings.json not found. Wrote snippet file instead.");
        }

        JsonObject root;
        if (File.Exists(settingsPath))
        {
            var json = File.ReadAllText(settingsPath);
            root = JsonNode.Parse(json)?.AsObject() ?? new JsonObject();
        }
        else
        {
            root = new JsonObject();
            Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
        }

        root["cursor.general.openAiBaseUrl"] = info.BaseUrl;
        root["cursor.general.openAiKey"] = info.ApiKey;

        var backup = settingsPath + $".bak-{DateTime.UtcNow:yyyyMMddHHmmss}";
        if (File.Exists(settingsPath))
        {
            File.Copy(settingsPath, backup, overwrite: true);
        }

        File.WriteAllText(
            settingsPath,
            root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

        return new CursorWriteResult(true, settingsPath, $"Updated Cursor settings (backup: {backup})");
    }

    public static string? ResolveCursorSettingsPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var path = Path.Combine(appData, "Cursor", "User", "settings.json");
        return Directory.Exists(Path.GetDirectoryName(path)) || File.Exists(path) ? path : null;
    }
}

public sealed class CursorConnectionInfo
{
    public string BaseUrl { get; init; } = "";

    public string ApiKey { get; init; } = "";

    public string Model { get; init; } = "local";
}

public readonly record struct CursorWriteResult(bool Written, string Path, string Message);
