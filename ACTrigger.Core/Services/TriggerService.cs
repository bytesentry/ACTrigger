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

                if (trigger.Channel != TriggerChannel.Any &&
                    trigger.Channel.ToString() != entry.Channel)
                {
                    continue;
                }

            if (trigger.IgnoreOutgoing &&
                IsOutgoingSocialMessage(entry))
            {
                continue;
            }

            StringComparison comparison =
                trigger.CaseSensitive
                    ? StringComparison.Ordinal
                    : StringComparison.OrdinalIgnoreCase;

            bool matched =
                string.IsNullOrWhiteSpace(trigger.Pattern)
                    ? true
                    : trigger.StartsWith
                        ? entry.Message.StartsWith(
                            trigger.Pattern,
                            comparison)
                        : entry.Message.Contains(
                            trigger.Pattern,
                            comparison);

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

    private static bool IsOutgoingSocialMessage(LogEntry entry)
    {
        if (entry.Channel is not ("Tell" or
                                "Area" or
                                "General" or
                                "Trade" or
                                "Fellowship" or
                                "Allegiance" or
                                "LFG" or
                                "Roleplay" or
                                "Society"))
        {
            return false;
        }

        return entry.Message.StartsWith(
                "You tell ",
                StringComparison.OrdinalIgnoreCase)
            || entry.Message.StartsWith(
                "You say, ",
                StringComparison.OrdinalIgnoreCase);
    }
}