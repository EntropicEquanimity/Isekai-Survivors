using System.Collections.Generic;

namespace Isekai
{
    /// <summary>Shards, the meta currency: earned when a Run ends, won or lost, and spent on the <see cref="LegacyTree"/>.</summary>
    public static class MetaRewards
    {
        public const int PerRegion = 10;
        public const int PerDepth = 5;
        public const int ForWinning = 50;

        /// <summary>Each captured Region pays <see cref="PerRegion"/> plus <see cref="PerDepth"/> per ring in from the edge; winning adds <see cref="ForWinning"/>.</summary>
        public static int ShardsFor(IEnumerable<int> capturedDepths, bool won)
        {
            int shards = won ? ForWinning : 0;
            if (capturedDepths != null) foreach (int depth in capturedDepths) shards += PerRegion + PerDepth * depth;
            return shards;
        }

        /// <summary>The Shards a finished <paramref name="run"/> earns.</summary>
        public static int ShardsFor(PlanetRun run)
        {
            if (run == null) return 0;
            var depths = new List<int>();
            foreach (string id in run.CapturedInOrder) depths.Add(run.Planet[id].Depth);
            return ShardsFor(depths, run.State == PlanetRunState.Won);
        }
    }
}
