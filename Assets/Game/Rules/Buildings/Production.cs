using System;

namespace Isekai
{
    /// <summary>
    /// A Building's Unit production: one Unit every <see cref="Interval"/> seconds while fewer than <see cref="Cap"/> are
    /// alive. The timer waits at full while the cap is reached, so a replacement comes as soon as one dies.
    /// </summary>
    public sealed class Production
    {
        public float Interval { get; }
        public int Cap { get; }
        public float Timer { get; private set; }

        public Production(float interval, int cap)
        {
            if (interval <= 0f) throw new ArgumentOutOfRangeException(nameof(interval));
            if (cap < 0) throw new ArgumentOutOfRangeException(nameof(cap));
            Interval = interval;
            Cap = cap;
        }

        /// <returns>True when a Unit should be spawned now.</returns>
        public bool Tick(float seconds, int alive)
        {
            Timer = Math.Min(Interval, Timer + Math.Max(0f, seconds));
            if (alive >= Cap || Timer < Interval) return false;
            Timer = 0f;
            return true;
        }
    }
}
