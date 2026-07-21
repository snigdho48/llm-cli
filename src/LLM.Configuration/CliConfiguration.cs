namespace LLM.Configuration;
public sealed class CliConfiguration
{
    public string RuntimeProvider {get;set;}="llama.cpp";
    public int Port {get;set;}=8080;
    public string ModelsDirectory {get;set;}="models";
}
