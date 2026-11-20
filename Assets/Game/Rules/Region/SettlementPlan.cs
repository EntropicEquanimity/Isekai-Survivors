using System;
using System.Collections.Generic;
using Blondie.Core;

namespace Isekai
{
    /// <summary>A rectangle of cells, both corners included.</summary>
    public readonly struct CellRect
    {
        public CellRect(int minX, int minY, int maxX, int maxY)
        {
            MinX = minX;
            MinY = minY;
            MaxX = maxX;
            MaxY = maxY;
        }

        public int MinX { get; }
        public int MinY { get; }
        public int MaxX { get; }
        public int MaxY { get; }

        public bool Contains(Int2 c) => c.x >= MinX && c.x <= MaxX && c.y >= MinY && c.y <= MaxY;

        public bool Overlaps(CellRect o) => MinX <= o.MaxX && MaxX >= o.MinX && MinY <= o.MaxY && MaxY >= o.MinY;

        public CellRect Inflate(int by) => new(MinX - by, MinY - by, MaxX + by, MaxY + by);
    }

    /// <summary>One Building of a placed settlement: what it is, and the lower-left cell and size of its footprint on the map.</summary>
    public readonly struct PlacedBuilding
    {
        public PlacedBuilding(MortalBuilding kind, Int2 origin)
        {
            Kind = kind;
            Origin = origin;
            Size = MortalBuildings.SizeOf(kind);
        }

        public MortalBuilding Kind { get; }
        public Int2 Origin { get; }
        public Int2 Size { get; }

        /// <summary>A fence piece rather than part of the settlement proper.</summary>
        public bool IsWall => MortalBuildings.IsWall(Kind);

        public bool Covers(Int2 c) => c.x >= Origin.x && c.x < Origin.x + Size.x && c.y >= Origin.y && c.y < Origin.y + Size.y;
    }

    /// <summary>A spawn marker on the map.</summary>
    public readonly struct PlacedMarker
    {
        public PlacedMarker(MarkerKind kind, Int2 cell)
        {
            Kind = kind;
            Cell = cell;
        }

        public MarkerKind Kind { get; }
        public Int2 Cell { get; }
    }

    /// <summary>A road end on the map: the cell the road starts from (outside the wall ring), the wall ring's gap, and whether a road uses it.</summary>
    public sealed class PlacedRoadEnd
    {
        public PlacedRoadEnd(Int2 edge, Int2 facing, Int2[] gap)
        {
            Edge = edge;
            Facing = facing;
            Gap = gap;
            Outside = new Int2(edge.x + facing.x * 2, edge.y + facing.y * 2);
        }

        /// <summary>The cell just inside the piece's edge.</summary>
        public Int2 Edge { get; }

        public Int2 Facing { get; }

        /// <summary>The wall ring's cells that stay open (or hold the gate) here.</summary>
        public Int2[] Gap { get; }

        /// <summary>The first cell outside the wall ring: where a road starts.</summary>
        public Int2 Outside { get; }

        /// <summary>A road leaves here.</summary>
        public bool InUse { get; internal set; }
    }

    /// <summary>One settlement placed on the map: a <see cref="SettlementPiece"/> at an origin, maybe mirrored, with its Buildings, spawn markers, road ends and ground patch in map cells.</summary>
    public sealed class PlacedSettlement
    {
        private readonly List<PlacedBuilding> _buildings = new();
        private readonly List<PlacedMarker> _markers = new();
        private readonly List<PlacedRoadEnd> _roadEnds = new();
        private readonly List<KeyValuePair<Int2, GroundTile>> _tiles = new();

