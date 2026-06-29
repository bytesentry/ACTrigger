namespace ACTrigger.Models;

public enum TriggerChannel
{
    Any,

    // Chat
    Area,
    Tell,
    TellOutgoing,
    TellNpc,
    Fellowship,
    Allegiance,
    General,
    Trade,
    LFG,
    Roleplay,
    Society,

    // Gameplay
    Combat,
    SpellCast,
    Enchant,

    // Plugin events
    DebuffCast,
    PortalComplete,
    ReleaseVerified,

    // System
    System
}