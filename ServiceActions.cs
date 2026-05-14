using System.Diagnostics;

namespace VRDeveloperUtility;

internal static class ServiceActions
{
    public static void RestartOvrService()
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "sc.exe",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            Arguments = "stop OVRService",
        });
        process?.WaitForExit(15000);

        WaitForServiceState("OVRService", "STOPPED", TimeSpan.FromSeconds(15));

        using var startProcess = Process.Start(new ProcessStartInfo
        {
            FileName = "sc.exe",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            Arguments = "start OVRService",
        });
        startProcess?.WaitForExit(15000);
    }

    private static void WaitForServiceState(string serviceName, string targetState, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (QueryServiceState(serviceName).Equals(targetState, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            Thread.Sleep(500);
        }
    }

    private static string QueryServiceState(string serviceName)
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "sc.exe",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            Arguments = $"query {serviceName}",
        });

        if (process is null)
        {
            return string.Empty;
        }

        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit(5000);

        foreach (var line in output.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("STATE", StringComparison.OrdinalIgnoreCase))
            {
                return trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? string.Empty;
            }
        }

        return string.Empty;
    }
}
