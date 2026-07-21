using System.Diagnostics;
using System.Text;

namespace LLM.CLI.Services;

public sealed class WingetService
{
    public bool IsAvailable()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "winget.exe",
                Arguments = "--version",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            using var process = Process.Start(psi);
            if (process is null)
            {
                return false;
            }

            process.WaitForExit(5000);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public async Task<WingetInstallResult> InstallAsync(
        string packageId,
        CancellationToken cancellationToken = default)
    {
        if (!IsAvailable())
        {
            return new WingetInstallResult(false, "winget.exe not found");
        }

        var arguments =
            $"install -e --id {packageId} --accept-package-agreements --accept-source-agreements --disable-interactivity";

        var psi = new ProcessStartInfo
        {
            FileName = "winget.exe",
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start winget.exe");

        var output = new StringBuilder();
        process.OutputDataReceived += (_, args) =>
        {
            if (args.Data is not null)
            {
                output.AppendLine(args.Data);
            }
        };
        process.ErrorDataReceived += (_, args) =>
        {
            if (args.Data is not null)
            {
                output.AppendLine(args.Data);
            }
        };

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken);

        // winget returns 0 on success; -1978335189 (0x8A15002B) often means already installed
        var text = output.ToString();
        var alreadyInstalled =
            text.Contains("already installed", StringComparison.OrdinalIgnoreCase)
            || process.ExitCode == unchecked((int)0x8A15002B);

        var success = process.ExitCode == 0 || alreadyInstalled;
        var detail = success
            ? (alreadyInstalled ? "Already installed" : "Installed successfully")
            : $"exit {process.ExitCode}: {Truncate(text, 400)}";

        return new WingetInstallResult(success, detail);
    }

    private static string Truncate(string value, int max)
    {
        var trimmed = value.Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max] + "...";
    }
}

public readonly record struct WingetInstallResult(bool Success, string Detail);
