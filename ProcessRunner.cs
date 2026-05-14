using System.Diagnostics;
using System.Text;

namespace VRDeveloperUtility;

internal static class ProcessRunner
{
    public static async Task<(int ExitCode, string Output)> RunAsync(string fileName, params string[] arguments)
    {
        return await RunAsync(fileName, TimeSpan.FromSeconds(30), arguments);
    }

    public static async Task<(int ExitCode, string Output)> RunAsync(
        string fileName,
        TimeSpan timeout,
        params string[] arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start process.");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        var exited = await Task.Run(() => process.WaitForExit((int)timeout.TotalMilliseconds));
        if (!exited)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch
            {
                // Best effort cleanup after a timed-out tool process.
            }

            return (-1, $"Process timed out after {timeout.TotalSeconds:0} seconds.");
        }

        return (process.ExitCode, string.Concat(await stdout, await stderr));
    }
}
