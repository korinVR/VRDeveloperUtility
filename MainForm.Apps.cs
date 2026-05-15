using System.Diagnostics;

namespace VRDeveloperUtility;

internal sealed partial class MainForm
{
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

    private void RestoreFromTray()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
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