        internal PlacedSettlement(int index, SettlementPiece piece, Int2 origin, bool flipX, bool flipY)
        {
            Index = index;
            Piece = piece;
            Origin = origin;
            FlipX = flipX;
            FlipY = flipY;
            Interior = new CellRect(origin.x, origin.y, origin.x + piece.Width - 1, origin.y + piece.Height - 1);
            Reserved = Interior.Inflate(1);

            foreach (BuildingSpot spot in piece.Spots) _buildings.Add(new PlacedBuilding(spot.Kind, MapBox(spot.Offset, spot.Size)));
            foreach (SpawnMarker m in piece.Markers) _markers.Add(new PlacedMarker(m.Kind, MapCell(m.Offset)));
            foreach (RoadEnd end in piece.RoadEnds)
            {
                var gap = new Int2[end.Gap.Length];
                for (int i = 0; i < gap.Length; i++) gap[i] = MapCell(end.Gap[i]);
                _roadEnds.Add(new PlacedRoadEnd(MapCell(end.Edge), new Int2(flipX ? -end.Facing.x : end.Facing.x, flipY ? -end.Facing.y : end.Facing.y), gap));
            }
            for (int y = 0; y < piece.Height; y++)
            for (int x = 0; x < piece.Width; x++)
            {
                GroundTile? tile = piece.TileAt(x, y);
                if (tile.HasValue) _tiles.Add(new KeyValuePair<Int2, GroundTile>(MapCell(new Int2(x, y)), tile.Value));
            }
        }

        /// <summary>Its place in the layout: the Seat is 0, then villages, farms and camps in the order they were placed.</summary>
        public int Index { get; }

        public SettlementPiece Piece { get; }
        public SettlementKind Kind => Piece.Kind;
        public bool IsSeat => Piece.Kind == SettlementKind.Seat;

        /// <summary>The map cell of the piece's lower-left corner (before mirroring: the lower-left of its rectangle).</summary>
        public Int2 Origin { get; }

        public bool FlipX { get; }
        public bool FlipY { get; }

        /// <summary>The cells of the piece's rectangle.</summary>
        public CellRect Interior { get; }

        /// <summary>The rectangle plus the ring of cells around it that the wall takes.</summary>
        public CellRect Reserved { get; }

        /// <summary>Its Buildings: the piece's spots first, then (once walls are placed) its fence.</summary>
        public IReadOnlyList<PlacedBuilding> Buildings => _buildings;

        public IReadOnlyList<PlacedMarker> Markers => _markers;
        public IReadOnlyList<PlacedRoadEnd> RoadEnds => _roadEnds;

        /// <summary>The ground patch: the cells it paints and with what.</summary>
        public IReadOnlyList<KeyValuePair<Int2, GroundTile>> Tiles => _tiles;

        /// <summary>A map cell for a cell of the piece (mirrored as placed).</summary>
        public Int2 MapCell(Int2 local) => new(
            Origin.x + (FlipX ? Piece.Width - 1 - local.x : local.x),
            Origin.y + (FlipY ? Piece.Height - 1 - local.y : local.y));

        // A footprint's lower-left on the map: mirrored, its lower-left comes from the far corner.
        private Int2 MapBox(Int2 local, Int2 size) => new(
            Origin.x + (FlipX ? Piece.Width - local.x - size.x : local.x),
            Origin.y + (FlipY ? Piece.Height - local.y - size.y : local.y));

        internal void AddWall(MortalBuilding kind, Int2 cell) => _buildings.Add(new PlacedBuilding(kind, cell));
    }

    /// <summary>A road: the cells from the road end of a settlement already joined (<see cref="From"/>) to one of the settlement it joins (<see cref="To"/>).</summary>
    public sealed class Road
    {
        public Road(PlacedSettlement from, PlacedSettlement to, Int2 start, Int2 end, List<Int2> cells)
        {
            From = from;
            To = to;
            Start = start;
            End = end;
            Cells = cells;
        }

        public PlacedSettlement From { get; }
        public PlacedSettlement To { get; }
        public Int2 Start { get; }
        public Int2 End { get; }

        /// <summary>The road's cells from <see cref="Start"/> to <see cref="End"/>, 4-connected.</summary>
        public IReadOnlyList<Int2> Cells { get; }
    }

    /// <summary>The steps a layout is made in, in the order the spec fixes (recorded as they happen so the order is testable).</summary>
    public enum LayoutStep { Seat, Villages, Farms, Camps, Roads, Walls, Rocks, Nodes, Wildlife }

    /// <summary>
    /// The mortal places of a generated Region, decided from its recipe alone: the Seat first (on the reserved spot, far
    /// from the start), then villages, farms and camps (3–10 in all on a full-size map), then roads between their road
    /// ends, then walls that wrap what is already placed. Pieces are mirrored and put where they fit; one that doesn't
    /// fit anywhere is left out (<see cref="Unplaced"/>), the Seat never is. Nothing overlaps: every Building, wall,
    /// road and ground cell belongs to one place.
    /// </summary>
    public sealed class SettlementPlan
    {
        /// <summary>The fewest villages, farms and camps a full-size map gets (the spec's 3–10).</summary>
        public const int MinCount = 3, MaxCount = 10;

