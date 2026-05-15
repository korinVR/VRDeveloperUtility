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
    private readonly Label deviceSsidLabel = new();
    private readonly BatteryBarPanel deviceBatteryBar = new();
    private readonly Button logToggleButton = new();
    private readonly Button refreshButton = new();
    private readonly Button connectButton = new();
    private readonly Button disconnectButton = new();
    private readonly Button rebootQuestButton = new();
    private readonly Button screenshotButton = new();
    private readonly Button debugToolButton = new();
    private readonly Button launchPcAppButton = new();
    private readonly Button restartServiceButton = new();
    private readonly ComboBox unityProcessCombo = new();
    private readonly Button refreshUnityButton = new();
    private readonly Button killUnityButton = new();
    private readonly System.Windows.Forms.Timer adbRefreshTimer = new();
    private readonly NotifyIcon notifyIcon = new();

    private string? adbPath;
    private int? batteryLevel;
    private RowStyle? logRowStyle;
    private bool isRefreshingAdbStatus;

    public MainForm()
    {
        Text = "VR Developer Utility";
        Width = 860;
        Height = 660;
        MinimumSize = new Size(760, 600);
        StartPosition = FormStartPosition.CenterScreen;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            Padding = new Padding(12),
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 124));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        logRowStyle = root.RowStyles[3];
        Controls.Add(root);

        root.Controls.Add(new Label
        {
            Text = "ADB",
            AutoSize = true,
            Font = new Font(Font, FontStyle.Bold),
            Margin = new Padding(0, 0, 0, 4),
        }, 0, 0);

        adbPathText.Dock = DockStyle.Top;
        adbPathText.ReadOnly = true;
        adbPathText.Margin = new Padding(0, 0, 0, 8);
        root.Controls.Add(adbPathText, 0, 1);

        root.Controls.Add(BuildAdbStatusPanel(), 0, 2);

        logBox.Dock = DockStyle.Fill;
        logBox.Multiline = true;
        logBox.ReadOnly = true;
        logBox.ScrollBars = ScrollBars.Both;
        logBox.WordWrap = false;
        logBox.BackColor = Color.White;
        logBox.ForeColor = Color.Black;
        logBox.Font = new Font("Consolas", 9);
        logBox.Visible = false;
        root.Controls.Add(logBox, 0, 3);
        SetLogVisible(false);

        root.Controls.Add(BuildButtonRow(), 0, 4);

        root.Controls.Add(BuildUnityPanel(), 0, 5);

        notifyIcon.Icon = SystemIcons.Application;
        notifyIcon.Text = "VR Developer Utility";
        notifyIcon.Visible = true;
        notifyIcon.DoubleClick += (_, _) => RestoreFromTray();
        notifyIcon.ContextMenuStrip = BuildTrayMenu();

        adbRefreshTimer.Interval = 5000;
        adbRefreshTimer.Tick += async (_, _) => await RefreshAdbStatusAsync();
        adbRefreshTimer.Start();

        Resize += (_, _) =>
        {
            if (WindowState == FormWindowState.Minimized)
            {
                Hide();
            }
        };

        FormClosing += (_, _) =>
        {
            adbRefreshTimer.Stop();
            notifyIcon.Visible = false;
        };
        Shown += async (_, _) =>
        {
            await RefreshAdbAsync();
            RefreshUnityProcesses();
        };
    }

    private Control BuildAdbStatusPanel()
    {
        var group = new GroupBox
        {
            Text = "ADB Connection",
            Dock = DockStyle.Top,
            Height = 112,
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
        AddStatusCell(grid, "Battery", 2, 0, FontStyle.Bold);
        AddStatusCell(grid, BuildBatteryBar(), 2, 1);
        AddStatusCell(grid, "SSID", 2, 2, FontStyle.Bold);
        AddStatusCell(grid, deviceSsidLabel, 2, 3);

        SetAdbStatusText("Unknown", "Unknown", "-", "-", "-", "-");
        return group;
    }

    private Control BuildButtonRow()
    {
        var buttonRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 1,
            Height = 112,
            Margin = new Padding(0, 10, 0, 0),
        };
        buttonRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        buttonRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var mainButtons = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Margin = new Padding(0),
        };
        mainButtons.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        mainButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        mainButtons.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainButtons.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        buttonRow.Controls.Add(mainButtons, 0, 0);

        var pcAppGroup = new GroupBox
        {
            Text = "Meta Horizon Link",
            Width = 430,
            Height = 68,
            Padding = new Padding(8, 4, 8, 6),
            Margin = new Padding(0, 0, 8, 0),
        };
        mainButtons.Controls.Add(pcAppGroup, 0, 0);
        mainButtons.SetColumnSpan(pcAppGroup, 2);

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
        AddButton(pcAppButtons, disconnectButton, "Disconnect", 104, async (_, _) => await RunAdbCommandAsync(
            "Disconnect",
            "shell",
            "am",
            "force-stop",
            GetPackageName()));

        var actionButtons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0, 8, 0, 0),
        };
        mainButtons.Controls.Add(actionButtons, 0, 1);
        mainButtons.SetColumnSpan(actionButtons, 2);

        AddButton(actionButtons, refreshButton, "Refresh", 78, async (_, _) => await RefreshAdbAsync());
        AddButton(actionButtons, screenshotButton, "Screenshot", 92, async (_, _) => await CaptureScreenshotAsync());
        AddButton(actionButtons, rebootQuestButton, "Reboot Meta Quest", 156, async (_, _) => await RunAdbCommandAsync("Reboot Meta Quest", "reboot"));
        AddButton(actionButtons, debugToolButton, "Oculus Debug Tool", 156, (_, _) => StartDebugTool());
        AddButton(actionButtons, logToggleButton, "Log", 84, (_, _) => SetLogVisible(!logBox.Visible));

        return buttonRow;
    }

    private Control BuildUnityPanel()
    {
        var group = new GroupBox
        {
            Text = "Unity",
            Dock = DockStyle.Fill,
            Height = 68,
            Padding = new Padding(8, 8, 8, 8),
            Margin = new Padding(0, 8, 0, 0),
        };

        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            Margin = new Padding(0),
        };
        row.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        group.Controls.Add(row);

        unityProcessCombo.Dock = DockStyle.Fill;
        unityProcessCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        unityProcessCombo.DropDownHeight = 160;
        unityProcessCombo.Margin = new Padding(0, 6, 8, 0);
        unityProcessCombo.SelectedIndexChanged += (_, _) => killUnityButton.Enabled = unityProcessCombo.SelectedItem is UnityProcessItem;
        row.Controls.Add(unityProcessCombo, 0, 0);

        AddButton(row, refreshUnityButton, "Refresh", 78, (_, _) => RefreshUnityProcesses());
        AddButton(row, killUnityButton, "Force Quit", 92, (_, _) => KillSelectedUnityProcess());

        return group;
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

    private static void AddStatusCell(TableLayoutPanel grid, Control control, int row, int column)
    {
        control.Dock = DockStyle.Fill;
        grid.Controls.Add(control, column, row);
    }

    private Control BuildBatteryBar()
    {
        var container = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            Margin = new Padding(6, 3, 16, 3),
        };
        container.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        container.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        deviceBatteryBar.Dock = DockStyle.Fill;
        deviceBatteryBar.BackColor = SystemColors.Control;
        deviceBatteryBar.Height = 18;
        deviceBatteryBar.Margin = new Padding(0, 1, 8, 1);
        deviceBatteryBar.Paint += (_, e) => PaintBatteryBar(e.Graphics, deviceBatteryBar.ClientRectangle);
        container.Controls.Add(deviceBatteryBar, 0, 0);

        deviceBatteryLabel.AutoSize = true;
        deviceBatteryLabel.Dock = DockStyle.Fill;
        deviceBatteryLabel.Margin = new Padding(0);
        container.Controls.Add(deviceBatteryLabel, 1, 0);

        return container;
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
            await RefreshAdbStatusAsync(showChecking: true);
        }
        catch (Exception ex)
        {
            adbPath = null;
            adbPathText.Text = "adb detection failed";
            SetAdbStatusText("Detection failed", "Unknown", "-", "-", "-", "-");
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
        debugToolButton.Enabled = enabled;
        logToggleButton.Enabled = enabled;
        launchPcAppButton.Enabled = enabled;
        restartServiceButton.Enabled = enabled;
        refreshUnityButton.Enabled = enabled;
        killUnityButton.Enabled = enabled && unityProcessCombo.SelectedItem is UnityProcessItem;
    }

    private void RefreshUnityProcesses()
    {
        var selectedProcessId = (unityProcessCombo.SelectedItem as UnityProcessItem)?.ProcessId;
        var unityProcesses = Process.GetProcessesByName("Unity")
            .Select(process =>
            {
                try
                {
                    return string.IsNullOrWhiteSpace(process.MainWindowTitle)
                        ? null
                        : new UnityProcessItem(process.Id, process.MainWindowTitle);
                }
                finally
                {
                    process.Dispose();
                }
            })
            .OfType<UnityProcessItem>()
            .OrderBy(item => item.Title, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(item => item.ProcessId)
            .ToArray();

        unityProcessCombo.Items.Clear();
        unityProcessCombo.Items.AddRange(unityProcesses);

        if (unityProcesses.Length == 0)
        {
            unityProcessCombo.Items.Add("No Unity windows found");
            unityProcessCombo.SelectedIndex = 0;
            killUnityButton.Enabled = false;
            return;
        }

        var selectedIndex = Array.FindIndex(unityProcesses, item => item.ProcessId == selectedProcessId);
        unityProcessCombo.SelectedIndex = selectedIndex >= 0 ? selectedIndex : 0;
        killUnityButton.Enabled = true;
        Log($"Found {unityProcesses.Length} Unity process(es).");
    }

    private void KillSelectedUnityProcess()
    {
        if (unityProcessCombo.SelectedItem is not UnityProcessItem selected)
        {
            Log("No Unity process selected.");
            return;
        }

        var result = MessageBox.Show(
            $"Force quit Unity?\n\n{selected.Title}\nPID: {selected.ProcessId}",
            "Force Quit Unity",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);

        if (result != DialogResult.Yes)
        {
            return;
        }

        try
        {
            using var process = Process.GetProcessById(selected.ProcessId);
            if (!string.Equals(process.ProcessName, "Unity", StringComparison.OrdinalIgnoreCase))
            {
                Log($"Selected process is no longer Unity: PID {selected.ProcessId}");
                RefreshUnityProcesses();
                return;
            }

            process.Kill(entireProcessTree: true);
            Log($"Force quit Unity: {selected.Title} (PID {selected.ProcessId})");
        }
        catch (ArgumentException)
        {
            Log($"Unity process already exited: PID {selected.ProcessId}");
        }
        catch (Exception ex)
        {
            Log($"Unity force quit failed: {ex.Message}");
        }
        finally
        {
            RefreshUnityProcesses();
        }
    }

    private async Task RefreshAdbStatusAsync(bool showChecking = false)
    {
        if (isRefreshingAdbStatus)
        {
            return;
        }

        isRefreshingAdbStatus = true;
        if (string.IsNullOrWhiteSpace(adbPath) || !File.Exists(adbPath))
        {
            SetAdbStatusText("adb not found", "No device", "-", "-", "-", "-");
            isRefreshingAdbStatus = false;
            return;
        }

        try
        {
            if (showChecking)
            {
                SetAdbStatusText(TcpTable.TryFindListeningProcessId(5037) is null ? "Starting" : "Running", "Checking...", "-", "-", "-", "-");
            }

            var status = await AdbTools.QueryConnectionStatusAsync(adbPath);
            SetAdbStatusText(status.Server, status.Device, status.Model, status.Ip, status.Battery, status.Ssid);
        }
        catch (Exception ex)
        {
            SetAdbStatusText("Query failed", "Unknown", "-", "-", "-", "-");
            Log($"ADB status refresh failed: {ex.Message}");
        }
        finally
        {
            isRefreshingAdbStatus = false;
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

    private void SetAdbStatusText(string server, string device, string model, string ip, string battery, string ssid)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => SetAdbStatusText(server, device, model, ip, battery, ssid));
            return;
        }

        adbServerStatusLabel.Text = server;
        deviceStatusLabel.Text = device;
        deviceModelLabel.Text = model;
        deviceIpLabel.Text = ip;
        deviceBatteryLabel.Text = battery;
        deviceSsidLabel.Text = ssid;
        batteryLevel = ParseBatteryLevel(battery);
        deviceBatteryBar.Invalidate();
    }

    private void SetLogVisible(bool visible)
    {
        logBox.Visible = visible;
        logToggleButton.Text = visible ? "Hide Log" : "Log";

        if (logRowStyle is not null)
        {
            logRowStyle.SizeType = visible ? SizeType.Percent : SizeType.Absolute;
            logRowStyle.Height = visible ? 100 : 0;
        }
    }

    private void PaintBatteryBar(Graphics graphics, Rectangle bounds)
    {
        graphics.Clear(deviceBatteryBar.BackColor);
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        var rect = Rectangle.Inflate(bounds, -1, -2);
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        using var background = new SolidBrush(Color.FromArgb(225, 245, 229));
        using var border = new Pen(Color.FromArgb(142, 190, 152));
        graphics.FillRectangle(background, rect);
        graphics.DrawRectangle(border, rect);

        if (batteryLevel is null)
        {
            return;
        }

        var level = Math.Clamp(batteryLevel.Value, 0, 100);
        var fillWidth = Math.Max(1, (int)Math.Round((rect.Width - 2) * (level / 100.0)));
        var fillRect = new Rectangle(rect.Left + 1, rect.Top + 1, fillWidth, rect.Height - 2);
        using var fill = new SolidBrush(GetBatteryColor(level));
        graphics.FillRectangle(fill, fillRect);
    }

    private static Color GetBatteryColor(int level)
    {
        var low = Color.FromArgb(220, 53, 69);
        var mid = Color.FromArgb(245, 180, 0);
        var high = Color.FromArgb(40, 167, 69);

        if (level <= 50)
        {
            return InterpolateColor(low, mid, level / 50.0);
        }

        return InterpolateColor(mid, high, (level - 50) / 50.0);
    }

    private static Color InterpolateColor(Color from, Color to, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        return Color.FromArgb(
            (int)Math.Round(from.R + ((to.R - from.R) * amount)),
            (int)Math.Round(from.G + ((to.G - from.G) * amount)),
            (int)Math.Round(from.B + ((to.B - from.B) * amount)));
    }

    private static int? ParseBatteryLevel(string battery)
    {
        var match = System.Text.RegularExpressions.Regex.Match(battery, @"(?<level>\d+)%");
        return match.Success ? int.Parse(match.Groups["level"].Value) : null;
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

    private sealed record UnityProcessItem(int ProcessId, string Title)
    {
        public override string ToString()
        {
            return $"{ProcessId} - {Title}";
        }
    }

    private sealed class BatteryBarPanel : Panel
    {
        public BatteryBarPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.UserPaint,
                true);
        }
    }
}
