using System.Text.Json;
using ACTrigger.Models;

namespace ACTrigger.Core.Services;

public class SettingsService
{
    private const string SettingsFile =
        "settings.json";

    public void Save(Settings settings)
    {
        var json = JsonSerializer.Serialize(
            settings,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        File.WriteAllText(SettingsFile, json);
    }
    
    public Settings Load()
    {
        Console.WriteLine($"Current Directory: {Directory.GetCurrentDirectory()}");

        Console.WriteLine($"Looking for settings at: {Path.GetFullPath(SettingsFile)}");
        
        if (!File.Exists(SettingsFile))
        {
            return new Settings();
        }

        var json = File.ReadAllText(SettingsFile);
        //Console.WriteLine(json);

        var settings = JsonSerializer.Deserialize<Settings>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        if (settings == null)
        {
            settings = new Settings();
        }

        if (string.IsNullOrWhiteSpace(settings.HudFont))
        {
            settings.HudFont = "Palatino Linotype";
        }

        return settings;
    }
}