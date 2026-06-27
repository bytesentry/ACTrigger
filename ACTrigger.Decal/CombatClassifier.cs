using System.Text.RegularExpressions;
using System.Linq;

namespace ACTrigger.Decal
{
    public static class CombatClassifier
    {
        private static readonly Regex[] CombatPatterns =
        {
            // Failed attacks
            new(@"^You evaded .+!$"),
            new(@"^.+ evaded your attack\.$"),
            new(@"^You resist the spell cast by .+$"),
            new(@"^.+ resists your spell$"),

            // Melee/Missile received
            new(@"^Critical hit! Overpower! .+ your .+ for .+ point.* of .+ damage.*$"),
            new(@"^Critical hit! .+ your .+ for .+ point.* of .+ damage.*$"),
            new(@"^Overpower! .+ your .+ for .+ point.* of .+ damage.*$"),
            new(@"^.+ your .+ for .+ point.* of .+ damage.*$"),

            // Melee/Missile given
            new(@"^Critical hit!  You .+ for .+ point.* of .+ damage.*$"),
            new(@"^You .+ for .+ point.* of .+ damage.*$"),
            new(@"^Critical hit!  Sneak Attack! You .+ for .+ point.* of .+ damage.*$"),
            new(@"^Sneak Attack! You .+ for .+ point.* of .+ damage.*$"),
            new(@"^Sneak Attack! Recklessness! You .+ for .+ point.* of .+ damage.*$"),
            new(@"^Recklessness! You .+ for .+ point.* of .+ damage.*$"),

            // Magic received
            new(@"^Critical hit! Overpower! .+ you for .+ point.* with .+$"),
            new(@"^Critical hit! .+ you for .+ point.* with .+$"),
            new(@"^Overpower! .+ you for .+ point.* with .+$"),
            new(@"^.+ you for .+ point.* with .+$"),
            new(@"^Magical energies lose .+ point.* of health due to .+$"),
            new(@"^You lose .+ point.* of health due to .+$"),
            new(@"^.+ casts .+ and drains .+ point.* .+$"),

            // Magic given
            new(@"^Critical hit! You .+ for .+ point.* with .+$"),
            new(@"^You .+ for .+ point.* with .+$"),

            // Magic casts
            new(@"^You cast .+ on .+$"),

            // Kill messages
            new(@"^You flatten .+'s body with the force of your assault!$"),
            new(@"^You bring .+ to a fiery end!$"),
            new(@"^You beat .+ to a lifeless pulp!$"),
            new(@"^You smite .+ mightily!$"),
            new(@"^You obliterate .+!$"),
            new(@"^You run .+ through!$"),
            new(@"^You reduce .+ to a sizzling, oozing mass!$"),
            new(@"^You knock .+ into next Morningthaw!$"),
            new(@"^You split .+ apart!$"),
            new(@"^You cleave .+ in twain!$"),
            new(@"^You slay .+ viciously enough to impart death several times over!$"),
            new(@"^You reduce .+ to a drained, twisted corpse!$"),

            new(@"^Your killing blow nearly turns .+ inside-out!$"),
            new(@"^Your attack stops .+ cold!$"),
            new(@"^Your lightning coruscates over .+'s mortal remains!$"),
            new(@"^Your assault sends .+ to an icy death!$"),
            new(@"^You killed .+!$"),

            new(@"^The thunder of crushing .+ is followed by the deafening silence of death!$"),
            new(@"^The deadly force of your attack is so strong that .+'s ancestors feel it!$"),

            new(@"^.+'s seared corpse smolders before you!$"),
            new(@"^.+ is reduced to cinders!$"),
            new(@"^.+ is shattered by your assault!$"),
            new(@"^.+ catches your attack, with dire consequences!$"),
            new(@"^.+ is utterly destroyed by your attack!$"),
            new(@"^.+ suffers a frozen fate!$"),
            new(@"^.+'s perforated corpse falls before you!$"),
            new(@"^.+ is fatally punctured!$"),
            new(@"^.+'s death is preceded by a sharp, stabbing pain!$"),
            new(@"^.+ is torn to ribbons by your assault!$"),
            new(@"^.+ is liquified by your attack!$"),
            new(@"^.+'s last strength dissolves before you!$"),
            new(@"^Electricity tears .+ apart!$"),
            new(@"^Blistered by lightning, .+ falls!$"),
            new(@"^.+'s last strength withers before you!$"),
            new(@"^.+ is dessicated by your attack!$"),
            new(@"^.+ is incinerated by your assault!$"),

            //healing
            new(@"^You .*heal yourself for (\d+) Health points\."),
            new(@"^You receive (\d+) points of periodic healing\.$"),
            new(@"^.+ restore(?:s)? (\d+) points of your health\.$")
        };
        
        public static bool IsCombat(
            string text)
        {
            return CombatPatterns.Any(
                pattern =>
                    pattern.IsMatch(text));
        }
        
    }
}