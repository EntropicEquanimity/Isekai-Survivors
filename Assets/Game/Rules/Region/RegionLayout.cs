using System;
using System.Collections.Generic;
using Blondie.Core;

namespace Isekai
{
    /// <summary>Names of the <see cref="CellLayer"/>s the terrain writes, for the systems that read them.</summary>
    public static class TerrainLayers
    {
        /// <summary>1 where a rock or a cliff cuts line of sight (vision reads this); 0 elsewhere. A rock mined out clears its cell.</summary>
        public const string SightBlock = "SightBlock";

        /// <summary>1 where wildlife may be spawned (<see cref="RegionLayout.WildlifeSpots"/>); 0 elsewhere.</summary>
        public const string WildlifeSpot = "WildlifeSpot";
    }

    /// <summary>The map's four corners: where Jack can start.</summary>
    public enum MapCorner
    {
        SouthWest,
        SouthEast,
        NorthWest,
        NorthEast,
    }

    /// <summary>
    /// Where everything on a generated Region's ground goes, decided from its <see cref="RegionRecipe"/> alone and with
    /// no engine: Jack and the Main Base (the Anchor) in one far corner, the Seat's fort in the opposite one, then
    /// villages, farms and camps with their roads and walls (<see cref="Settlements"/>), then rocks, cliffs, Stone and
    /// Wood nodes and wildlife spawn spots in the ground they leave (<see cref="PlacementOrder"/>). The same recipe lays out the same Region.
    /// Cells are map cells, (0,0) at the lower-left. Nothing lands on the start area, the Seat's area or the map's
    /// edge, every free cell can be walked to from the start, and nodes stand clear of everything so they never wall
    /// anything off. The Unity side (<c>RegionTerrain</c>) turns this into tiles, colliders and prefabs.
    /// </summary>
    public sealed class RegionLayout
    {
        /// <summary>Cells between the map's edge and the Anchor.</summary>
        public const int CornerInset = 6;

        /// <summary>The start area reaches this far (in cells, each way) from the Anchor; it stays empty.</summary>
        public const int StartClearRadius = 10;

        /// <summary>The smallest map a layout can be made for.</summary>
        public const int MinSize = 32;

        /// <summary>The Anchor (Main Base) is this many cells wide and tall.</summary>
        public const int AnchorSize = 2;

        private const int FullSize = 160 * 110;
        private const int StarterNodes = 3;
        private const int RockMargin = 1;
        private const int NodeMargin = 2;
        private const int NodeSpacing = 5;
        private const int WildlifeSpacing = 10;
        private const int WildlifeMinDistance = 20;
        private const int MaxAttempts = 6;

        private readonly bool[] _blocked;
        private readonly List<Int2> _rocks = new();
        private readonly List<Int2> _cliffs = new();
        private readonly List<Int2> _stone = new();
        private readonly List<Int2> _wood = new();
        private readonly List<Int2> _wildlife = new();

        private readonly SettlementPlan _plan;
        private readonly List<LayoutStep> _steps = new();

        private RegionLayout(RegionRecipe recipe, SettlementPlan plan)
        {
            Recipe = recipe;
            Width = recipe.Width;
            Height = recipe.Height;
            _blocked = new bool[Width * Height];

            Random corners = new(StableHash.Seed(recipe.Seed, "layout/corner"));
            Corner = (MapCorner)corners.Next(4);
            bool east = Corner == MapCorner.SouthEast || Corner == MapCorner.NorthEast;
            bool north = Corner == MapCorner.NorthWest || Corner == MapCorner.NorthEast;
            Interior = new Int2(east ? -1 : 1, north ? -1 : 1);
            Anchor = new Int2(east ? Width - CornerInset - AnchorSize : CornerInset, north ? Height - CornerInset - AnchorSize : CornerInset);
            Character = new Int2(Anchor.x + (east ? -2 : AnchorSize + 1), Anchor.y + (north ? -2 : AnchorSize + 1));
            // The far corner, mirrored through the map's centre from the Anchor; a bigger Seat sits further in so its walls stay on the map.
            int inset = Math.Max(CornerInset, recipe.Ring.SeatSize / 2 + 1);
            int mirroredX = Width - 1 - Anchor.x, mirroredY = Height - 1 - Anchor.y;
            Seat = new Int2(east ? Math.Max(mirroredX, inset) : Math.Min(mirroredX, Width - 1 - inset),
                north ? Math.Max(mirroredY, inset) : Math.Min(mirroredY, Height - 1 - inset));
            SeatClearRadius = recipe.Ring.SeatSize / 2 + 3;
            // The Seat, villages, farms and camps, roads and walls come first; the rocks, nodes and wildlife that follow keep clear of them.
            _plan = plan ?? SettlementPlan.Generate(recipe, Seat, new CellRect(
                Anchor.x - StartClearRadius, Anchor.y - StartClearRadius, Anchor.x + AnchorSize - 1 + StartClearRadius, Anchor.y + AnchorSize - 1 + StartClearRadius));
            _steps.AddRange(_plan.Steps);
        }

