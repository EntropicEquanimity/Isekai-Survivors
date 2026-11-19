namespace Isekai
{
    /// <summary>Who a unit or Building fights for. Each side is one Blondie Combat team (<see cref="TeamIds"/>).</summary>
    public enum Side
    {
        /// <summary>Jack and his demons, and what they build.</summary>
        Yours,
        /// <summary>The mortal defenders.</summary>
        Mortals,
        /// <summary>Creatures that attack no one by default.</summary>
        Neutral,
        /// <summary>Hostile wildlife.</summary>
        Wildlife,
        /// <summary>Revolted demons: hostile to everyone.</summary>
        Rogue,
    }

    /// <summary>How one side treats another.</summary>
    public enum SideRelation { Allied, Neutral, Hostile }

    /// <summary>
    /// The relation table between sides (symmetric). Yours, mortals and hostile wildlife fight each other; rogues
    /// fight everyone; a neutral side starts no fight. A side is allied with itself, so rogues don't brawl among
    /// themselves and wildlife leaves wildlife alone.
    /// </summary>
    public static class SideRelations
    {
        /// <summary>Every side.</summary>
        public static readonly Side[] All = { Side.Yours, Side.Mortals, Side.Neutral, Side.Wildlife, Side.Rogue };

        public static SideRelation Between(Side a, Side b)
        {
            if (a == b) return SideRelation.Allied;
            if (a == Side.Rogue || b == Side.Rogue) return SideRelation.Hostile;   // hostile to everyone, neutral creatures included
            if (a == Side.Neutral || b == Side.Neutral) return SideRelation.Neutral;
            return SideRelation.Hostile;   // yours / mortals / wildlife: every pair of different sides
        }

        public static bool AreHostile(Side a, Side b) => Between(a, b) == SideRelation.Hostile;
    }
}
