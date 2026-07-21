namespace LLM.CLI.Commands;

public interface ICommand
{
    string Name { get; }

    Task<int> ExecuteAsync(string[] args);
}

public static class CommandResults
{
    public const int Success = 0;

    public const int Failure = 1;
}