        public RegionRecipe Recipe { get; }

        /// <summary>How far the empty ground reserved around the Seat spot reaches (a bigger Seat reserves more).</summary>
        public int SeatClearRadius { get; }

        public int Width { get; }
        public int Height { get; }

        /// <summary>The corner Jack starts in.</summary>
        public MapCorner Corner { get; }

        /// <summary>Which way is into the map from the start corner: (1,1) from the south-west, (-1,-1) from the north-east.</summary>
        public Int2 Interior { get; }

        /// <summary>The lower-left cell of the Main Base's footprint (<see cref="AnchorSize"/> cells square).</summary>
        public Int2 Anchor { get; }

        /// <summary>The cell Jack's Character starts on, a few cells in from the Anchor.</summary>
        public Int2 Character { get; }

        /// <summary>The reserved spot for the Seat, mirrored through the map's centre from the Anchor: the far side from the start.</summary>
        public Int2 Seat { get; }

        /// <summary>The mortal places: the Seat's fort, villages, farms and camps, their roads and walls.</summary>
        public SettlementPlan Settlements => _plan;

        /// <summary>The steps the layout was made in, in order: Seat, villages, farms, camps, roads, walls, rocks, nodes, wildlife.</summary>
        public IReadOnlyList<LayoutStep> PlacementOrder => _steps;

        public IReadOnlyList<Int2> Rocks => _rocks;
        public IReadOnlyList<Int2> Cliffs => _cliffs;
        public IReadOnlyList<Int2> StoneNodes => _stone;
        public IReadOnlyList<Int2> WoodNodes => _wood;

        /// <summary>Open ground away from the start and the Seat where wildlife may be spawned (ticket: wildlife units).</summary>
        public IReadOnlyList<Int2> WildlifeSpots => _wildlife;

        public bool Contains(Int2 cell) => (uint)cell.x < (uint)Width && (uint)cell.y < (uint)Height;

        /// <summary>A rock or a cliff stands here (both stop movement and cut sight).</summary>
        public bool IsBlocked(Int2 cell) => Contains(cell) && _blocked[cell.x + cell.y * Width];

        /// <summary>The empty ground around the Anchor and the Character.</summary>
        public bool InStartArea(Int2 cell) =>
            cell.x >= Anchor.x - StartClearRadius && cell.x <= Anchor.x + AnchorSize - 1 + StartClearRadius &&
            cell.y >= Anchor.y - StartClearRadius && cell.y <= Anchor.y + AnchorSize - 1 + StartClearRadius;

        /// <summary>The empty ground reserved around the Seat spot.</summary>
        public bool InSeatArea(Int2 cell) => Math.Abs(cell.x - Seat.x) <= SeatClearRadius && Math.Abs(cell.y - Seat.y) <= SeatClearRadius;

        /// <summary>Makes the layout for <paramref name="recipe"/>: always the same one for the same recipe.</summary>
        public static RegionLayout Generate(RegionRecipe recipe)
        {
            if (recipe == null) throw new ArgumentNullException(nameof(recipe));
            if (recipe.Width < MinSize || recipe.Height < MinSize)
                throw new ArgumentOutOfRangeException(nameof(recipe), $"A Region layout needs a map of at least {MinSize}x{MinSize} cells, not {recipe.Width}x{recipe.Height}.");
            // A try that walls the Seat off (or most of the map) is thrown away and tried with another roll; the last
            // try has no cliffs and half the rocks, which cannot.
            SettlementPlan plan = null;
            for (int attempt = 0; attempt < MaxAttempts; attempt++)
            {
                var layout = new RegionLayout(recipe, plan);
                plan = layout._plan;
                layout.Fill(attempt, lean: false);
                if (layout.IsOpen()) return layout;
            }
            var last = new RegionLayout(recipe, plan);
            last.Fill(MaxAttempts, lean: true);
            return last;
        }

