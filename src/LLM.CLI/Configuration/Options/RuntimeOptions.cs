namespace LLM.CLI.Configuration.Options;

public sealed class RuntimeOptions
{
    public string Provider { get; set; } = "llama.cpp";

    public int Port { get; set; } = 8080;

    public string ModelsDirectory { get; set; } = "models";
}