using ACTrigger.Core.Models;

namespace ACTrigger.Core.Events;

public sealed class SpellRemovedEvent
{
    public SpellEffect Effect { get; }

    public SpellRemovedEvent(SpellEffect effect)
    {
        Effect = effect;
    }
}