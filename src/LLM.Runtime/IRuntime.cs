namespace LLM.Runtime;
public interface IRuntime{
 string Name{get;}
 Task StartAsync();
 Task StopAsync();
 Task<string> GetStatusAsync();
}