        /// <summary>Empty cells kept between one settlement's ring and the next, the Seat's, the start's and the map's edge.</summary>
        public const int Spacing = 3;

        private const int Attempts = 80;

        private readonly List<PlacedSettlement> _settlements = new();
        private readonly List<SettlementKind> _unplaced = new();
        private readonly List<Road> _roads = new();
        private readonly List<LayoutStep> _steps = new();
        private readonly HashSet<Int2> _roadCells = new();
        private readonly int _width, _height;
        private readonly CellRect _start;
        private readonly RingRecipe _ring;

        private SettlementPlan(int width, int height, CellRect start, RingRecipe ring)
        {
            _width = width;
            _height = height;
            _start = start;
            _ring = ring;
        }

        /// <summary>Every settlement: the Seat first.</summary>
        public IReadOnlyList<PlacedSettlement> Settlements => _settlements;

        public PlacedSettlement Seat => _settlements[0];

        /// <summary>The kinds of settlement that found no room, in the order they were tried.</summary>
        public IReadOnlyList<SettlementKind> Unplaced => _unplaced;

        public IReadOnlyList<Road> Roads => _roads;

        /// <summary>The steps taken so far, in order.</summary>
        public IReadOnlyList<LayoutStep> Steps => _steps;

        /// <summary>Every cell a road runs over.</summary>
        public IReadOnlyCollection<Int2> RoadCells => _roadCells;

        /// <summary>True for a cell inside any settlement's rectangle or wall ring.</summary>
        public bool IsSettled(Int2 cell)
        {
            foreach (PlacedSettlement s in _settlements)
                if (s.Reserved.Contains(cell)) return true;
            return false;
        }

        /// <summary>True for a cell a settlement or road covers, or lies next to one: nothing else is placed there.</summary>
        public bool IsKeepOut(Int2 cell)
        {
            foreach (PlacedSettlement s in _settlements)
                if (s.Reserved.Inflate(1).Contains(cell)) return true;
            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
                if (_roadCells.Contains(new Int2(cell.x + dx, cell.y + dy))) return true;
            return false;
        }

        /// <summary>The Building (of any settlement) covering a cell, as (settlement, index in its Buildings); null when there is none.</summary>
        public (PlacedSettlement settlement, int building)? BuildingAt(Int2 cell)
        {
            foreach (PlacedSettlement s in _settlements)
            {
                if (!s.Reserved.Contains(cell)) continue;
                for (int i = 0; i < s.Buildings.Count; i++)
                    if (s.Buildings[i].Covers(cell)) return (s, i);
            }
            return null;
        }

        /// <summary>
        /// Lays out the places for a map. <paramref name="seat"/> is the Seat's reserved spot (the fort is built round
        /// it), <paramref name="start"/> the empty ground round Jack's start, which nothing may touch.
        /// </summary>
        public static SettlementPlan Generate(RegionRecipe recipe, Int2 seat, CellRect start)
        {
            var plan = new SettlementPlan(recipe.Width, recipe.Height, start, recipe.Ring);
            Random rnd = new(StableHash.Seed(recipe.Seed, "layout/settlements"));
            plan.PlaceSeat(seat);
            plan.PlaceOthers(rnd);
            plan.PlaceRoads();
            plan.PlaceWalls();
            return plan;
        }

        // The fort is mirrored so its gate faces the middle of the map; it goes round the spot, which is the courtyard's middle.
        private void PlaceSeat(Int2 seat)
        {
            SettlementPiece fort = SettlementPieces.SeatFor(_ring);
            bool flipY = seat.y < _height / 2;
            var settlement = new PlacedSettlement(0, fort, new Int2(seat.x - fort.Anchor.x, seat.y - fort.Anchor.y), false, flipY);
            _settlements.Add(settlement);
            _steps.Add(LayoutStep.Seat);
        }

