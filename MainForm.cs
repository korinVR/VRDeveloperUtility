using System.Diagnostics;

namespace VRDeveloperUtility;

internal sealed class MainForm : Form
{
    private const string DefaultHorizonPackage = "com.oculus.xrstreamingclient";
    private const string DebugToolPath = @"C:\Program Files\Meta Horizon\Support\oculus-diagnostics\OculusDebugTool.exe";
    private const string ServiceName = "OVRService";

    private readonly TextBox adbPathText = new();
    private readonly TextBox logBox = new();
    private readonly Label adbServerStatusLabel = new();
    private readonly Label deviceStatusLabel = new();
    private readonly Label deviceModelLabel = new();
    private readonly Label deviceIpLabel = new();
    private readonly Label deviceBatteryLabel = new();
    private readonly Button refreshButton = new();
    private readonly Button connectButton = new();
    private readonly Button disconnectButton = new();
    private readonly Button rebootQuestButton = new();
    private readonly Button screenshotButton = new();
    private readonly Button devicesButton = new();
    private readonly Button debugToolButton = new();
    private readonly Button launchPcAppButton = new();
    private readonly Button restartServiceButton = new();
    private readonly NotifyIcon notifyIcon = new();

    private string? adbPath;

    public MainForm()
    {
        Text = "VR Developer Utility";
        Width = 860;
        Height = 560;
        MinimumSize = new Size(720, 460);
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

        root.Controls.Add(BuildAdbStatusPanel());

        logBox.Dock = DockStyle.Fill;
        logBox.Multiline = true;
        logBox.ReadOnly = true;
        logBox.ScrollBars = ScrollBars.Both;
        logBox.WordWrap = false;
        logBox.BackColor = Color.White;
        logBox.ForeColor = Color.Black;
        logBox.Font = new Font("Consolas", 9);
        root.Controls.Add(logBox);

        root.Controls.Add(BuildButtonRow());

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

    private Control BuildAdbStatusPanel()
    {
        var group = new GroupBox
        {
            Text = "ADB Connection",
            Dock = DockStyle.Top,
            Height = 96,
            Padding = new Padding(8, 4, 8, 8),
            Margin = new Padding(0, 0, 0, 8),
        };

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 3,
            Margin = new Padding(0),
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        group.Controls.Add(grid);

        AddStatusRow(grid, 0, "Server", adbServerStatusLabel, "Device", deviceStatusLabel);
        AddStatusRow(grid, 1, "Model", deviceModelLabel, "IP", deviceIpLabel);
        AddStatusRow(grid, 2, "Battery", deviceBatteryLabel, string.Empty, new Label());

        SetAdbStatusText("Unknown", "Unknown", "-", "-", "-");
        return group;
    }

    private Control BuildButtonRow()
    {
        var buttonRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            Height = 116,
            Margin = new Padding(0, 10, 0, 0),
        };
        buttonRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        buttonRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

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
            Width = 430,
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

        AddButton(pcAppButtons, launchPcAppButton, "Launch", 78, (_, _) => StartPcApp());
        AddButton(pcAppButtons, restartServiceButton, "Restart", 78, (_, _) => RestartServiceElevated());
        AddButton(pcAppButtons, connectButton, "Connect", 78, async (_, _) => await RunAdbCommandAsync(
            "Connect",
            "shell",
            "monkey",
            "-p",
            GetPackageName(),
            "-c",
            "android.intent.category.LAUNCHER",
            "1"));
        AddButton(pcAppButtons, disconnectButton, "Disconnect", 88, async (_, _) => await RunAdbCommandAsync(
            "Disconnect",
            "shell",
            "am",
            "force-stop",
            GetPackageName()));

        var toolButtons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Margin = new Padding(8, 0, 0, 0),
        };
        buttonRow.Controls.Add(toolButtons, 1, 0);

        AddButton(mainButtons, refreshButton, "Refresh", 78, async (_, _) => await RefreshAdbAsync());
        AddButton(mainButtons, devicesButton, "Devices", 78, async (_, _) => await RunAdbCommandAsync("Devices", "devices"));
        AddButton(mainButtons, screenshotButton, "Screenshot", 92, async (_, _) => await CaptureScreenshotAsync());
        AddButton(mainButtons, rebootQuestButton, "Reboot Meta Quest", 128, async (_, _) => await RunAdbCommandAsync("Reboot Meta Quest", "reboot"));
        AddButton(toolButtons, debugToolButton, "Oculus Debug Tool", 132, (_, _) => StartDebugTool());

