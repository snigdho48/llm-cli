using LLM.CLI.Commands;
namespace LLM.CLI.Runtime;
public sealed class RuntimeCommand:ICommand
{
 public string Name=>"runtime";
 public Task ExecuteAsync(string[] args){Console.WriteLine("Runtime management is coming soon.");return Task.CompletedTask;}
}
