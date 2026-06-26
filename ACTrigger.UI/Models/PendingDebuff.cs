namespace ACTrigger.UI.Models;

public class PendingDebuff
{
    public int TargetId { get; set; }
    public string TargetName { get; set; } = "";
    public string SpellName { get; set; } = "";
    public string Description { get; set; } = "";
    public double DurationSeconds { get; set; }
}