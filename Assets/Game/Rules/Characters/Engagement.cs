namespace Isekai
{
    /// <summary>
    /// Shared fight-or-return decision for anything that fights around a spot (its anchor or post): the Character while
    /// idle, and Units by their Building's <see cref="Stance"/>.
    /// </summary>
    public static class Engagement
    {
        /// <param name="enemyDistance">Distance to the nearest living enemy, or null when there is none.</param>
        /// <param name="enemyDistanceFromAnchor">That enemy's distance from the anchor (ignored when there is none).</param>
        /// <param name="attackRange">The reach of the fighter's weapons.</param>
        /// <param name="leash">How far from the anchor it will chase (0 = never moves to attack).</param>
        /// <param name="distanceFromAnchor">The fighter's own distance from its anchor.</param>
        /// <param name="returnTolerance">How close to the anchor counts as back.</param>
        public static AutoFightOrder Decide(float? enemyDistance, float enemyDistanceFromAnchor, float attackRange, float leash,
            float distanceFromAnchor, float returnTolerance = 0.5f)
        {
            if (enemyDistance is float d)
            {
                if (d <= attackRange) return AutoFightOrder.Hold;
                if (leash > 0f && enemyDistanceFromAnchor <= leash) return AutoFightOrder.Approach;
            }
            return distanceFromAnchor > returnTolerance ? AutoFightOrder.Return : AutoFightOrder.Hold;
        }
    }
}
