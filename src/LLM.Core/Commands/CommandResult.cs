namespace LLM.Core.Commands;

public sealed class CommandResult
{
    private CommandResult(bool success, int exitCode, string? message = null)
    {
        Success = success;
        ExitCode = exitCode;
        Message = message;
    }

    public bool Success { get; }

    public int ExitCode { get; }

    public string? Message { get; }

    public static CommandResult Ok(string? message = null)
        => new(true, 0, message);

    public static CommandResult Fail(string message, int exitCode = 1)
        => new(false, exitCode, message);
}