using Microsoft.Extensions.Options;
using LLM.CLI.Configuration.Options;

namespace LLM.CLI.Configuration;

public sealed class ConfigurationService
{
    private readonly LLMOptions _options;

    public ConfigurationService(IOptions<LLMOptions> options)
    {
        _options = options.Value;
    }

    public LLMOptions Options => _options;
}