using System.Diagnostics;

namespace VRDeveloperUtility;

internal static class UnityProcessTools
{
    public static UnityProcessItem[] FindUnityProcesses()
    {
        return Process.GetProcessesByName("Unity")
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
    }
}

internal sealed record UnityProcessItem(int ProcessId, string Title)
{
    public override string ToString()
    {
        return $"{ProcessId} - {Title}";
    }
}
