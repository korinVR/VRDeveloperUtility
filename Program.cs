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
