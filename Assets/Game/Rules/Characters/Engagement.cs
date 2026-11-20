namespace Isekai
{
    /// <summary>
    /// Shared fight-or-return decision for anything that fights around a spot (its post): the Character on
    /// auto-fight and demons that fight around their post.
    /// </summary>
    public static class Engagement
    {
        /// <param name="targetDistance">Distance to the nearest living hostile unit, or null when there is none.</param>
        /// <param name="targetDistanceFromPost">That target's distance from the post (ignored when there is none).</param>
        /// <param name="attackRange">The reach of the fighter's weapons.</param>
        /// <param name="leash">How far from the post it will chase (0 = never moves to attack).</param>
        /// <param name="distanceFromPost">The fighter's own distance from its post.</param>
        /// <param name="returnTolerance">How close to the post counts as back.</param>
        public static AutoFightOrder Decide(float? targetDistance, float targetDistanceFromPost, float attackRange, float leash,
            float distanceFromPost, float returnTolerance = 0.5f)
        {
            if (targetDistance is float d)
            {
                if (d <= attackRange) return AutoFightOrder.Hold;
                if (leash > 0f && targetDistanceFromPost <= leash) return AutoFightOrder.Approach;
            }
            return distanceFromPost > returnTolerance ? AutoFightOrder.Return : AutoFightOrder.Hold;
        }
    }
}