        private void PlaceOthers(Random rnd)
        {
            // A map smaller than its ring's full size holds proportionally fewer places.
            double scale = (double)(_width * _height) / _ring.MapCells;
            int low = Math.Clamp((int)Math.Round(_ring.MinSettlements * scale), MinCount, _ring.MinSettlements);
            int high = Math.Clamp((int)Math.Round(_ring.MaxSettlements * scale), MinCount, _ring.MaxSettlements);
            int total = rnd.Next(low, Math.Max(low, high) + 1);
            int villages = Math.Max(1, (int)Math.Round(total * 0.4));
            int camps = Math.Max(1, (int)Math.Round(total * 0.25));
            int farms = Math.Max(0, total - villages - camps);

            Place(rnd, SettlementKind.Village, villages, LayoutStep.Villages);
            Place(rnd, SettlementKind.Farm, farms, LayoutStep.Farms);
            Place(rnd, SettlementKind.Camp, camps, LayoutStep.Camps);
        }

        private void Place(Random rnd, SettlementKind kind, int count, LayoutStep step)
        {
            SettlementPiece piece = SettlementPieces.For(kind);
            for (int i = 0; i < count; i++)
            {
                if (!TryPlace(rnd, piece)) _unplaced.Add(kind);
            }
            _steps.Add(step);
        }

        private bool TryPlace(Random rnd, SettlementPiece piece)
        {
            int ring = Spacing + 1;
            int maxX = _width - ring - piece.Width, maxY = _height - ring - piece.Height;
            if (maxX < ring || maxY < ring) return false;
            for (int attempt = 0; attempt < Attempts; attempt++)
            {
                var origin = new Int2(ring + rnd.Next(maxX - ring + 1), ring + rnd.Next(maxY - ring + 1));
                bool flipX = rnd.Next(2) == 0, flipY = rnd.Next(2) == 0;
                CellRect reserved = new CellRect(origin.x, origin.y, origin.x + piece.Width - 1, origin.y + piece.Height - 1).Inflate(1);
                if (!Fits(reserved)) continue;
                _settlements.Add(new PlacedSettlement(_settlements.Count, piece, origin, flipX, flipY));
                return true;
            }
            return false;
        }

        private bool Fits(CellRect reserved)
        {
            CellRect spaced = reserved.Inflate(Spacing);
            if (spaced.Overlaps(_start)) return false;
            foreach (PlacedSettlement s in _settlements)
                if (spaced.Overlaps(s.Reserved)) return false;
            return true;
        }

        // Roads join the Seat to everything else: each settlement not yet joined is linked, nearest first, to one that is.
        private void PlaceRoads()
        {
            var joined = new List<PlacedSettlement> { Seat };
            var waiting = new List<PlacedSettlement>(_settlements);
            waiting.RemoveAt(0);
            while (waiting.Count > 0)
            {
                PlacedSettlement bestTo = null, bestFrom = null;
                PlacedRoadEnd bestEnd = null, bestFromEnd = null;
                int best = int.MaxValue;
                foreach (PlacedSettlement w in waiting)
                foreach (PlacedSettlement j in joined)
                foreach (PlacedRoadEnd a in w.RoadEnds)
                foreach (PlacedRoadEnd b in j.RoadEnds)
                {
                    int d = Math.Abs(a.Outside.x - b.Outside.x) + Math.Abs(a.Outside.y - b.Outside.y);
                    if (d >= best) continue;
                    best = d;
                    bestTo = w;
                    bestFrom = j;
                    bestEnd = a;
                    bestFromEnd = b;
                }
                if (bestTo == null) break;
                List<Int2> cells = Route(bestFromEnd.Outside, bestEnd.Outside);
                waiting.Remove(bestTo);
                if (cells == null) continue;   // boxed in: it stays unjoined (its walls keep a gap anyway)
                bestEnd.InUse = true;
                bestFromEnd.InUse = true;
                foreach (Int2 c in cells) _roadCells.Add(c);
                _roads.Add(new Road(bestFrom, bestTo, bestFromEnd.Outside, bestEnd.Outside, cells));
                joined.Add(bestTo);
            }
            _steps.Add(LayoutStep.Roads);
        }

        // An L (along, then across; or across, then along) when one is clear, else the shortest way round.
        private List<Int2> Route(Int2 a, Int2 b)
        {
            foreach (bool horizontalFirst in new[] { true, false })
            {
                List<Int2> path = L(a, b, horizontalFirst);
                if (path != null) return path;
            }
            return Search(a, b);
        }

