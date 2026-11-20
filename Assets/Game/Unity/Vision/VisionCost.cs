using System.Diagnostics;
using Blondie.Core;

namespace Isekai
{
    /// <summary>
    /// What fog of war costs: the time its systems spent in their updates, summed, so a test (or the debug menu)
    /// can check it stays small however many units there are.
    /// </summary>
    public static class VisionCost
    {
        private static long s_ticks;

        static VisionCost() => StaticReset.Register(() => s_ticks = 0);

        /// <summary>Total time spent so far, in milliseconds.</summary>
        public static double TotalMilliseconds => s_ticks * 1000.0 / Stopwatch.Frequency;

        public static void Add(long ticks) => s_ticks += ticks;
    }
}
