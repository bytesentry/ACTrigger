using ACTrigger.Core.Models;

namespace ACTrigger.Core.Events;

public sealed class SpellAddedEvent
{
    public SpellEffect Effect { get; }

    public SpellAddedEvent(SpellEffect effect)
    {
        Effect = effect;
    }
}