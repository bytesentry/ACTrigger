namespace ACTrigger.Core.Models;

public sealed class SpellEffect
{
    public int SpellId { get; init; }

    public int Family { get; init; }

    public double Duration { get; init; }

    public double Remaining { get; init; }

    public double Adjustment { get; init; }
}