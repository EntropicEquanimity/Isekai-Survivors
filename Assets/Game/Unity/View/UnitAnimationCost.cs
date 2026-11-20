using System.Diagnostics;
using Blondie.Core;

namespace Isekai
{
    /// <summary>
    /// What the shared unit flipbook animator costs: time spent in its update, summed, so tests can keep it within
    /// budget with hundreds of units.
    /// </summary>
    public static class UnitAnimationCost
    {
        private static long s_ticks;

        static UnitAnimationCost() => StaticReset.Register(() => s_ticks = 0);

        /// <summary>Total time spent so far, in milliseconds.</summary>
        public static double TotalMilliseconds => s_ticks * 1000.0 / Stopwatch.Frequency;

        internal static void Add(long ticks) => s_ticks += ticks;
    }
}
