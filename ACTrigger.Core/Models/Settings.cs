namespace ACTrigger.Models;

public class Settings
{
    public string LogPath { get; set; } = "";
    
    public OverlaySettings DamageOut { get; set; } =
    new();

    public OverlaySettings DamageIn { get; set; } =
        new();

    public OverlaySettings DebuffOverlay { get; set; } = new();
    
    public bool OverlayEnabled { get; set; }

    public bool ShowDamageOut { get; set; }

    public bool ShowDamageIn { get; set; }

    public bool EditOverlayLayout { get; set; } = false;

    public bool ShowDebuffs { get; set; } = false;

    public bool AllowTargeting { get; set; } = false;

    public string HudFont { get; set; } = "Palatino Linotype";
}