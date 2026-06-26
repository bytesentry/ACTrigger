namespace ACTrigger.Models;
using CommunityToolkit.Mvvm.ComponentModel;

public enum CombatTextType
{
    OutgoingDamage,
    IncomingDamage,
    Kill,
    Heal,
    Buff,
    Debuff,
    System
}

public partial class CombatText
    : ObservableObject
{
    public string Text { get; set; } = "";

    public CombatTextType Type { get; set; }

    public bool IsCritical { get; set; }

    public DateTime Created { get; set; }

    public string Color { get; set; } = "White";

    public int FontSize { get; set; } = 40;

    [ObservableProperty]
    private double x;

    [ObservableProperty]
    private double y;

    [ObservableProperty]
    private double spawnX;

    [ObservableProperty]
    private double spawnY;

    [ObservableProperty]
    private double opacity = 1.0;

    [ObservableProperty]
    private double scale = 1.0;
    
    [ObservableProperty]
    private double velocityX;

    [ObservableProperty]
    private double velocityY;

    [ObservableProperty]
    private double age;

    [ObservableProperty]
    private double lifetime = 3.0;
}