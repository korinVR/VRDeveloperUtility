namespace VRDeveloperUtility;

internal static class PcAppTools
{
    public static string? FindClientPath()
    {
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        var candidates = new[]
        {
            @"C:\Program Files\Meta Horizon\Support\oculus-client\Client.exe",
            @"C:\Program Files\Meta Horizon\Support\oculus-client\OculusClient.exe",
            @"C:\Program Files\Oculus\Support\oculus-client\Client.exe",
            @"C:\Program Files\Oculus\Support\oculus-client\OculusClient.exe",
            Path.Combine(programFiles, "Meta Horizon", "Support", "oculus-client", "OculusClient.exe"),
            Path.Combine(programFiles, "Meta Horizon", "Support", "oculus-client", "Client.exe"),
            Path.Combine(programFiles, "Oculus", "Support", "oculus-client", "OculusClient.exe"),
            Path.Combine(programFiles, "Oculus", "Support", "oculus-client", "Client.exe"),
            Path.Combine(programFilesX86, "Meta Horizon", "Support", "oculus-client", "Client.exe"),
            Path.Combine(programFilesX86, "Meta Horizon", "Support", "oculus-client", "OculusClient.exe"),
            Path.Combine(programFilesX86, "Oculus", "Support", "oculus-client", "Client.exe"),
            Path.Combine(programFilesX86, "Oculus", "Support", "oculus-client", "OculusClient.exe"),
        };

        return candidates
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(File.Exists);
    }
}
