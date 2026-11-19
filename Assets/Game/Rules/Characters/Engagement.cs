namespace Isekai
{
    /// <summary>
    /// Shared fight-or-return decision for anything that fights around a spot (its post): the Character while
    /// idle, and Units by their Building's <see cref="Stance"/>.
    /// </summary>
    public static class Engagement
    {
        /// <param name="enemyDistance">Distance to the nearest living enemy, or null when there is none.</param>
        /// <param name="enemyDistanceFromPost">That enemy's distance from the post (ignored when there is none).</param>
        /// <param name="attackRange">The reach of the fighter's weapons.</param>
        /// <param name="leash">How far from the post it will chase (0 = never moves to attack).</param>
        /// <param name="distanceFromPost">The fighter's own distance from its post.</param>
        /// <param name="returnTolerance">How close to the post counts as back.</param>
        public static AutoFightOrder Decide(float? enemyDistance, float enemyDistanceFromPost, float attackRange, float leash,
            float distanceFromPost, float returnTolerance = 0.5f)
        {
            if (enemyDistance is float d)
            {
                if (d <= attackRange) return AutoFightOrder.Hold;
                if (leash > 0f && enemyDistanceFromPost <= leash) return AutoFightOrder.Approach;
            }
            return distanceFromPost > returnTolerance ? AutoFightOrder.Return : AutoFightOrder.Hold;
        }
    }
}
