using System.Collections.Generic;
using Blondie.Core;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// What each side sees of the Region. Every living unit with eyes (<see cref="VisionRanges"/>: Jack 14 cells,
    /// demons 8, mortals 7, wildlife 6) is a viewer on its side's <see cref="VisionMap"/>; a unit that stays in its
    /// cell costs a cell compare, and one that changes cell updates only the cells that entered or left its sight.
    /// Sight is cut by blocked ground: the "SightBlock" layer (rocks and cliffs a generator marks) and any cell the
    /// flow field blocks (rocks, walls and Buildings in the hand-built Region).
    /// <para>The result lives on grid layers, "Visible.&lt;Side&gt;" and "Explored.&lt;Side&gt;" (1 = yes), so overlays redraw
    /// only changed cells, and other systems ask <see cref="CanSee(Side, Int2)"/>. The Character's range and the
    /// others' scale with <see cref="RangeMultiplier"/> (night halves it).</para>
    /// </summary>
    [RegionSystem]
    public sealed class RegionVision : MonoBehaviour
    {
        private const int RefreshPerFrame = 2;

        private sealed class Eyes
        {
            public RegionEntity Unit;
            public VisionViewer Viewer;
            public Side Side;
            public int Seen;
            public int Index;
        }

        private static float s_multiplier = 1f;
        private static bool s_on = true;

        private readonly VisionMap[] _maps = new VisionMap[5];
        private readonly Dictionary<RegionEntity, Eyes> _eyes = new();
        private readonly List<Eyes> _list = new();
        private readonly List<Eyes> _gone = new();
        private RegionGrid _grid;
        private CellLayer _sightBlock;
        private int _frame;
        private int _refreshCursor;

        static RegionVision() => StaticReset.Register(() => { s_multiplier = 1f; s_on = true; });

        public static RegionVision Current { get; private set; }

        /// <summary>
        /// Scales every vision range (1 by day). Night halves it: the day-and-night cycle sets 0.5 here. Takes effect
        /// on the next frame, and only units whose scaled range changed look again.
        /// </summary>
        public static float RangeMultiplier
        {
            get => s_multiplier;
            set => s_multiplier = Mathf.Max(0.01f, value);
        }

        /// <summary>Fog of war is on. Off (the debug menu, a test measuring its cost) the Region is fully shown and nothing updates.</summary>
        public static bool IsOn
        {
            get => s_on;
            set => s_on = value;
        }

        /// <summary>The Region is baked and the viewers are being tracked.</summary>
        public bool IsReady => _grid != null;

        /// <summary>How many units are looking out.</summary>
        public int ViewerCount => _list.Count;

        private void OnEnable() => Current = this;

        private void OnDisable()
        {
            if (Current == this) Current = null;
            if (_grid != null) _grid.Baked -= RefreshAll;
        }

        /// <summary>Whether <paramref name="side"/> sees <paramref name="cell"/> right now. False before the Region is ready or for a side with no viewers yet.</summary>
        public bool CanSee(Side side, Int2 cell)
        {
            VisionMap map = _maps[(int)side];
            return map != null && map.IsVisible(cell);
        }

        /// <summary>Whether <paramref name="side"/> sees the ground under <paramref name="world"/>.</summary>
        public bool CanSee(Side side, Vector3 world) => _grid != null && CanSee(side, _grid.CellAt(world));

        /// <summary>Unseen, explored (seen before) or visible, for one side.</summary>
        public FogState StateOf(Side side, Int2 cell)
        {
            VisionMap map = _maps[(int)side];
            return map != null ? map.StateOf(cell) : FogState.Unseen;
        }

        /// <summary>The layer holding 1 for each cell a side sees now.</summary>
        public static string VisibleLayer(Side side) => "Visible." + side;

        /// <summary>The layer holding 1 for each cell a side has seen.</summary>
        public static string ExploredLayer(Side side) => "Explored." + side;

        private void Update()
        {
            if (!s_on) return;
            if (_grid == null && !TryStart()) return;
            _frame++;
            IReadOnlyList<RegionEntity> living = RegionEntities.Living;
            for (int i = 0; i < living.Count; i++) Track(living[i]);
            Sweep();
            RefreshSome();
        }

        private bool TryStart()
        {
            RegionGrid grid = RegionGrid.Current;
            if (grid == null || !grid.IsBaked) return false;
            _grid = grid;
            _sightBlock = grid.Layer(TerrainLayers.SightBlock);
            grid.Baked += RefreshAll;
            return true;
        }

        private bool Blocks(Int2 cell) => _sightBlock.Get(cell) != 0 || !_grid.IsWalkable(cell);

        private VisionMap MapFor(Side side)
        {
            VisionMap map = _maps[(int)side];
            if (map != null) return map;
            map = _maps[(int)side] = new VisionMap(_grid.Width, _grid.Height, Blocks);
            CellLayer visible = _grid.Layer(VisibleLayer(side)), explored = _grid.Layer(ExploredLayer(side));
            map.VisibleChanged += (cell, on) => visible.Set(cell, on ? (byte)1 : (byte)0);
            map.Explored += cell => explored.Set(cell, 1);
            return map;
        }

        private void Track(RegionEntity unit)
        {
            int baseRange = VisionRanges.For(unit.Kind);
            if (baseRange == 0) return;
            Side side = unit.Side;
            int range = VisionRanges.Scaled(baseRange, s_multiplier);
            Int2 cell = unit.Cell;
            if (!_eyes.TryGetValue(unit, out Eyes eyes))
            {
                eyes = new Eyes { Unit = unit, Side = side, Index = _list.Count, Viewer = MapFor(side).Add(cell, range) };
                _eyes[unit] = eyes;
                _list.Add(eyes);
            }
            else if (eyes.Side != side)
            {
                _maps[(int)eyes.Side].Remove(eyes.Viewer);   // a revolt: it looks out for another side now
                eyes.Side = side;
                eyes.Viewer = MapFor(side).Add(cell, range);
            }
            else _maps[(int)side].Move(eyes.Viewer, cell, range);
            eyes.Seen = _frame;
        }

        // Units that left the registry (died, despawned) take their sight with them.
        private void Sweep()
        {
            _gone.Clear();
            foreach (Eyes eyes in _list)
                if (eyes.Seen != _frame) _gone.Add(eyes);
            if (_gone.Count == 0) return;
            foreach (Eyes eyes in _gone)
            {
                _maps[(int)eyes.Side].Remove(eyes.Viewer);
                _eyes.Remove(eyes.Unit);
                int last = _list.Count - 1;
                _list[eyes.Index] = _list[last];
                _list[eyes.Index].Index = eyes.Index;
                _list.RemoveAt(last);
            }
        }

        // A rock mined or a wall built changes what a standing unit sees: a couple of viewers look again each frame.
        private void RefreshSome()
        {
            int n = _list.Count;
            if (n == 0) return;
            for (int k = 0; k < RefreshPerFrame && k < n; k++)
            {
                _refreshCursor = (_refreshCursor + 1) % n;
                Eyes eyes = _list[_refreshCursor];
                _maps[(int)eyes.Side].Refresh(eyes.Viewer);
            }
        }

        private void RefreshAll()
        {
            foreach (Eyes eyes in _list) _maps[(int)eyes.Side].Refresh(eyes.Viewer);
        }
    }
}
