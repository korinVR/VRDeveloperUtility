using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace VRDeveloperUtility;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Contains("--restart-ovr-service", StringComparer.OrdinalIgnoreCase))
        {
            ServiceActions.RestartOvrService();
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

internal sealed class MainForm : Form
{
    private const string DefaultHorizonPackage = "com.oculus.xrstreamingclient";
    private const string DebugToolPath = @"C:\Program Files\Meta Horizon\Support\oculus-diagnostics\OculusDebugTool.exe";
    private const string ServiceName = "OVRService";

    private readonly TextBox adbPathText = new();
    private readonly TextBox packageText = new();
    private readonly TextBox logBox = new();
    private readonly Button refreshButton = new();
    private readonly Button connectButton = new();
    private readonly Button disconnectButton = new();
    private readonly Button rebootQuestButton = new();
    private readonly Button devicesButton = new();
    private readonly Button debugToolButton = new();
    private readonly Button launchPcAppButton = new();
    private readonly Button restartServiceButton = new();
    private readonly NotifyIcon notifyIcon = new();

    private string? adbPath;

    public MainForm()
    {
        Text = "VR Developer Utility";
        Width = 640;
        Height = 360;
        MinimumSize = new Size(560, 320);
        StartPosition = FormStartPosition.CenterScreen;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(12),
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(root);

        root.Controls.Add(new Label
        {
            Text = "ADB",
            AutoSize = true,
            Font = new Font(Font, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 4),
        });

        adbPathText.Dock = DockStyle.Top;
        adbPathText.ReadOnly = true;
        adbPathText.Margin = new Padding(0, 0, 0, 8);
        root.Controls.Add(adbPathText);

        packageText.Dock = DockStyle.Top;
        packageText.Text = DefaultHorizonPackage;
        packageText.Margin = new Padding(0, 0, 0, 8);
        packageText.PlaceholderText = "Android package name";
        root.Controls.Add(packageText);

        logBox.Dock = DockStyle.Fill;
        logBox.Multiline = true;
        logBox.ReadOnly = true;
        logBox.ScrollBars = ScrollBars.Both;
        logBox.WordWrap = false;
        logBox.Font = new Font("Consolas", 9);
        root.Controls.Add(logBox);

        var buttonRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            Height = 116,
            Margin = new Padding(0, 10, 0, 0),
        };
        buttonRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        buttonRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        root.Controls.Add(buttonRow);

        var mainButtons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Margin = new Padding(0),
        };
        buttonRow.Controls.Add(mainButtons, 0, 0);

        var pcAppGroup = new GroupBox
        {
            Text = "Meta Horizon Link",
            Width = 386,
            Height = 68,
            Padding = new Padding(8, 4, 8, 6),
            Margin = new Padding(0, 0, 8, 0),
        };
        mainButtons.Controls.Add(pcAppGroup);

        var pcAppButtons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0),
        };
        pcAppGroup.Controls.Add(pcAppButtons);

        launchPcAppButton.Text = "Launch";
        launchPcAppButton.Width = 78;
        launchPcAppButton.Height = 30;
        launchPcAppButton.Click += (_, _) => StartPcApp();
        pcAppButtons.Controls.Add(launchPcAppButton);

        restartServiceButton.Text = "Restart";
        restartServiceButton.Width = 78;
        restartServiceButton.Height = 30;
        restartServiceButton.Click += (_, _) => RestartServiceElevated();
        pcAppButtons.Controls.Add(restartServiceButton);

        connectButton.Text = "Connect";
        connectButton.Width = 78;
        connectButton.Height = 30;
        connectButton.Click += async (_, _) => await RunAdbCommandAsync(
            "Connect",
            "shell",
            "monkey",
            "-p",
            GetPackageName(),
            "-c",
            "android.intent.category.LAUNCHER",
            "1");
        pcAppButtons.Controls.Add(connectButton);

        disconnectButton.Text = "Disconnect";
        disconnectButton.Width = 88;
        disconnectButton.Height = 30;
        disconnectButton.Click += async (_, _) => await RunAdbCommandAsync(
            "Disconnect",
            "shell",
            "am",
            "force-stop",
            GetPackageName());
        pcAppButtons.Controls.Add(disconnectButton);

        var toolButtons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Margin = new Padding(8, 0, 0, 0),
        };
        buttonRow.Controls.Add(toolButtons, 1, 0);

        refreshButton.Text = "Refresh";
        refreshButton.Width = 78;
        refreshButton.Height = 30;
        refreshButton.Click += async (_, _) => await RefreshAdbAsync();
        mainButtons.Controls.Add(refreshButton);

        devicesButton.Text = "Devices";
        devicesButton.Width = 78;
        devicesButton.Height = 30;
        devicesButton.Click += async (_, _) => await RunAdbCommandAsync("Devices", "devices");
        mainButtons.Controls.Add(devicesButton);

        rebootQuestButton.Text = "Reboot Quest";
        rebootQuestButton.Width = 100;
        rebootQuestButton.Height = 30;
        rebootQuestButton.Click += async (_, _) => await RunAdbCommandAsync("Reboot Quest", "reboot");
        mainButtons.Controls.Add(rebootQuestButton);

        debugToolButton.Text = "Debug Tool";
        debugToolButton.Width = 92;
        debugToolButton.Height = 30;
        debugToolButton.Click += (_, _) => StartDebugTool();
        toolButtons.Controls.Add(debugToolButton);

        notifyIcon.Icon = SystemIcons.Application;
        notifyIcon.Text = "VR Developer Utility";
        notifyIcon.Visible = true;
        notifyIcon.DoubleClick += (_, _) => RestoreFromTray();
        notifyIcon.ContextMenuStrip = BuildTrayMenu();

        Resize += (_, _) =>
        {
            if (WindowState == FormWindowState.Minimized)
            {
                Hide();
            }
        };

        FormClosing += (_, _) => notifyIcon.Visible = false;
        Shown += async (_, _) => await RefreshAdbAsync();
    }

    private ContextMenuStrip BuildTrayMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Open", null, (_, _) => RestoreFromTray());
        menu.Items.Add("Meta Horizon Link Launch", null, (_, _) => StartPcApp());
        menu.Items.Add("Meta Horizon Link Restart", null, (_, _) => RestartServiceElevated());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Connect", null, async (_, _) => await RunAdbCommandAsync(
            "Connect",
            "shell",
            "monkey",
            "-p",
            GetPackageName(),
            "-c",
            "android.intent.category.LAUNCHER",
            "1"));
        menu.Items.Add("Disconnect", null, async (_, _) => await RunAdbCommandAsync(
            "Disconnect",
            "shell",
            "am",
            "force-stop",
            GetPackageName()));
        menu.Items.Add("Reboot Quest", null, async (_, _) => await RunAdbCommandAsync("Reboot Quest", "reboot"));
        menu.Items.Add("Debug Tool", null, (_, _) => StartDebugTool());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => Close());
        return menu;
    }

    private void RestoreFromTray()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    private string GetPackageName()
    {
        var value = packageText.Text.Trim();
        return string.IsNullOrWhiteSpace(value) ? DefaultHorizonPackage : value;
    }

    private async Task RefreshAdbAsync()
    {
        refreshButton.Enabled = false;
        adbPathText.Text = "Detecting adb...";

        try
        {
            adbPath = await Task.Run(() => FindAdbCandidates().FirstOrDefault());
            adbPathText.Text = adbPath ?? "adb not found";
            Log(adbPath is null ? "No adb server or adb.exe found." : $"Using adb: {adbPath}");
        }
        catch (Exception ex)
        {
            adbPath = null;
            adbPathText.Text = "adb detection failed";
            Log($"ADB detection failed: {ex.Message}");
        }
        finally
        {
            refreshButton.Enabled = true;
        }
    }

    private async Task RunAdbCommandAsync(string label, params string[] arguments)
    {
        SetButtonsEnabled(false);
        try
        {
            if (string.IsNullOrWhiteSpace(adbPath) || !File.Exists(adbPath))
            {
                await RefreshAdbAsync();
            }

            if (string.IsNullOrWhiteSpace(adbPath) || !File.Exists(adbPath))
            {
                Log("adb was not found. Start Unity Android Logcat or install Android SDK platform-tools.");
                return;
            }

            Log($"> {label}: adb {string.Join(' ', arguments)}");

            var result = await RunProcessAsync(adbPath, arguments);
            foreach (var line in SplitLines(result.Output))
            {
                Log(line);
            }

            if (result.ExitCode != 0)
            {
                Log($"adb exited with code {result.ExitCode}");
            }
        }
        catch (Exception ex)
        {
            Log($"{label} failed: {ex.Message}");
        }
        finally
        {
            SetButtonsEnabled(true);
        }
    }

    private void SetButtonsEnabled(bool enabled)
    {
        refreshButton.Enabled = enabled;
        connectButton.Enabled = enabled;
        disconnectButton.Enabled = enabled;
        rebootQuestButton.Enabled = enabled;
        devicesButton.Enabled = enabled;
        debugToolButton.Enabled = enabled;
        launchPcAppButton.Enabled = enabled;
        restartServiceButton.Enabled = enabled;
    }

    private void StartDebugTool()
    {
        try
        {
            if (!File.Exists(DebugToolPath))
            {
                Log($"Debug Tool not found: {DebugToolPath}");
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = DebugToolPath,
                UseShellExecute = true,
            });
            Log($"Started Debug Tool: {DebugToolPath}");
        }
        catch (Exception ex)
        {
            Log($"Debug Tool failed: {ex.Message}");
        }
    }

    private void StartPcApp()
    {
        try
        {
            var appPath = FindPcClientPath();
            if (appPath is null)
            {
                Log("PC app not found.");
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = appPath,
                UseShellExecute = true,
            });
            Log($"Started PC app: {appPath}");
        }
        catch (Exception ex)
        {
            Log($"PC app start failed: {ex.Message}");
        }
    }

    private void RestartServiceElevated()
    {
        try
        {
            var command = $"Restart-Service -Name '{ServiceName}' -Force";
            var executablePath = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(executablePath))
            {
                Log("Service restart failed: current executable path was not available.");
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = executablePath,
                Arguments = "--restart-ovr-service",
                UseShellExecute = true,
                Verb = "runas",
                WindowStyle = ProcessWindowStyle.Hidden,
            });
            Log($"Requested elevated service restart: {ServiceName}");
        }
        catch (Exception ex)
        {
            Log($"Service restart failed: {ex.Message}");
        }
    }

    private void Log(string message)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => Log(message));
            return;
        }

        logBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        logBox.SelectionStart = logBox.TextLength;
        logBox.ScrollToCaret();
    }

    private static IEnumerable<string> SplitLines(string value)
    {
        return value
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);
    }

    private static async Task<(int ExitCode, string Output)> RunProcessAsync(string fileName, params string[] arguments)
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
        await process.WaitForExitAsync();

        return (process.ExitCode, string.Concat(await stdout, await stderr));
    }

    private static IEnumerable<string> FindAdbCandidates()
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

    private static string? FindPcClientPath()
    {
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var candidates = new[]
        {
            @"C:\Program Files\Meta Horizon\Support\oculus-client\OculusClient.exe",
            @"C:\Program Files\Oculus\Support\oculus-client\OculusClient.exe",
            Path.Combine(programFiles, "Meta Horizon", "Support", "oculus-client", "OculusClient.exe"),
            Path.Combine(programFiles, "Oculus", "Support", "oculus-client", "OculusClient.exe"),
        };

        return candidates.FirstOrDefault(File.Exists);
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

internal static class TcpTable
{
    private const int AfInet = 2;
    private const int TcpTableOwnerPidListener = 3;

    public static int? TryFindListeningProcessId(int localPort)
    {
        var bufferSize = 0;
        var result = GetExtendedTcpTable(
            IntPtr.Zero,
            ref bufferSize,
            true,
            AfInet,
            TcpTableOwnerPidListener,
            0);

        var buffer = Marshal.AllocHGlobal(bufferSize);
        try
        {
            result = GetExtendedTcpTable(
                buffer,
                ref bufferSize,
                true,
                AfInet,
                TcpTableOwnerPidListener,
                0);

            if (result != 0)
            {
                return null;
            }

            var rowCount = Marshal.ReadInt32(buffer);
            var rowPtr = IntPtr.Add(buffer, 4);
            var rowSize = Marshal.SizeOf<MibTcpRowOwnerPid>();

            for (var i = 0; i < rowCount; i++)
            {
                var row = Marshal.PtrToStructure<MibTcpRowOwnerPid>(IntPtr.Add(rowPtr, i * rowSize));
                var port = (ushort)IPAddress.NetworkToHostOrder((short)row.LocalPort);

                if (port == localPort && row.State == 2)
                {
                    return (int)row.OwningPid;
                }
            }

            return null;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern int GetExtendedTcpTable(
        IntPtr tcpTable,
        ref int tcpTableLength,
        bool sort,
        int ipVersion,
        int tableClass,
        int reserved);

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct MibTcpRowOwnerPid
    {
        public readonly uint State;
        public readonly uint LocalAddr;
        public readonly uint LocalPort;
        public readonly uint RemoteAddr;
        public readonly uint RemotePort;
        public readonly uint OwningPid;
    }
}
