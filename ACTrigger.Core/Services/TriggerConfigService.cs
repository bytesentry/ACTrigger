using System.Text.Json;
using ACTrigger.Models;

namespace ACTrigger.Core.Services;

public class TriggerConfigService
{
    private const string TriggerFile =
        "Triggers/triggers.json";

    public TriggerConfig Load()
    {
        if (!File.Exists(TriggerFile))
        {
            return new TriggerConfig();
        }

        var json = File.ReadAllText(TriggerFile);

        return JsonSerializer.Deserialize<TriggerConfig>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            })
            ?? new TriggerConfig();
    }
    public void Save(TriggerConfig config)
    {
        var json = JsonSerializer.Serialize(
            config,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        Directory.CreateDirectory("Triggers");

        File.WriteAllText(
            TriggerFile,
            json);
    }
}   
