using ACTrigger.Models;

namespace ACTrigger.Core.Services;

public class TriggerService
{
    private readonly SoundService soundService =
        new();
    
    private readonly List<Trigger> _triggers;
    public event Action<Trigger, LogEntry>? TriggerMatched;

    public TriggerService()
    {
        var triggerConfigService = new TriggerConfigService();

        var config = triggerConfigService.Load();

        _triggers = config.Triggers;
    }

    public void Check(LogEntry entry)
    {
        foreach (var trigger in _triggers)
        {
            if (!trigger.Enabled)
                continue;

            bool matched =
                trigger.CaseSensitive
                ? entry.Message.Contains(trigger.Pattern)
                : entry.Message.Contains(
                    trigger.Pattern,
                    StringComparison.OrdinalIgnoreCase);

            if (matched)
            {

                TriggerMatched?.Invoke(trigger, entry);

                if (!string.IsNullOrEmpty(trigger.SoundFile))
                {
                    soundService.Play(trigger.SoundFile);
                }
            }
        }
    }
    public void Reload()
    {
        var triggerConfigService = new TriggerConfigService();

        var config = triggerConfigService.Load();

        _triggers.Clear();

        _triggers.AddRange(config.Triggers);
    }
}