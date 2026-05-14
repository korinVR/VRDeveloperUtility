using System.Diagnostics;
using System.Text.RegularExpressions;

namespace VRDeveloperUtility;

internal static class AdbTools
{
    public static readonly string ScreenshotDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
        "VRDeveloperUtility");

    private const string QuestScreenshotDirectory = "/sdcard/Oculus/Screenshots";
    public static IEnumerable<string> FindAdbCandidates()
    {
        var candidates = new List<string>();

        var runningAdb = FindRunningAdbServerPath();
        if (!string.IsNullOrWhiteSpace(runningAdb))
        {
            candidates.Add(runningAdb);
        }

        candidates.AddRange(FindOnPath("adb.exe"));

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

        candidates.Add(Path.Combine(localAppData, "Android", "Sdk", "platform-tools", "adb.exe"));
        candidates.Add(Path.Combine(programFilesX86, "Android", "android-sdk", "platform-tools", "adb.exe"));

        var unityHubEditors = Path.Combine(programFiles, "Unity", "Hub", "Editor");
        if (Directory.Exists(unityHubEditors))
        {
            candidates.AddRange(Directory.EnumerateDirectories(unityHubEditors)
                .Select(editorPath => Path.Combine(
                    editorPath,
                    "Editor",
                    "Data",
                    "PlaybackEngines",
                    "AndroidPlayer",
                    "SDK",
                    "platform-tools",
                    "adb.exe")));
        }

        return candidates
            .Where(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path))
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    public static async Task<AdbConnectionStatus> QueryConnectionStatusAsync(string adbPath)
    {
        var server = TcpTable.TryFindListeningProcessId(5037) is null ? "Not running" : "Running";
        var devices = await ProcessRunner.RunAsync(adbPath, TimeSpan.FromSeconds(5), "devices", "-l");
        if (devices.ExitCode != 0)
        {
            return new AdbConnectionStatus(server, "Query failed", "-", "-", "-");
        }

        var device = ParseFirstDevice(devices.Output);
        if (device is null)
        {
            return new AdbConnectionStatus(server, "No device", "-", "-", "-");
        }

        if (!device.State.Equals("device", StringComparison.OrdinalIgnoreCase))
        {
            return new AdbConnectionStatus(server, $"{device.Serial} ({device.State})", device.Model ?? "-", "-", "-");
        }

        var model = await QueryShellValueAsync(adbPath, "getprop", "ro.product.model");
        if (string.IsNullOrWhiteSpace(model))
        {
            model = device.Model;
        }

        var androidVersion = await QueryShellValueAsync(adbPath, "getprop", "ro.build.version.release");
        var ip = await QueryDeviceIpAsync(adbPath);
        var battery = await QueryBatteryAsync(adbPath);
        var modelText = string.IsNullOrWhiteSpace(androidVersion)
            ? model ?? "-"
            : $"{model ?? "-"} / Android {androidVersion}";

        return new AdbConnectionStatus(server, $"{device.Serial} (connected)", modelText, ip, battery);
    }

    public static async Task<string> CaptureScreenshotAsync(string adbPath)
    {
        Directory.CreateDirectory(ScreenshotDirectory);
        var previousLatest = await TryGetLatestDeviceFileAsync(adbPath, QuestScreenshotDirectory);

        var capture = await ProcessRunner.RunAsync(
            adbPath,
            TimeSpan.FromSeconds(15),
            "shell",
            "am",
            "startservice",
            "-n",
            "com.oculus.metacam/.capture.CaptureService",
            "-a",
            "TAKE_SCREENSHOT");
        if (capture.ExitCode != 0)
        {
            throw new InvalidOperationException(capture.Output.Trim());
        }

        var devicePath = await WaitForNewDeviceFileAsync(adbPath, QuestScreenshotDirectory, previousLatest, TimeSpan.FromSeconds(8));
        return await PullDeviceMediaAsync(adbPath, devicePath, "quest-screenshot");
    }

    private static async Task<string> PullDeviceMediaAsync(string adbPath, string devicePath, string localPrefix)
    {
        Directory.CreateDirectory(ScreenshotDirectory);
        var extension = Path.GetExtension(devicePath);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".dat";
        }

        var outputPath = Path.Combine(ScreenshotDirectory, $"{localPrefix}-{DateTime.Now:yyyyMMdd-HHmmss}{extension}");
        var pull = await ProcessRunner.RunAsync(
            adbPath,
            TimeSpan.FromMinutes(2),
            "pull",
            devicePath,
            outputPath);
        if (pull.ExitCode != 0)
        {
            throw new InvalidOperationException(pull.Output.Trim());
        }

        return outputPath;
    }

    private static async Task<string> WaitForNewDeviceFileAsync(
        string adbPath,
        string deviceDirectory,
        string? previousLatest,
        TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            var latest = await TryGetLatestDeviceFileAsync(adbPath, deviceDirectory);
            if (!string.IsNullOrWhiteSpace(latest) && !latest.Equals(previousLatest, StringComparison.Ordinal))
            {
                return latest;
            }

            await Task.Delay(500);
        }

        throw new TimeoutException($"No new media file appeared in {deviceDirectory}.");
    }

    private static async Task<string?> TryGetLatestDeviceFileAsync(string adbPath, string deviceDirectory)
    {
        var result = await ProcessRunner.RunAsync(
            adbPath,
            TimeSpan.FromSeconds(5),
            "shell",
            "ls",
            "-t",
            deviceDirectory);
        if (result.ExitCode != 0)
        {
            return null;
        }

        var fileName = TextOutput.SplitLines(result.Output)
            .Select(line => line.Trim())
            .FirstOrDefault(line => !string.IsNullOrWhiteSpace(line));

        return string.IsNullOrWhiteSpace(fileName) ? null : $"{deviceDirectory}/{fileName}";
    }

    private static async Task<string?> QueryShellValueAsync(string adbPath, params string[] shellArguments)
    {
        var arguments = new[] { "shell" }.Concat(shellArguments).ToArray();
        var result = await ProcessRunner.RunAsync(adbPath, TimeSpan.FromSeconds(5), arguments);
        return result.ExitCode == 0 ? result.Output.Trim() : null;
    }

    private static async Task<string> QueryDeviceIpAsync(string adbPath)
    {
        var output = await QueryShellValueAsync(adbPath, "ip", "-f", "inet", "addr", "show", "wlan0");
        if (string.IsNullOrWhiteSpace(output))
        {
            return "-";
        }

        var match = Regex.Match(output, @"\binet\s+(?<ip>\d+\.\d+\.\d+\.\d+)/");
        return match.Success ? match.Groups["ip"].Value : "-";
    }

    private static async Task<string> QueryBatteryAsync(string adbPath)
    {
        var output = await QueryShellValueAsync(adbPath, "dumpsys", "battery");
        if (string.IsNullOrWhiteSpace(output))
        {
            return "-";
        }

        var level = ParseDumpsysValue(output, "level");
        var status = ParseDumpsysValue(output, "status");
        var statusText = status switch
        {
            "2" => "charging",
            "3" => "discharging",
            "4" => "not charging",
            "5" => "full",
            _ => null,
        };

        if (string.IsNullOrWhiteSpace(level))
        {
            return statusText ?? "-";
        }

        return string.IsNullOrWhiteSpace(statusText) ? $"{level}%" : $"{level}% ({statusText})";
    }

    private static string? ParseDumpsysValue(string output, string key)
    {
        foreach (var line in TextOutput.SplitLines(output))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith($"{key}:", StringComparison.OrdinalIgnoreCase))
            {
                return trimmed[(key.Length + 1)..].Trim();
            }
        }

        return null;
    }

    private static AdbDevice? ParseFirstDevice(string output)
    {
        foreach (var line in TextOutput.SplitLines(output).Skip(1))
        {
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                continue;
            }

            var model = parts
                .FirstOrDefault(part => part.StartsWith("model:", StringComparison.OrdinalIgnoreCase))?
                .Split(':', 2)[1]
                .Replace('_', ' ');

            return new AdbDevice(parts[0], parts[1], model);
        }

        return null;
    }

    private static IEnumerable<string> FindOnPath(string fileName)
    {
        var pathValue = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathValue))
        {
            return Array.Empty<string>();
        }

        return pathValue
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(path => Path.Combine(path, fileName))
            .Where(File.Exists);
    }

    private static string? FindRunningAdbServerPath()
    {
        var pid = TcpTable.TryFindListeningProcessId(5037);
        if (pid is null)
        {
            return null;
        }

        try
        {
            using var process = Process.GetProcessById(pid.Value);
            return process.MainModule?.FileName;
        }
        catch
        {
            return null;
        }
    }
}

internal sealed record AdbConnectionStatus(string Server, string Device, string Model, string Ip, string Battery);

internal sealed record AdbDevice(string Serial, string State, string? Model);
