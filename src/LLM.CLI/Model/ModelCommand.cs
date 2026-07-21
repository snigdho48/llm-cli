using LLM.CLI.Commands;
namespace LLM.CLI.Model;
public sealed class ModelCommand:ICommand
{
 public string Name=>"model";
 public Task ExecuteAsync(string[] args){Console.WriteLine("Model management is coming soon.");return Task.CompletedTask;}
}
