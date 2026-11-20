namespace Isekai
{
    /// <summary>
    /// The Traits (CONTEXT.md: random data on a unit that weights its AI's decisions and comes with a buff). Adding one is a
    /// row in <see cref="TraitTable"/>, not a new AI.
    /// </summary>
    public enum TraitId
    {
        /// <summary>Flees below 75% health. +30% max health.</summary>
        Cowardly,
        /// <summary>Never flees. +20% damage.</summary>
        Berserk,
        /// <summary>Does not flee until 10% health. +10% armour.</summary>
        Brave,
        /// <summary>Invisible until it attacks (then visible for 3 seconds). +25% damage on its first hit.</summary>
        Sneaky,
        /// <summary>Backs off after 3 kills. +30% move speed while hidden.</summary>
        Ambusher,
        /// <summary>Ignores orders, wanders off to cause trouble. +15% attack speed.</summary>
        Unruly,
        /// <summary>Raises alarms faster. +50% vision.</summary>
        Vigilant,
        /// <summary>Stays near its settlement or post. +20% health near it.</summary>
        Protector,
        /// <summary>Removes corruption, heals nearby allies slowly.</summary>
        Purifier,
    }

    /// <summary>How a unit ranks, which sets its odds of having Traits: Heroes (the Region's rulers and their kind) always have 2 or 3.</summary>
    public enum TraitRank
    {
        Ordinary,
        Elite,
        Hero,
    }
}
