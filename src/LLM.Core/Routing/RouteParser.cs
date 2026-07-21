namespace LLM.Core.Routing;

public static class RouteParser
{
    public static IReadOnlyList<string> Parse(string[] args)
    {
        return args
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .ToList();
    }
}