        /// <summary>True when <paramref name="to"/> can be walked to from <paramref name="from"/> over cells without a rock or a cliff.</summary>
        public bool CanWalk(Int2 from, Int2 to)
        {
            if (!Contains(from) || !Contains(to) || IsBlocked(from) || IsBlocked(to)) return false;
            return Reach(from)[to.x + to.y * Width];
        }

        private void Fill(int attempt, bool lean)
        {
            Random rnd = new(StableHash.Seed(Recipe.Seed, "layout/" + attempt));
            int area = Width * Height;
            PlaceRocks(rnd, (int)(area * (lean ? 0.006 : 0.012)));
            if (!lean) PlaceCliffs(rnd, (int)(area * 0.008));
            _steps.Add(LayoutStep.Rocks);
            bool[] reach = Reach(Character);
            PlaceNodes(rnd, reach);
            _steps.Add(LayoutStep.Nodes);
            PlaceWildlife(rnd, reach);
            _steps.Add(LayoutStep.Wildlife);
        }

        private bool IsOpen()
        {
            bool[] reach = Reach(Character);
            if (!reach[Seat.x + Seat.y * Width]) return false;
            int free = 0, reached = 0;
            for (int i = 0; i < _blocked.Length; i++)
            {
                if (_blocked[i]) continue;
                free++;
                if (reach[i]) reached++;
            }
            return reached >= free * 0.97;
        }

        private bool CanHold(Int2 cell, int margin) =>
            cell.x >= margin && cell.y >= margin && cell.x < Width - margin && cell.y < Height - margin &&
            !InStartArea(cell) && !InSeatArea(cell) && !IsBlocked(cell) && !_plan.IsKeepOut(cell);

        private void Block(Int2 cell, List<Int2> into)
        {
            _blocked[cell.x + cell.y * Width] = true;
            into.Add(cell);
        }

        // Rocks come in small clumps grown cell by cell from a random start.
        private void PlaceRocks(Random rnd, int target)
        {
            int guard = target * 20 + 100;
            while (_rocks.Count < target && guard-- > 0)
            {
                var at = new Int2(rnd.Next(Width), rnd.Next(Height));
                int clump = 1 + rnd.Next(8);
                for (int i = 0; i < clump && _rocks.Count < target; i++)
                {
                    if (CanHold(at, RockMargin)) Block(at, _rocks);
                    at = Step(at, rnd);
                }
            }
        }

        // Cliffs are short ridges (straight with the odd jog) that leave gaps to walk through.
        private void PlaceCliffs(Random rnd, int target)
        {
            int guard = target * 20 + 100;
            while (_cliffs.Count < target && guard-- > 0)
            {
                var at = new Int2(rnd.Next(Width), rnd.Next(Height));
                bool horizontal = rnd.Next(2) == 0;
                int length = 5 + rnd.Next(10);
                for (int i = 0; i < length && _cliffs.Count < target; i++)
                {
                    if (rnd.NextDouble() >= 0.12 && CanHold(at, RockMargin)) Block(at, _cliffs);
                    at = horizontal ? new Int2(at.x + 1, at.y + (rnd.NextDouble() < 0.15 ? rnd.Next(3) - 1 : 0))
                                    : new Int2(at.x + (rnd.NextDouble() < 0.15 ? rnd.Next(3) - 1 : 0), at.y + 1);
                }
            }
        }

        private static Int2 Step(Int2 at, Random rnd)
        {
            switch (rnd.Next(4))
            {
                case 0: return new Int2(at.x + 1, at.y);
                case 1: return new Int2(at.x - 1, at.y);
                case 2: return new Int2(at.x, at.y + 1);
                default: return new Int2(at.x, at.y - 1);
            }
        }

