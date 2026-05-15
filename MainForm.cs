namespace VRDeveloperUtility;

internal sealed partial class MainForm : Form
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
}
