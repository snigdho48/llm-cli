using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using LLM.CLI.Configuration;

namespace LLM.CLI.Services;

public sealed class OpenAiService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private readonly UserDataService _userData;
    private readonly RuntimeService _runtime;

    public OpenAiService(UserDataService userData, RuntimeService runtime)
    {
        _userData = userData;
        _runtime = runtime;
    }

    public async Task<string> ChatAsync(
        string prompt,
        bool ensureRuntime = true,
        CancellationToken cancellationToken = default)
    {
        if (ensureRuntime)
        {
            await _runtime.EnsureRunningAsync(cancellationToken: cancellationToken);
        }

        var config = _userData.Load();
        var url = $"http://{config.Runtime.Host}:{config.Runtime.Port}/v1/chat/completions";

        using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", config.Runtime.ApiKey);

        var payload = new
        {
            model = "local",
            messages = new[]
            {
                new { role = "user", content = prompt },
            },
            temperature = 0.2,
            max_tokens = 512,
        };

        using var content = new StringContent(
            JsonSerializer.Serialize(payload, JsonOptions),
            Encoding.UTF8,
            "application/json");

        using var response = await client.PostAsync(url, content, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Chat request failed ({(int)response.StatusCode}): {body}");
        }

        using var document = JsonDocument.Parse(body);
        var message = document
            .RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return message ?? string.Empty;
    }
}
