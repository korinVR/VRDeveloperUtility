namespace VRDeveloperUtility;

internal sealed partial class MainForm
{
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
}