        // A node stands on a reachable cell whose eight neighbours are all free, and away from the other nodes, so it
        // can never wall a corridor off. A few of each kind lie near the start, for the first minutes' economy.
        private void PlaceNodes(Random rnd, bool[] reach)
        {
            List<Int2> candidates = Candidates(rnd, reach, NodeMargin, NodeFits);
            int stoneTotal = Math.Max(StarterNodes, Width * Height / (FullSize / 16));
            int woodTotal = Math.Max(StarterNodes, Width * Height / (FullSize / 20));
            PlaceStarters(candidates, _stone, StarterNodes);
            PlaceStarters(candidates, _wood, StarterNodes);
            PlaceMore(candidates, _stone, stoneTotal);
            PlaceMore(candidates, _wood, woodTotal);
        }

        private bool NodeFits(Int2 cell)
        {
            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
                if (IsBlocked(new Int2(cell.x + dx, cell.y + dy))) return false;
            return true;
        }

        private void PlaceStarters(List<Int2> candidates, List<Int2> into, int count)
        {
            // Near, but outside the empty start area: the closest fitting cells to the area's edge band.
            int low = StartClearRadius + 1, high = StartClearRadius + 14;
            for (int pass = 0; pass < 2 && into.Count < count; pass++)
            {
                foreach (Int2 c in candidates)
                {
                    if (into.Count >= count) break;
                    int d = DistanceToAnchor(c);
                    if (pass == 0 && (d < low || d > high)) continue;
                    TryNode(c, into);
                }
            }
        }

        private void PlaceMore(List<Int2> candidates, List<Int2> into, int total)
        {
            foreach (Int2 c in candidates)
            {
                if (into.Count >= total) break;
                TryNode(c, into);
            }
        }

        private bool TryNode(Int2 cell, List<Int2> into)
        {
            if (IsBlocked(cell) || !NodeFits(cell) || NearNode(cell, _stone) || NearNode(cell, _wood)) return false;
            into.Add(cell);
            return true;
        }

        private static bool NearNode(Int2 cell, List<Int2> nodes)
        {
            foreach (Int2 n in nodes)
                if (Math.Max(Math.Abs(n.x - cell.x), Math.Abs(n.y - cell.y)) < NodeSpacing) return true;
            return false;
        }

        private void PlaceWildlife(Random rnd, bool[] reach)
        {
            List<Int2> candidates = Candidates(rnd, reach, NodeMargin, c => DistanceToAnchor(c) >= WildlifeMinDistance && !IsBlocked(c) && !NearNode(c, _stone) && !NearNode(c, _wood));
            int target = Math.Max(3, Width * Height / 1800);
            foreach (Int2 c in candidates)
            {
                if (_wildlife.Count >= target) break;
                bool clear = true;
                foreach (Int2 w in _wildlife)
                    if (Math.Max(Math.Abs(w.x - c.x), Math.Abs(w.y - c.y)) < WildlifeSpacing) { clear = false; break; }
                if (clear) _wildlife.Add(c);
            }
        }

        // Reachable free cells outside the start and Seat areas that pass the filter, in a random order.
        private List<Int2> Candidates(Random rnd, bool[] reach, int margin, Func<Int2, bool> filter)
        {
            var list = new List<Int2>();
            for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                var c = new Int2(x, y);
                if (reach[x + y * Width] && CanHold(c, margin) && filter(c)) list.Add(c);
            }
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rnd.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
            return list;
        }

        private int DistanceToAnchor(Int2 c) => Math.Max(Math.Abs(c.x - Anchor.x), Math.Abs(c.y - Anchor.y));

        // Flood fill over cells without a rock, cliff or node, 4-connected.
        private bool[] Reach(Int2 from)
        {
            var seen = new bool[Width * Height];
            if (!Contains(from) || IsBlocked(from)) return seen;
            var queue = new Queue<Int2>();
            seen[from.x + from.y * Width] = true;
            queue.Enqueue(from);
            while (queue.Count > 0)
            {
                Int2 c = queue.Dequeue();
                for (int k = 0; k < 4; k++)
                {
                    var n = new Int2(c.x + (k == 0 ? 1 : k == 1 ? -1 : 0), c.y + (k == 2 ? 1 : k == 3 ? -1 : 0));
                    if (!Contains(n) || seen[n.x + n.y * Width] || _blocked[n.x + n.y * Width]) continue;
                    seen[n.x + n.y * Width] = true;
                    queue.Enqueue(n);
                }
            }
            return seen;
        }
    }
}
