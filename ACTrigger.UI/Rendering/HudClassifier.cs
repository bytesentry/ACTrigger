using System;

namespace ACTrigger.UI.Rendering;

public static class HudClassifier
{
    public static void Classify(
        ref HudType hudType,
        ref HudStyle style,
        ref string name,
        ref int level,
        ref string monarch)
    {
        if (TryParsePet(
            hudType,
            name,
            out string owner,
            out string petName))
        {
            hudType = HudType.Pet;
            name = petName;
            level = 0;
            monarch = owner;
        }
    }

    private static bool TryParsePet(
        HudType hudType,
        string name,
        out string owner,
        out string petName)
    {
        owner = string.Empty;
        petName = string.Empty;

        if (string.IsNullOrWhiteSpace(name))
            return false;

        const string marker = "'s ";

        int index = name.IndexOf(
            marker,
            StringComparison.Ordinal);

        if (index <= 0)
            return false;

        owner = name[..index].Trim();
        petName = name[(index + marker.Length)..].Trim();

        if (petName.Length == 0)
            return false;

        switch (hudType)
        {
            case HudType.Npc:
                // Vanity pets only.
                if (petName.IndexOf(
                        "Pet",
                        StringComparison.OrdinalIgnoreCase) < 0)
                {
                    return false;
                }
                break;

            case HudType.Monster:
            case HudType.Pet:
                // Summoner pets.
                break;

            default:
                return false;
        }

        return true;
    }
}