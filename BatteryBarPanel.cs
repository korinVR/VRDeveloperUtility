namespace VRDeveloperUtility;

internal sealed class BatteryBarPanel : Panel
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
