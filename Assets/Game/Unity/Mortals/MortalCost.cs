using System.Diagnostics;
using Blondie.Core;

namespace Isekai
{
    /// <summary>What the mortals' brains cost: the time <see cref="RegionMortals"/> spent in its update (thinking, steering, senses), summed, so a test can check it stays small however many mortals there are.</summary>
    public static class MortalCost
    {
        private static long s_ticks;

        static MortalCost() => StaticReset.Register(() => s_ticks = 0);

        /// <summary>Total time spent so far, in milliseconds.</summary>
        public static double TotalMilliseconds => s_ticks * 1000.0 / Stopwatch.Frequency;

        internal static void Add(long ticks) => s_ticks += ticks;
    }
}
