namespace Isekai
{
    /// <summary>Every tunable number of the corruption rules, in one place.</summary>
    public static class CorruptionTuning
    {
        /// <summary>The highest level a cell reaches.</summary>
        public const int MaxLevel = 4;

        /// <summary>Buildings need every footprint cell at this level or higher.</summary>
        public const int BuildLevel = 2;

        /// <summary>Chance that a dying demon of yours raises its cell one level.</summary>
        public const float DyingDemonChance = 0.25f;

        /// <summary>Damage dealt on cells below <see cref="BuildLevel"/>: about 20% weaker.</summary>
        public const float WeakMultiplier = 0.8f;

        /// <summary>A priest near its settlement lowers a cell one level this often (seconds).</summary>
        public const float PurifySeconds = 20f;

        /// <summary>The Anchor's ground at Region start: cells around it, and their level.</summary>
        public const int AnchorSeedRadius = 10;
        public const int AnchorSeedLevel = 3;

        /// <summary>The Anchor spreads far and fast.</summary>
        public static readonly CorruptionSource Anchor = new(radius: 12, secondsPerStep: 4f);

        /// <summary>An Outpost takes ground further out, more slowly.</summary>
        public static readonly CorruptionSource Outpost = new(radius: 8, secondsPerStep: 6f);

        /// <summary>A Demon King's body: a small patch around it.</summary>
        public static readonly CorruptionSource Body = new(radius: 3, secondsPerStep: 8f);
    }
}
