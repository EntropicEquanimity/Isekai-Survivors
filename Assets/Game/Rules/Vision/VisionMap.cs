using System;
using System.Collections.Generic;
using Blondie.Core;

namespace Isekai
{
    /// <summary>
    /// What one side sees of the Region: for every cell how many of its viewers see it now (so overlapping sight
    /// leaves a cell visible until the last of them looks away) and whether it was ever seen. A viewer that moves
    /// changes only the cells that left or entered its sight: <see cref="Move"/> does work for those and raises
    /// <see cref="VisibleChanged"/> only for cells that really went dark or lit.
    /// Engine-free: the Region feeds it a blocking test and the cells its units stand in.
    /// </summary>
    public sealed class VisionMap
    {
        private readonly int[] _counts;
        private readonly bool[] _explored;
        private readonly int[] _stamp;
        private readonly Func<Int2, bool> _blocks;
        private readonly Func<Int2, int> _groupOf;
        private List<int> _scratch = new();
        private int _generation = 1;

        /// <param name="groupOf">Optional: which blocking object a cell belongs to (0 for none). A viewer who sees any cell of
        /// one sees it whole (a 3x3 keep, not only its front row); see <see cref="LineOfSight.Clear(Int2, Int2, Func{Int2, bool}, Func{Int2, int})"/>.</param>
        public VisionMap(int width, int height, Func<Int2, bool> blocks, Func<Int2, int> groupOf = null)
        {
            _groupOf = groupOf;
            if (width < 1) throw new ArgumentOutOfRangeException(nameof(width));
            if (height < 1) throw new ArgumentOutOfRangeException(nameof(height));
            Width = width;
            Height = height;
            _blocks = blocks ?? (_ => false);
            _counts = new int[width * height];
            _explored = new bool[width * height];
            _stamp = new int[width * height];
        }

        public int Width { get; }
        public int Height { get; }

        /// <summary>A cell went from unseen-now to seen or back (not raised for a cell another viewer still sees).</summary>
        public event Action<Int2, bool> VisibleChanged;

        /// <summary>A cell was seen for the first time.</summary>
        public event Action<Int2> Explored;

        public bool Contains(Int2 cell) => (uint)cell.x < (uint)Width && (uint)cell.y < (uint)Height;

        public bool IsVisible(Int2 cell) => Contains(cell) && _counts[cell.x + cell.y * Width] > 0;

        public bool IsExplored(Int2 cell) => Contains(cell) && _explored[cell.x + cell.y * Width];

        public FogState StateOf(Int2 cell) => IsVisible(cell) ? FogState.Visible : IsExplored(cell) ? FogState.Explored : FogState.Unseen;

        /// <summary>A viewer standing in <paramref name="cell"/> seeing <paramref name="range"/> cells.</summary>
        public VisionViewer Add(Int2 cell, int range)
        {
            var viewer = new VisionViewer { Cell = cell, Range = range, IsActive = true };
            Apply(viewer, cell, range);
            return viewer;
        }

        /// <summary>Moves a viewer (and/or changes its range); only the cells entering or leaving its sight are touched. False when nothing changed.</summary>
        public bool Move(VisionViewer viewer, Int2 cell, int range)
        {
            if (!viewer.IsActive) return false;
            if (viewer.Cell.x == cell.x && viewer.Cell.y == cell.y && viewer.Range == range) return false;
            Apply(viewer, cell, range);
            return true;
        }

        /// <summary>Looks again from where it stands (a rock was mined, a wall went up).</summary>
        public void Refresh(VisionViewer viewer)
        {
            if (viewer.IsActive) Apply(viewer, viewer.Cell, viewer.Range);
        }

        /// <summary>Takes a viewer's whole sight away.</summary>
        public void Remove(VisionViewer viewer)
        {
            if (!viewer.IsActive) return;
            viewer.IsActive = false;
            foreach (int i in viewer.Cells) Leave(i);
            viewer.Cells.Clear();
        }

        private void Apply(VisionViewer viewer, Int2 cell, int range)
        {
            viewer.Cell = cell;
            viewer.Range = range;
            List<int> now = _scratch;
            now.Clear();
            Collect(cell, range, now);

            List<int> before = viewer.Cells;
            int keep = _generation + 1;
            foreach (int i in before) _stamp[i] = _generation;
            foreach (int i in now)
            {
                if (_stamp[i] == _generation) _stamp[i] = keep;   // seen before and now: nothing to do
                else Enter(i);
            }
            foreach (int i in before)
                if (_stamp[i] == _generation) Leave(i);
            _generation += 2;

            viewer.Cells = now;
            _scratch = before;
        }

        // Every cell within range of the origin that the origin has a clear line to.
        private void Collect(Int2 origin, int range, List<int> into)
        {
            if (range < 0 || !Contains(origin)) return;
            int r2 = range * range;
            int y0 = Math.Max(0, origin.y - range), y1 = Math.Min(Height - 1, origin.y + range);
            for (int y = y0; y <= y1; y++)
            {
                int dy = y - origin.y;
                int span = (int)Math.Sqrt(r2 - dy * dy);
                int x0 = Math.Max(0, origin.x - span), x1 = Math.Min(Width - 1, origin.x + span);
                for (int x = x0; x <= x1; x++)
                {
                    int dx = x - origin.x;
                    bool near = dx >= -1 && dx <= 1 && dy >= -1 && dy <= 1;
                    if (near || LineOfSight.Clear(origin, new Int2(x, y), _blocks, _groupOf)) into.Add(x + y * Width);
                }
            }
        }

        private void Enter(int i)
        {
            if (_counts[i]++ != 0) return;
            var cell = new Int2(i % Width, i / Width);
            VisibleChanged?.Invoke(cell, true);
            if (_explored[i]) return;
            _explored[i] = true;
            Explored?.Invoke(cell);
        }

        private void Leave(int i)
        {
            if (--_counts[i] != 0) return;
            VisibleChanged?.Invoke(new Int2(i % Width, i / Width), false);
        }
    }
}
