using Blondie.Combat;

namespace Isekai
{
    /// <summary>
    /// Writes the <see cref="SideRelations"/> table into Blondie Combat's <see cref="Teams"/> when a Region starts, for
    /// every pair of sides, so who fights whom is explicit instead of resting on Blondie's "different teams are
    /// hostile" default.
    /// </summary>
    public static class RegionSides
    {
        public static void Apply()
        {
            foreach (Side a in SideRelations.All)
            foreach (Side b in SideRelations.All)
                Teams.SetRelation(TeamIds.Of(a), TeamIds.Of(b), ToTeam(SideRelations.Between(a, b)));
        }

        private static TeamRelation ToTeam(SideRelation relation) => relation switch
        {
            SideRelation.Hostile => TeamRelation.Hostile,
            SideRelation.Allied => TeamRelation.Allied,
            _ => TeamRelation.Neutral,
        };
    }
}
