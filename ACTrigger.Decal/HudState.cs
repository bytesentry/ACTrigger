using System;

namespace ACTrigger.Decal;

sealed class HudState
{
    public int ObjectId;

    // Last successfully displayed HUD.
    public string DisplayedName = "";
    public int DisplayedLevel;
    public string DisplayedMonarch = "";

    // Current world state.
    public string DesiredName = "";
    public int DesiredLevel;
    public string DesiredMonarch = "";
    public bool Visible;
    public long HiddenOrder { get; set; }
    //public bool Initialized;

    // One outstanding request at a time.
    public bool RegenerationRequested;
    public bool CacheDirty;

    public bool CreateQueued;
    public bool DisposeQueued;

    // Existing file reload tracking.
    public DateTime LastHudWriteTimeUtc;
    public float ImageWidth;
    public float ImageHeight;

    // True when the displayed HUD no longer matches the world.
    public bool NeedsUpdate =>
        !Synced;

    public bool Synced =>
        DisplayedName == DesiredName &&
        DisplayedLevel == DesiredLevel &&
        DisplayedMonarch == DesiredMonarch;
}