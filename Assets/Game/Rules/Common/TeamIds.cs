namespace Isekai
{
    /// <summary>
    /// The team names units carry (Blondie Combat's Team): who is hostile to whom comes from
    /// <c>Blondie.Combat.Teams</c>, never from a unit's component type. More factions add names here.
    /// </summary>
    public static class TeamIds
    {
        /// <summary>The player's side (today's Character, Buildings and Units; the demons after the redesign).</summary>
        public const string Player = "Players";

        /// <summary>The defenders (today's horde; the mortal factions after the redesign).</summary>
        public const string Enemy = "Enemies";
    }
}
