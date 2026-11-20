using System;
using Blondie.Core;

namespace Isekai
{
    /// <summary>
    /// Picking where things appear on a Region's cell grid: candidate cells come from a shape (anywhere, a ring around a
    /// point, a band along the edges), and a cell is taken when it is walkable and passes the caller's test (far from
    /// the player, on corrupted ground, inside a settlement...). One place for every spawner instead of a copy each.
    /// </summary>
    public static class SpawnPlacement
    {
        /// <summary>
        /// Tries up to <paramref name="attempts"/> candidates from <paramref name="candidate"/> and returns the first
        /// that is walkable and accepted.
        /// </summary>
        public static bool TryPick(Random random, Func<Random, Int2> candidate, Func<Int2, bool> walkable, Func<Int2, bool> accept,
            out Int2 cell, int attempts = 30)
        {
            if (random == null) throw new ArgumentNullException(nameof(random));
            for (int i = 0; i < attempts; i++)
            {
                cell = candidate(random);
                if (walkable(cell) && (accept == null || accept(cell))) return true;
            }
            cell = default;
            return false;
        }

        /// <summary>Any cell of a <paramref name="width"/> × <paramref name="height"/> grid, keeping <paramref name="margin"/> cells off the edges.</summary>
        public static Func<Random, Int2> Anywhere(int width, int height, int margin = 0) =>
            r => new Int2(margin + r.Next(Math.Max(1, width - 2 * margin)), margin + r.Next(Math.Max(1, height - 2 * margin)));

        /// <summary>A cell between <paramref name="minRadius"/> and <paramref name="maxRadius"/> cells from <paramref name="centre"/>.</summary>
        public static Func<Random, Int2> Ring(Int2 centre, float minRadius, float maxRadius) => r =>
        {
            double angle = r.NextDouble() * Math.PI * 2.0;
            double radius = minRadius + (maxRadius - minRadius) * r.NextDouble();
            return new Int2(centre.x + (int)Math.Round(Math.Cos(angle) * radius), centre.y + (int)Math.Round(Math.Sin(angle) * radius));
        };

        /// <summary>A cell within <paramref name="band"/> cells of the grid's edge.</summary>
        public static Func<Random, Int2> EdgeBand(int width, int height, int band) => r =>
        {
            band = Math.Max(1, Math.Min(band, Math.Min(width, height) / 2));
            int side = r.Next(4), along = r.Next(side < 2 ? width : height), depth = r.Next(band);
            return side switch
            {
                0 => new Int2(along, depth),
                1 => new Int2(along, height - 1 - depth),
                2 => new Int2(depth, along),
                _ => new Int2(width - 1 - depth, along),
            };
        };

        /// <summary>A cell within <paramref name="radius"/> cells of <paramref name="centre"/> (a square, for clumps).</summary>
        public static Func<Random, Int2> Near(Int2 centre, int radius) =>
            r => new Int2(centre.x + r.Next(-radius, radius + 1), centre.y + r.Next(-radius, radius + 1));
    }
}
