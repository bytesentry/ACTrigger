using System;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;

namespace ACTrigger.UI.Models;

public partial class TrackedDebuff : ObservableObject
{
    public int TargetId { get; set; }

    public string TargetName { get; set; } = "";

    public string SpellName { get; set; } = "";

    public string Description { get; set; } = "";

    public double DurationSeconds { get; set; }

    [ObservableProperty]
    private DateTime expiresAt;

    private static readonly Dictionary<string, string> DebuffDisplayNames =
        new()
        {
            ["Imperil Other"] = "Imperil",

            ["Fire Vulnerability"] = "Fire",
            ["Acid Vulnerability"] = "Acid",
            ["Lightning Vulnerability"] = "Lightning",
            ["Cold Vulnerability"] = "Cold",
            ["Olthoi's Gift"] = "Olthoi's Gift", 
            ["Inferno's Gift"] = "Inferno's Gift",      
            ["Astyrrian's Gift"] = "Astyrrian's Gift", 
            ["Gelidite's Gift"] = "Gelidite's Gift", 

            ["Blade Vulnerability"] = "Slash",
            ["Piercing Vulnerability"] = "Pierce",
            ["Bludgeoning Vulnerability"] = "Bludgeon",

            ["Broadside of a Barn"] = "Broadside",
            ["Defenselessness"] = "Defenselessness",
            ["Futility"] = "Futility",
            ["Gravity Well"] = "Gravity Well",
            ["Vulnerability"] = "Vulnerability",
            ["Fester"] = "Fester",
            ["Magic Yield"] = "Magic Yield",
            ["Frailty"] = "Frailty",
            ["Brittle Bones"] = "Brittle Bones"
        };

    public double ProgressValue =>
    DurationSeconds <= 0
        ? 0
        : RemainingSeconds /
          DurationSeconds * 100;

    public double ProgressPercent =>
    DurationSeconds <= 0
        ? 0
        : RemainingSeconds /
          DurationSeconds;

    public double RemainingSeconds =>
        Math.Max(
            0,
            (ExpiresAt - DateTime.UtcNow)
            .TotalSeconds);

    public void RefreshTimer()
    {
        OnPropertyChanged(
            nameof(RemainingSeconds));

        OnPropertyChanged(
            nameof(RemainingTimeText));

        OnPropertyChanged(
            nameof(ProgressValue));
    }
    
    public string RemainingTimeText
    {
        get
        {
            var remaining =
                TimeSpan.FromSeconds(
                    RemainingSeconds);

            return
                $"{remaining.Minutes}:{remaining.Seconds:00}";
        }
    }

    public string DisplayName
    {
        get
        {
            foreach (var kvp in DebuffDisplayNames)
            {
                if (SpellName.Equals(
                        kvp.Key,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Value;
                }
            }

            return SpellName;
        }
    }
    public string ProgressBar
    {
        get
        {
            const int width = 15;

            int filled =
                (int)Math.Round(
                    ProgressPercent *
                    width);

            return
                new string('█', filled) +
                new string(
                    '░',
                    width - filled);
        }
    }
}