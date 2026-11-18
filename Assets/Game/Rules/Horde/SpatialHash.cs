using System;
using System.Collections.Generic;

namespace Isekai
{
    /// <summary>
    /// A uniform grid of buckets for answering "what is near here?" over thousands of moving points. Rebuilt every frame
    /// (<see cref="Clear"/> then <see cref="Insert"/> each item); after warm-up it allocates nothing. Points only: an
    /// item's size is the caller's to add to the query radius.
    /// </summary>
    public sealed class SpatialHash<T>
    {
        private readonly float _cellSize;
        private readonly Dictionary<long, int> _heads = new();
        private T[] _items = new T[256];
        private float[] _xs = new float[256];
        private float[] _ys = new float[256];
        private int[] _next = new int[256];

        public SpatialHash(float cellSize)
        {
            if (cellSize <= 0f) throw new ArgumentOutOfRangeException(nameof(cellSize));
            _cellSize = cellSize;
        }

        public int Count { get; private set; }
        public float CellSize => _cellSize;

        public void Clear()
        {
            _heads.Clear();
            Array.Clear(_items, 0, Count);
            Count = 0;
        }

        public void Insert(T item, float x, float y)
        {
            if (Count == _items.Length) Grow();
            long key = Key(Cell(x), Cell(y));
            _items[Count] = item;
            _xs[Count] = x;
            _ys[Count] = y;
            _next[Count] = _heads.TryGetValue(key, out int head) ? head : -1;
            _heads[key] = Count;
            Count++;
        }

        /// <summary>Adds every item within <paramref name="radius"/> of (x, y) to <paramref name="results"/> (not cleared first).</summary>
        public void Query(float x, float y, float radius, List<T> results)
        {
            float r2 = radius * radius;
            int minX = Cell(x - radius), maxX = Cell(x + radius), minY = Cell(y - radius), maxY = Cell(y + radius);
            for (int cx = minX; cx <= maxX; cx++)
            for (int cy = minY; cy <= maxY; cy++)
            {
                if (!_heads.TryGetValue(Key(cx, cy), out int i)) continue;
                for (; i >= 0; i = _next[i])
                {
                    float dx = _xs[i] - x, dy = _ys[i] - y;
                    if (dx * dx + dy * dy <= r2) results.Add(_items[i]);
                }
            }
        }

        /// <summary>The nearest item within <paramref name="radius"/> that passes <paramref name="accept"/> (null = any).</summary>
        public bool TryNearest(float x, float y, float radius, Func<T, bool> accept, out T nearest, out float distance)
        {
            nearest = default;
            float best = radius * radius;
            bool found = false;
            int minX = Cell(x - radius), maxX = Cell(x + radius), minY = Cell(y - radius), maxY = Cell(y + radius);
            for (int cx = minX; cx <= maxX; cx++)
            for (int cy = minY; cy <= maxY; cy++)
            {
                if (!_heads.TryGetValue(Key(cx, cy), out int i)) continue;
                for (; i >= 0; i = _next[i])
                {
                    float dx = _xs[i] - x, dy = _ys[i] - y, d2 = dx * dx + dy * dy;
                    if (d2 > best || (accept != null && !accept(_items[i]))) continue;
                    best = d2;
                    nearest = _items[i];
                    found = true;
                }
            }
            distance = found ? (float)Math.Sqrt(best) : float.MaxValue;
            return found;
        }

        private int Cell(float v) => (int)Math.Floor(v / _cellSize);

        private static long Key(int cx, int cy) => ((long)cx << 32) ^ (uint)cy;

        private void Grow()
        {
            int size = _items.Length * 2;
            Array.Resize(ref _items, size);
            Array.Resize(ref _xs, size);
            Array.Resize(ref _ys, size);
            Array.Resize(ref _next, size);
        }
    }
}
