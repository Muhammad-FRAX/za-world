namespace ZaWorld.Core.Configuration;

public sealed class CapturePathResolver
{
    public string GetEffectiveCaptureFolder(
        string? customPath,
        string picturesFolder,
        Func<string, bool> pathExists,
        Func<string, bool> pathWritable)
    {
        var defaultPath = Path.Combine(picturesFolder, "TheWorldLock", "Captures");
        if (string.IsNullOrWhiteSpace(customPath))
        {
            return defaultPath;
        }

        if (pathExists(customPath) && pathWritable(customPath))
        {
            return customPath;
        }

        return defaultPath;
    }
}
