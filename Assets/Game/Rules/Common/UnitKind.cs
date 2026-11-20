namespace Isekai
{
    /// <summary>What a registered unit is, for systems that react to kinds of death (Souls, Vigilance, bones) or count units.</summary>
    public enum UnitKind
    {
        /// <summary>The body Jack controls.</summary>
        Character,
        /// <summary>A demon Jack doesn't inhabit (on whichever side it ends up).</summary>
        Demon,
        /// <summary>A mortal unit: soldier, peasant, hero.</summary>
        Mortal,
        /// <summary>A wildlife creature, on the Neutral or the Hostile side.</summary>
        Wildlife,
        /// <summary>Anything placed, of either side.</summary>
        Building,
    }
}
