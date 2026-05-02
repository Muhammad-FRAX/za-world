namespace ZaWorld.Core.Configuration;

public sealed class AppSettings
{
    public string UnlockHotkey { get; init; } = "Ctrl+Alt+Shift+U";
    public string CaptureFolderPath { get; init; } = string.Empty;
}
