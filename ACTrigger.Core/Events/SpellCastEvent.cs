namespace ACTrigger.Core.Events;

public sealed class SpellCastEvent
{
    public int SpellId { get; }

    public int TargetId { get; }

    public SpellCastEvent(
        int spellId,
        int targetId)
    {
        SpellId = spellId;
        TargetId = targetId;
    }
}