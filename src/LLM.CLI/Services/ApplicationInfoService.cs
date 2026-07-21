namespace LLM.CLI.Services;

public sealed class ApplicationInfoService
{
    public void Print()
    {
        Console.WriteLine();

        Console.WriteLine("==============================");
        Console.WriteLine(" LLM CLI");
        Console.WriteLine(" Version : 0.1.0");
        Console.WriteLine(" Runtime : .NET 8");
        Console.WriteLine("==============================");

        Console.WriteLine();
    }
}
