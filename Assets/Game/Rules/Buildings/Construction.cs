using System;

namespace Isekai
{
    /// <summary>A Building going up: it finishes after <see cref="Seconds"/> of build time, faster with a higher build speed.</summary>
    public sealed class Construction
    {
        public float Seconds { get; }
        public float Elapsed { get; private set; }
        public float Progress => Seconds <= 0f ? 1f : Math.Min(1f, Elapsed / Seconds);
        public bool IsComplete => Progress >= 1f;

        public Construction(float seconds) => Seconds = Math.Max(0f, seconds);

        /// <summary>Advances the build; returns true on the tick it completes.</summary>
        public bool Tick(float seconds, float speed = 1f)
        {
            if (IsComplete || seconds <= 0f || speed <= 0f) return false;
            Elapsed += seconds * speed;
            return IsComplete;
        }
    }
}
