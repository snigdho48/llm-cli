namespace LLM.Diagnostics;
public interface IDiagnosticCheck{
 string Name{get;}
 Task<bool> ExecuteAsync();
}
