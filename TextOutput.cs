namespace VRDeveloperUtility;

internal static class TextOutput
{
    public static IEnumerable<string> SplitLines(string value)
    {
        return value
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);
    }
}
