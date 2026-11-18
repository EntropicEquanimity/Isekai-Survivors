namespace Isekai
{
    /// <summary>What the Character does on its own while the player isn't steering it.</summary>
    public enum AutoFightOrder
    {
        /// <summary>The player is in control.</summary>
        PlayerControl,

        /// <summary>Stand and let the weapons fire at whatever is in range.</summary>
        Hold,

        /// <summary>Walk toward the nearest enemy inside the leash.</summary>
        Approach,

        /// <summary>Nothing to fight: walk back to where the player left the Character.</summary>
        Return,
    }

    /// <summary>
    /// When the Character fights by itself: after <see cref="IdleDelay"/> seconds without player input it holds and
    /// attacks, walks to enemies within <see cref="Leash"/> of the spot the player left it (its anchor), and walks back
    /// when there are none. Any input hands control straight back.
    /// </summary>
    public sealed class AutoFight
    {
        public float IdleDelay { get; set; } = 1.5f;

        /// <summary>How far from its anchor the Character will chase.</summary>
        public float Leash { get; set; } = 6f;

        /// <summary>How close to its anchor counts as back.</summary>
        public float ReturnTolerance { get; set; } = 0.5f;

        public float SecondsIdle { get; private set; }

        public bool IsActive => SecondsIdle >= IdleDelay;

        /// <summary>Call every frame with whether the player gave input this frame.</summary>
        public void Tick(float seconds, bool playerInput)
        {
            SecondsIdle = playerInput ? 0f : SecondsIdle + seconds;
        }

        /// <param name="enemyDistance">Distance to the nearest living enemy, or null when there is none.</param>
        /// <param name="enemyDistanceFromAnchor">That enemy's distance from the anchor (ignored when there is none).</param>
        /// <param name="attackRange">The reach of the Character's weapons.</param>
        /// <param name="distanceFromAnchor">The Character's own distance from its anchor.</param>
        public AutoFightOrder Decide(float? enemyDistance, float enemyDistanceFromAnchor, float attackRange, float distanceFromAnchor)
        {
            if (!IsActive) return AutoFightOrder.PlayerControl;
            return Engagement.Decide(enemyDistance, enemyDistanceFromAnchor, attackRange, Leash, distanceFromAnchor, ReturnTolerance);
        }
    }
}