        private List<Int2> L(Int2 a, Int2 b, bool horizontalFirst)
        {
            var path = new List<Int2>();
            int x = a.x, y = a.y;
            path.Add(a);
            if (!Open(a) || !Open(b)) return null;
            for (int leg = 0; leg < 2; leg++)
            {
                bool horizontal = (leg == 0) == horizontalFirst;
                while (horizontal ? x != b.x : y != b.y)
                {
                    if (horizontal) x += Math.Sign(b.x - x); else y += Math.Sign(b.y - y);
                    var c = new Int2(x, y);
                    if (!Open(c)) return null;
                    path.Add(c);
                }
            }
            return path;
        }

        // A cell a road may cross: on the map, outside every ring and Jack's start.
        private bool Open(Int2 c)
        {
            if (c.x < 1 || c.y < 1 || c.x >= _width - 1 || c.y >= _height - 1 || _start.Contains(c)) return false;
            foreach (PlacedSettlement s in _settlements)
                if (s.Reserved.Contains(c)) return false;
            return true;
        }

        private List<Int2> Search(Int2 a, Int2 b)
        {
            if (!Open(a) || !Open(b)) return null;
            var from = new Dictionary<Int2, Int2> { [a] = a };
            var queue = new Queue<Int2>();
            queue.Enqueue(a);
            while (queue.Count > 0)
            {
                Int2 c = queue.Dequeue();
                if (c.Equals(b)) break;
                for (int k = 0; k < 4; k++)
                {
                    var n = new Int2(c.x + (k == 0 ? 1 : k == 1 ? -1 : 0), c.y + (k == 2 ? 1 : k == 3 ? -1 : 0));
                    if (from.ContainsKey(n) || !Open(n)) continue;
                    from[n] = c;
                    queue.Enqueue(n);
                }
            }
            if (!from.ContainsKey(b)) return null;
            var path = new List<Int2>();
            for (Int2 c = b; ; c = from[c])
            {
                path.Add(c);
                if (c.Equals(a)) break;
            }
            path.Reverse();
            return path;
        }

        // The fence goes round what is already there, open where a road leaves (a village with no road keeps one gap).
        private void PlaceWalls()
        {
            foreach (PlacedSettlement s in _settlements)
            {
                if (s.Piece.Wall == WallStyle.None) continue;
                var open = new HashSet<Int2>();
                bool anyUsed = false;
                foreach (PlacedRoadEnd end in s.RoadEnds) anyUsed |= end.InUse;
                for (int i = 0; i < s.RoadEnds.Count; i++)
                {
                    PlacedRoadEnd end = s.RoadEnds[i];
                    // The Seat's gate always stands; elsewhere a gap opens for a road, or for the first end when no road came.
                    if (!s.IsSeat && !end.InUse && (anyUsed || i > 0)) continue;
                    foreach (Int2 g in end.Gap) open.Add(g);
                }
                WrapWall(s, open);
            }
            _steps.Add(LayoutStep.Walls);
        }

        private static void WrapWall(PlacedSettlement s, HashSet<Int2> open)
        {
            CellRect ring = s.Reserved;
            bool castle = s.Piece.Wall == WallStyle.Castle;
            for (int x = ring.MinX; x <= ring.MaxX; x++)
            for (int y = ring.MinY; y <= ring.MaxY; y++)
            {
                if (x != ring.MinX && x != ring.MaxX && y != ring.MinY && y != ring.MaxY) continue;
                var cell = new Int2(x, y);
                if (open.Contains(cell)) continue;
                bool left = x == ring.MinX, right = x == ring.MaxX, top = y == ring.MaxY, bottom = y == ring.MinY;
                MortalBuilding kind;
                if ((left || right) && (top || bottom))
                    kind = castle ? MortalBuilding.CastleTower
                         : top ? (left ? MortalBuilding.PalisadeCornerLeft : MortalBuilding.PalisadeCornerRight)
                         : MortalBuilding.PalisadeWall;
                else if (top || bottom) kind = castle ? MortalBuilding.CastleWall : MortalBuilding.PalisadeWall;
                else kind = castle ? MortalBuilding.CastleWallVertical : MortalBuilding.PalisadeWallVertical;
                s.AddWall(kind, cell);
            }
        }
    }
}
