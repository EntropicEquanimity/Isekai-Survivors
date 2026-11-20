using System;
using Blondie.Core;

namespace Isekai
{
    /// <summary>Line of sight over a grid: a straight line between two cell centres, cut by blocking cells (rocks, walls, cliffs).</summary>
    public static class LineOfSight
    {
        /// <summary>
        /// True when no cell strictly between <paramref name="from"/> and <paramref name="to"/> blocks. The end cells
        /// never block themselves, so a viewer sees the face of the rock or wall in front of it.
        /// </summary>
        public static bool Clear(Int2 from, Int2 to, Func<Int2, bool> blocks)
        {
            int dx = Math.Abs(to.x - from.x), dy = Math.Abs(to.y - from.y);
            int sx = from.x < to.x ? 1 : -1, sy = from.y < to.y ? 1 : -1;
            int err = dx - dy;
            int x = from.x, y = from.y;
            while (true)
            {
                if (x == to.x && y == to.y) return true;
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x += sx; }
                if (e2 < dx) { err += dx; y += sy; }
                if (x == to.x && y == to.y) return true;
                if (blocks(new Int2(x, y))) return false;
            }
        }
    }
}
