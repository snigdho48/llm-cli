namespace LLM.CLI.Commands;

public interface ICommand
{
    string Name { get; }
    Task ExecuteAsync(string[] args);
}
