using System;
using System.Collections.Generic;
using Blondie.Core;

namespace Isekai
{
    /// <summary>
    /// A path over the Region's cells for the bot to walk Jack along: A* with eight directions and no corner cutting, where
    /// cells beside a blocked cell cost more so the path keeps off the walls (a body has width). Engine-free; the map is a
    /// delegate saying which cells can be stood on.
    /// </summary>
    public static class GridPath
    {
        private const float Diagonal = 1.41421356f;
        private const float NearWallCost = 1.5f;

        private static readonly Int2[] Steps =
        {
            new(1, 0), new(-1, 0), new(0, 1), new(0, -1), new(1, 1), new(1, -1), new(-1, 1), new(-1, -1),
        };

        /// <summary>
        /// The cells from <paramref name="start"/> (left out) to <paramref name="goal"/> (included). False when the goal can't be
        /// stood on or can't be reached within <paramref name="maxNodes"/> cells explored.
        /// </summary>
        public static bool Find(int width, int height, Func<Int2, bool> walkable, Int2 start, Int2 goal, List<Int2> path, int maxNodes = 40000)
        {
            path.Clear();
            if (!Inside(width, height, start) || !Inside(width, height, goal) || !walkable(goal)) return false;
            if (start.Equals(goal)) return true;

            var g = new Dictionary<int, float>();
            var parent = new Dictionary<int, int>();
            var closed = new HashSet<int>();
            var open = new SortedSet<(float f, int key)>();
            int startKey = Key(width, start), goalKey = Key(width, goal);
            g[startKey] = 0f;
            open.Add((Heuristic(start, goal), startKey));
            int explored = 0;

            while (open.Count > 0)
            {
                (float _, int currentKey) = open.Min;
                open.Remove(open.Min);
                if (currentKey == goalKey) return Rebuild(width, parent, startKey, goalKey, path);
                if (!closed.Add(currentKey)) continue;
                if (++explored > maxNodes) return false;

                var current = new Int2(currentKey % width, currentKey / width);
                foreach (Int2 step in Steps)
                {
                    var next = new Int2(current.x + step.x, current.y + step.y);
                    if (!Inside(width, height, next) || !walkable(next)) continue;
                    bool diagonal = step.x != 0 && step.y != 0;
                    if (diagonal && (!walkable(new Int2(current.x + step.x, current.y)) || !walkable(new Int2(current.x, current.y + step.y)))) continue;
                    int nextKey = Key(width, next);
                    if (closed.Contains(nextKey)) continue;
                    float cost = g[currentKey] + (diagonal ? Diagonal : 1f) + (BesideWall(width, height, walkable, next) ? NearWallCost : 0f);
                    if (g.TryGetValue(nextKey, out float known) && known <= cost) continue;
                    if (g.TryGetValue(nextKey, out known)) open.Remove((known + Heuristic(next, goal), nextKey));
                    g[nextKey] = cost;
                    parent[nextKey] = currentKey;
                    open.Add((cost + Heuristic(next, goal), nextKey));
                }
            }
            return false;
        }

        /// <summary>The standable cell nearest <paramref name="cell"/> (itself when it is one) within <paramref name="radius"/>; false when there is none.</summary>
        public static bool NearestWalkable(int width, int height, Func<Int2, bool> walkable, Int2 cell, int radius, out Int2 found)
        {
            found = cell;
            if (Inside(width, height, cell) && walkable(cell)) return true;
            for (int r = 1; r <= radius; r++)
            {
                float best = float.MaxValue;
                bool any = false;
                for (int dx = -r; dx <= r; dx++)
                for (int dy = -r; dy <= r; dy++)
                {
                    if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != r) continue;
                    var c = new Int2(cell.x + dx, cell.y + dy);
                    if (!Inside(width, height, c) || !walkable(c)) continue;
                    float d = dx * dx + dy * dy;
                    if (d >= best) continue;
                    best = d;
                    found = c;
                    any = true;
                }
                if (any) return true;
            }
            return false;
        }

        private static bool Inside(int width, int height, Int2 c) => c.x >= 0 && c.y >= 0 && c.x < width && c.y < height;

        private static int Key(int width, Int2 c) => c.y * width + c.x;

        private static float Heuristic(Int2 a, Int2 b)
        {
            int dx = Math.Abs(a.x - b.x), dy = Math.Abs(a.y - b.y);
            return Math.Max(dx, dy) + (Diagonal - 1f) * Math.Min(dx, dy);
        }

        private static bool BesideWall(int width, int height, Func<Int2, bool> walkable, Int2 c)
        {
            for (int i = 0; i < 4; i++)
            {
                var n = new Int2(c.x + Steps[i].x, c.y + Steps[i].y);
                if (Inside(width, height, n) && !walkable(n)) return true;
            }
            return false;
        }

        private static bool Rebuild(int width, Dictionary<int, int> parent, int startKey, int goalKey, List<Int2> path)
        {
            for (int key = goalKey; key != startKey; key = parent[key]) path.Add(new Int2(key % width, key / width));
            path.Reverse();
            return true;
        }
    }

    /// <summary>
    /// Walks a point along waypoints: says which way to steer, moves on to the next waypoint once near it, and notices when
    /// it has stopped getting anywhere (stuck against something), so the bot can plan again.
    /// </summary>
    public sealed class PathWalker
    {
        private readonly List<(float x, float y)> _points = new();
        private int _next;
        private float _bestDistance = float.MaxValue;
        private float _stuckSeconds;

        /// <summary>How near a waypoint counts as reached.</summary>
        public float Reach { get; set; } = 0.6f;

        /// <summary>Seconds without getting closer to the waypoint before <see cref="IsStuck"/>.</summary>
        public float StuckAfter { get; set; } = 1.5f;

        public bool HasPath => _next < _points.Count;
        public bool IsStuck => _stuckSeconds >= StuckAfter;

        public void Clear()
        {
            _points.Clear();
            _next = 0;
            Reset();
        }

        public void Set(IEnumerable<(float x, float y)> waypoints)
        {
            _points.Clear();
            _points.AddRange(waypoints);
            _next = 0;
            Reset();
        }

        /// <summary>The unit direction to steer from (x, y); false (and zero) when there is no waypoint left.</summary>
        public bool Steer(float x, float y, float seconds, out float dx, out float dy)
        {
            dx = dy = 0f;
            while (_next < _points.Count)
            {
                float rx = _points[_next].x - x, ry = _points[_next].y - y;
                float d = (float)Math.Sqrt(rx * rx + ry * ry);
                bool last = _next == _points.Count - 1;
                if (d <= (last ? Reach * 0.5f : Reach))
                {
                    _next++;
                    Reset();
                    continue;
                }
                if (d < _bestDistance - 0.05f)
                {
                    _bestDistance = d;
                    _stuckSeconds = 0f;
                }
                else _stuckSeconds += seconds;
                dx = rx / d;
                dy = ry / d;
                return true;
            }
            return false;
        }

        private void Reset()
        {
            _bestDistance = float.MaxValue;
            _stuckSeconds = 0f;
        }
    }
}
