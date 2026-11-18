using System;

namespace Isekai
{
    /// <summary>
    /// Turns mining time into whole Resource units: progress builds at a rate (units per second) and pays out each time
    /// it passes a whole unit, never more than the node has left. Fractions carry over between ticks.
    /// </summary>
    public sealed class MiningProgress
    {
        /// <summary>Progress toward the next unit, 0 to 1.</summary>
        public float Partial { get; private set; }

        /// <returns>The whole units mined this tick.</returns>
        public int Tick(float seconds, float unitsPerSecond, int remaining)
        {
            if (remaining <= 0 || seconds <= 0f || unitsPerSecond <= 0f) return 0;
            float total = Partial + seconds * unitsPerSecond;
            int whole = (int)Math.Floor(total);
            if (whole >= remaining)
            {
                Partial = 0f;
                return remaining;
            }
            Partial = total - whole;
            return whole;
        }

        /// <summary>Drops progress toward the next unit, e.g. when the miner walks away.</summary>
        public void Reset() => Partial = 0f;
    }
}
