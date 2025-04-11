using System.Diagnostics;

namespace SimpleDeploy.Api.Jobs.Utils;

public static class ShellHelper
{
    public static async Task<(int ExitCode, string Output)> RunShell(string cmd)
    {
        var psi = new ProcessStartInfo("/bin/bash", $"-c \"{cmd}\"")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = Process.Start(psi)!;
        var output = await process.StandardOutput.ReadToEndAsync();
        output += await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        return (process.ExitCode, output);
    }
}