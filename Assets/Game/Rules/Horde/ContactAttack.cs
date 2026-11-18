namespace Isekai
{
    /// <summary>Melee timing for a horde enemy: it may hit something in reach once every <see cref="Interval"/> seconds.</summary>
    public struct ContactAttack
    {
        public float Damage;
        public float Interval;
        public float Reach;

        /// <summary>The game time it may hit again.</summary>
        public float ReadyAt;

        public ContactAttack(float damage, float interval, float reach)
        {
            Damage = damage;
            Interval = interval;
            Reach = reach;
            ReadyAt = 0f;
        }

        /// <summary>True (and starts the cooldown) if the attack is ready at <paramref name="now"/>.</summary>
        public bool TryHit(float now)
        {
            if (now < ReadyAt) return false;
            ReadyAt = now + Interval;
            return true;
        }
    }
}
