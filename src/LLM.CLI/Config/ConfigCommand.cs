using LLM.CLI.Commands;
namespace LLM.CLI.Config;
public sealed class ConfigCommand:ICommand
{
 public string Name=>"config";
 public Task ExecuteAsync(string[] args){Console.WriteLine("Configuration management is coming soon.");return Task.CompletedTask;}
}
