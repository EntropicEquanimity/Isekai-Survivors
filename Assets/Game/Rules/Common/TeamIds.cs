namespace Isekai
{
    /// <summary>
    /// The team names units carry (Blondie Combat's Team) and how they map to <see cref="Side"/>: who is hostile to
    /// whom is set from <see cref="SideRelations"/> when a Region starts, never from a unit's component type.
    /// </summary>
    public static class TeamIds
    {
        /// <summary>Jack's side (today's Character, Buildings and Units; the demons after the redesign).</summary>
        public const string Player = "Players";

        /// <summary>The mortals (today's horde; the mortal factions after the redesign).</summary>
        public const string Enemy = "Enemies";

        /// <summary>Creatures that attack no one by default.</summary>
        public const string Neutral = "Neutrals";

        /// <summary>Creatures that attack everything on sight.</summary>
        public const string Hostile = "Hostile";

        /// <summary>Revolted demons: their own team, hostile to everyone.</summary>
        public const string Rogue = "Rogues";

        /// <summary>The Blondie Combat team name of a side.</summary>
        public static string Of(Side side) => side switch
        {
            Side.Yours => Player,
            Side.Mortals => Enemy,
            Side.Hostile => Hostile,
            Side.Rogue => Rogue,
            _ => Neutral,
        };

        /// <summary>The side a team name belongs to; no team, or a name that is none of ours, is <see cref="Side.Neutral"/>.</summary>
        public static Side SideOf(string team) => team switch
        {
            Player => Side.Yours,
            Enemy => Side.Mortals,
            Hostile => Side.Hostile,
            Rogue => Side.Rogue,
            _ => Side.Neutral,
        };
    }
}
