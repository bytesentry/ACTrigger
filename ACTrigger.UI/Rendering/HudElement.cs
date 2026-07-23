using Avalonia.Media.Imaging;

namespace ACTrigger.UI.Rendering;

public sealed class HudElement
{
    public required string FileName
    {
        get;
        init;
    }

    public int Row
    {
        get;
        init;
    }

    public int Order
    {
        get;
        init;
    }
}