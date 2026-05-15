using System.Drawing.Drawing2D;
using System.Text.RegularExpressions;

namespace VRDeveloperUtility;

internal sealed partial class MainForm
{
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

    private void PaintBatteryBar(Graphics graphics, Rectangle bounds)
    {
        graphics.Clear(deviceBatteryBar.BackColor);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

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
        var match = Regex.Match(battery, @"(?<level>\d+)%");
        return match.Success ? int.Parse(match.Groups["level"].Value) : null;
    }
}
