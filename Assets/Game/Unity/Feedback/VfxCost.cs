using System.Diagnostics;
using Blondie.Core;

namespace Isekai
{
    /// <summary>What the visual effects cost: the time their handlers and updates spent, summed, so a test (or the perf overlay) can keep it small however many deaths a fight has.</summary>
    public static class VfxCost
    {
        private static long s_ticks;

        static VfxCost() => StaticReset.Register(() => s_ticks = 0);

        /// <summary>Total time spent so far, in milliseconds.</summary>
        public static double TotalMilliseconds => s_ticks * 1000.0 / Stopwatch.Frequency;

        internal static long Start() => Stopwatch.GetTimestamp();

        internal static void Stop(long start) => s_ticks += Stopwatch.GetTimestamp() - start;
    }
}
