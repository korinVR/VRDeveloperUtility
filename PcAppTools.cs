namespace VRDeveloperUtility;

internal static class PcAppTools
{
    public static string? FindClientPath()
    {
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var candidates = new[]
        {
            @"C:\Program Files\Meta Horizon\Support\oculus-client\OculusClient.exe",
            @"C:\Program Files\Oculus\Support\oculus-client\OculusClient.exe",
            Path.Combine(programFiles, "Meta Horizon", "Support", "oculus-client", "OculusClient.exe"),
            Path.Combine(programFiles, "Oculus", "Support", "oculus-client", "OculusClient.exe"),
        };

        return candidates.FirstOrDefault(File.Exists);
    }
}
