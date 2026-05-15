namespace VRDeveloperUtility;

internal sealed partial class MainForm
{
    private void RefreshUnityProcesses()
    {
        var selectedProcessId = (unityProcessCombo.SelectedItem as UnityProcessItem)?.ProcessId;
        var unityProcesses = UnityProcessTools.FindUnityProcesses();

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
            using var process = System.Diagnostics.Process.GetProcessById(selected.ProcessId);
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
}
