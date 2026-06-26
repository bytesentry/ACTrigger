namespace ACTrigger.Models;

public class OverlaySettings
{
    public double X { get; set; }
    public double Y { get; set; }

    public double Width { get; set; } = 250;
    public double Height { get; set; } = 250;

    public bool Enabled { get; set; } = true;
}