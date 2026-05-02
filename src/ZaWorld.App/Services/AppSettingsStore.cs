using System.Text.Json;
using ZaWorld.Core.Configuration;
using System.IO;

namespace ZaWorld.App.Services;

public sealed class AppSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    public string GetSettingsFilePath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appDataPath, "Za-World");
        Directory.CreateDirectory(appFolder);
        return Path.Combine(appFolder, "config.json");
    }

    public AppSettings LoadOrCreateDefault()
    {
        var path = GetSettingsFilePath();
        if (!File.Exists(path))
        {
            var created = CreateDefaultSettings();
            Save(created);
            return created;
        }

        var rawJson = File.ReadAllText(path);
        var loaded = JsonSerializer.Deserialize<AppSettings>(rawJson, JsonOptions);
        if (loaded is null)
        {
            var fallback = CreateDefaultSettings();
            Save(fallback);
            return fallback;
        }

        return loaded;
    }

    public void Save(AppSettings settings)
    {
        var path = GetSettingsFilePath();
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(path, json);
    }

    private static AppSettings CreateDefaultSettings()
    {
        var pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        return new AppSettingsFactory().CreateDefault(pictures);
    }
}
