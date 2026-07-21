using System.Reflection;

namespace LLM.CLI.Commands;

/// <summary>
/// Copies this executable into %LOCALAPPDATA%\LLM\bin and adds that folder to the user PATH.
/// Self-contained single-file builds install as llm.exe; framework builds copy the full output folder.
/// </summary>
public sealed class InstallCommand : ICommand
{
    public string Name => "install";

    public Task<int> ExecuteAsync(string[] args)
    {
        var source = ResolveCurrentExe();
        if (string.IsNullOrWhiteSpace(source) || !File.Exists(source))
        {
            Console.WriteLine("Could not locate this llm.exe to install.");
            return Task.FromResult(CommandResults.Failure);
        }

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var installRoot = Path.Combine(localAppData, "LLM", "cli");
        var binDirectory = Path.Combine(localAppData, "LLM", "bin");
        var binTarget = Path.Combine(binDirectory, "llm.exe");

        Directory.CreateDirectory(installRoot);
        Directory.CreateDirectory(binDirectory);

        try
        {
            var sourceDir = Path.GetDirectoryName(source)!;
            var frameworkDll = Path.Combine(sourceDir, "LLM.CLI.dll");
            var isFrameworkDependent = File.Exists(frameworkDll);

            if (isFrameworkDependent)
            {
                // Dev / non-single-file: copy whole output so deps resolve.
                foreach (var file in Directory.EnumerateFiles(sourceDir))
                {
                    var name = Path.GetFileName(file);
                    File.Copy(file, Path.Combine(installRoot, name), overwrite: true);
                }

                var installedExe = Path.Combine(installRoot, Path.GetFileName(source));
                var cliLlm = Path.Combine(installRoot, "llm.exe");
                if (!string.Equals(installedExe, cliLlm, StringComparison.OrdinalIgnoreCase))
                {
                    File.Copy(installedExe, cliLlm, overwrite: true);
                }

                // PATH entry launches via shim into the full install folder.
                File.WriteAllText(
                    Path.Combine(binDirectory, "llm.cmd"),
                    $"@echo off\r\nsetlocal\r\n\"{cliLlm}\" %*\r\n");

                if (File.Exists(binTarget))
                {
                    try { File.Delete(binTarget); } catch { /* ignore locked */ }
                }
            }
            else
            {
                var cliTarget = Path.Combine(installRoot, "llm.exe");
                File.Copy(source, cliTarget, overwrite: true);
                File.Copy(source, binTarget, overwrite: true);
                File.WriteAllText(
                    Path.Combine(binDirectory, "llm.cmd"),
                    "@echo off\r\nsetlocal\r\n\"%~dp0llm.exe\" %*\r\n");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Install failed while copying llm.exe: {ex.Message}");
            return Task.FromResult(CommandResults.Failure);
        }

        var pathAdded = EnsureUserPathContains(binDirectory);

        Console.WriteLine("Installed successfully.");
        Console.WriteLine($"  Binary : {binTarget}");
        if (File.Exists(Path.Combine(installRoot, "llm.exe")))
        {
            Console.WriteLine($"  Also   : {Path.Combine(installRoot, "llm.exe")}");
        }

        Console.WriteLine(pathAdded
            ? $"  PATH   : added {binDirectory}"
            : $"  PATH   : already contains {binDirectory}");

        Console.WriteLine();
        Console.WriteLine("Open a new terminal, then run:");
        Console.WriteLine("  llm version");
        Console.WriteLine("  llm init \"D:\\AI\" --auto");
        Console.WriteLine("  llm serve");

        return Task.FromResult(CommandResults.Success);
    }

    private static string? ResolveCurrentExe()
    {
        var processPath = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(processPath) && File.Exists(processPath))
        {
            return processPath;
        }

        var location = Assembly.GetExecutingAssembly().Location;
        if (!string.IsNullOrWhiteSpace(location) && File.Exists(location))
        {
            return location;
        }

        return null;
    }

    private static bool EnsureUserPathContains(string directory)
    {
        var userPath = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.User) ?? "";
        var segments = userPath.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Any(s => string.Equals(s, directory, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        var updated = string.IsNullOrWhiteSpace(userPath)
            ? directory
            : $"{userPath.TrimEnd(';')};{directory}";

        Environment.SetEnvironmentVariable("Path", updated, EnvironmentVariableTarget.User);

        var processPath = Environment.GetEnvironmentVariable("Path") ?? "";
        if (!processPath.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Any(s => string.Equals(s, directory, StringComparison.OrdinalIgnoreCase)))
        {
            Environment.SetEnvironmentVariable("Path", $"{processPath.TrimEnd(';')};{directory}");
        }

        return true;
    }
}