        return buttonRow;
    }

    private static void AddButton(Control parent, Button button, string text, int width, EventHandler click)
    {
        button.Text = text;
        button.Width = width;
        button.Height = 30;
        button.Click += click;
        parent.Controls.Add(button);
    }

    private static void AddStatusRow(
        TableLayoutPanel grid,
        int row,
        string leftTitle,
        Label leftValue,
        string rightTitle,
        Label rightValue)
    {
        grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        AddStatusCell(grid, leftTitle, row, 0, FontStyle.Bold);
        AddStatusCell(grid, leftValue, row, 1);

        if (!string.IsNullOrWhiteSpace(rightTitle))
        {
            AddStatusCell(grid, rightTitle, row, 2, FontStyle.Bold);
            AddStatusCell(grid, rightValue, row, 3);
        }
    }

    private static void AddStatusCell(
        TableLayoutPanel grid,
        string text,
        int row,
        int column,
        FontStyle fontStyle = FontStyle.Regular)
    {
        AddStatusCell(grid, new Label { Text = text, Font = new Font(grid.Font, fontStyle) }, row, column);
    }

    private static void AddStatusCell(TableLayoutPanel grid, Label label, int row, int column)
    {
        label.AutoSize = true;
        label.Dock = DockStyle.Fill;
        label.Margin = new Padding(column % 2 == 0 ? 0 : 6, 3, column == 1 ? 16 : 0, 3);
        grid.Controls.Add(label, column, row);
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
        menu.Items.Add("Screenshot", null, async (_, _) => await CaptureScreenshotAsync());
        menu.Items.Add("Reboot Meta Quest", null, async (_, _) => await RunAdbCommandAsync("Reboot Meta Quest", "reboot"));
        menu.Items.Add("Oculus Debug Tool", null, (_, _) => StartDebugTool());
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
        return DefaultHorizonPackage;
    }

    private async Task RefreshAdbAsync()
    {
        refreshButton.Enabled = false;
        adbPathText.Text = "Detecting adb...";

        try
        {
            adbPath = await Task.Run(() => AdbTools.FindAdbCandidates().FirstOrDefault());
            adbPathText.Text = adbPath ?? "adb not found";
            Log(adbPath is null ? "No adb server or adb.exe found." : $"Using adb: {adbPath}");
            await RefreshAdbStatusAsync();
        }
        catch (Exception ex)
        {
            adbPath = null;
            adbPathText.Text = "adb detection failed";
            SetAdbStatusText("Detection failed", "Unknown", "-", "-", "-");
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
            if (!await EnsureAdbReadyAsync())
            {
                return;
            }

            Log($"> {label}: adb {string.Join(' ', arguments)}");

            var result = await ProcessRunner.RunAsync(adbPath!, arguments);
            foreach (var line in TextOutput.SplitLines(result.Output))
            {
                Log(line);
            }

            if (result.ExitCode != 0)
            {
                Log($"adb exited with code {result.ExitCode}");
            }

            await RefreshAdbStatusAsync();
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
        screenshotButton.Enabled = enabled;
        devicesButton.Enabled = enabled;
        debugToolButton.Enabled = enabled;
        launchPcAppButton.Enabled = enabled;
        restartServiceButton.Enabled = enabled;
    }

    private async Task RefreshAdbStatusAsync()
    {
        if (string.IsNullOrWhiteSpace(adbPath) || !File.Exists(adbPath))
        {
            SetAdbStatusText("adb not found", "No device", "-", "-", "-");
            return;
        }

        try
        {
            SetAdbStatusText(TcpTable.TryFindListeningProcessId(5037) is null ? "Starting" : "Running", "Checking...", "-", "-", "-");
            var status = await AdbTools.QueryConnectionStatusAsync(adbPath);
            SetAdbStatusText(status.Server, status.Device, status.Model, status.Ip, status.Battery);
        }
        catch (Exception ex)
        {
            SetAdbStatusText("Query failed", "Unknown", "-", "-", "-");
            Log($"ADB status refresh failed: {ex.Message}");
        }
    }

    private async Task CaptureScreenshotAsync()
    {
        SetButtonsEnabled(false);
        try
        {
            if (!await EnsureAdbReadyAsync())
            {
                return;
            }

            Log("> Screenshot: native Quest capture");
            var path = await AdbTools.CaptureScreenshotAsync(adbPath!);
            Log($"Screenshot saved: {path}");
            await RefreshAdbStatusAsync();
        }
        catch (Exception ex)
        {
            Log($"Screenshot failed: {ex.Message}");
        }
        finally
        {
            SetButtonsEnabled(true);
        }
    }

    private async Task<bool> EnsureAdbReadyAsync()
    {
        if (string.IsNullOrWhiteSpace(adbPath) || !File.Exists(adbPath))
        {
            await RefreshAdbAsync();
        }

        if (string.IsNullOrWhiteSpace(adbPath) || !File.Exists(adbPath))
        {
            Log("adb was not found. Start Unity Android Logcat or install Android SDK platform-tools.");
            return false;
        }

        if (!await AdbTools.EnsureServerRunningAsync(adbPath))
        {
            Log("ADB server failed to start.");
            return false;
        }

        return true;
    }

    private void SetAdbStatusText(string server, string device, string model, string ip, string battery)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => SetAdbStatusText(server, device, model, ip, battery));
            return;
        }

        adbServerStatusLabel.Text = server;
        deviceStatusLabel.Text = device;
        deviceModelLabel.Text = model;
        deviceIpLabel.Text = ip;
        deviceBatteryLabel.Text = battery;
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
            var appPath = PcAppTools.FindClientPath();
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
}
