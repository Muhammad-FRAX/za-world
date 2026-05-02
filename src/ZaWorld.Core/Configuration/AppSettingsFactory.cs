namespace ZaWorld.Core.Configuration;

public sealed class AppSettingsFactory
{
    public AppSettings CreateDefault(string picturesFolder)
    {
        return new AppSettings
        {
            UnlockHotkey = "Ctrl+Alt+Shift+U",
            CaptureFolderPath = Path.Combine(picturesFolder, "TheWorldLock", "Captures"),
        };
    }
}
