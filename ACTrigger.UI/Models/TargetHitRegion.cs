using Avalonia;

namespace ACTrigger.UI.Models;

public class TargetHitRegion
{
    public int TargetId { get; set; }

    public string TargetName { get; set; } = "";

    public Rect Bounds { get; set; }
}