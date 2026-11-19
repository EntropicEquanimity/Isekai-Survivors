using System;
using System.Collections.Generic;
using Blondie.Core;

namespace Isekai
{
    /// <summary>The mortal places a Region generates (see CONTEXT.md: Seat, Settlement).</summary>
    public enum SettlementKind
    {
        /// <summary>The Region's seat of power: a fort around a keep. Destroying its keep captures the Region.</summary>
        Seat,
        Village,
        Farm,
        Camp,
    }

    /// <summary>What a mortal Building is, so the Unity side can pick its prefab and the rules can size and count it.</summary>
    public enum MortalBuilding
    {
        Keep,
        CastleTower,
        CastleGate,
        CastleWall,
        CastleWallVertical,
        PalisadeWall,
        PalisadeWallVertical,
        PalisadeCornerLeft,
        PalisadeCornerRight,
        TownHall,
        PeasantHouse,
        Farmhouse,
        CropField,
        Tent,
        Watchtower,
    }

    /// <summary>The fence a settlement is wrapped in (placed last, once everything inside it is).</summary>
    public enum WallStyle { None, Palisade, Castle }

    /// <summary>Who a spawn marker is for (the mortal units themselves arrive with the mortal units ticket).</summary>
    public enum MarkerKind { Peasant, Guard, Priest }

    /// <summary>The ground a piece's tile patch paints (grass is the default under everything).</summary>
    public enum GroundTile { Path }

    /// <summary>Facts about each mortal Building kind that the rules need: its footprint, and whether it is part of the fence.</summary>
    public static class MortalBuildings
    {
        public static Int2 SizeOf(MortalBuilding kind) => kind switch
        {
            MortalBuilding.Keep => new Int2(3, 3),
            MortalBuilding.TownHall => new Int2(3, 3),
            MortalBuilding.CropField => new Int2(3, 3),
            MortalBuilding.PeasantHouse => new Int2(2, 2),
            MortalBuilding.Farmhouse => new Int2(2, 2),
            MortalBuilding.Tent => new Int2(2, 2),
            MortalBuilding.CastleGate => new Int2(2, 1),
            _ => new Int2(1, 1),
        };

        /// <summary>A fence piece (a wall, a palisade, a tower or gate in a castle's wall): it defends a settlement but is not the settlement.</summary>
        public static bool IsWall(MortalBuilding kind) => kind switch
        {
            MortalBuilding.CastleTower or MortalBuilding.CastleGate or MortalBuilding.CastleWall or MortalBuilding.CastleWallVertical
                or MortalBuilding.PalisadeWall or MortalBuilding.PalisadeWallVertical
                or MortalBuilding.PalisadeCornerLeft or MortalBuilding.PalisadeCornerRight => true,
            _ => false,
        };
    }

    /// <summary>A Building spot in a piece: what stands there and the lower-left cell of its footprint, relative to the piece's lower-left. May lie in the wall ring around the piece (the Seat's gate).</summary>
    public readonly struct BuildingSpot
    {
        public BuildingSpot(MortalBuilding kind, int x, int y)
        {
            Kind = kind;
            Offset = new Int2(x, y);
        }

        public MortalBuilding Kind { get; }
        public Int2 Offset { get; }
        public Int2 Size => MortalBuildings.SizeOf(Kind);
    }

    /// <summary>Where a mortal unit of some kind will start (the units come with the mortal units ticket).</summary>
    public readonly struct SpawnMarker
    {
        public SpawnMarker(MarkerKind kind, int x, int y)
        {
            Kind = kind;
            Offset = new Int2(x, y);
        }

        public MarkerKind Kind { get; }
        public Int2 Offset { get; }
    }

    /// <summary>
    /// Where a road leaves a piece: <see cref="Edge"/> is the cell inside the piece's edge, <see cref="Facing"/> the way
    /// out, and <see cref="Gap"/> the cells of the wall ring that stay open (or hold a gate) for the road to pass.
    /// </summary>
    public readonly struct RoadEnd
    {
        public RoadEnd(int edgeX, int edgeY, int facingX, int facingY, params Int2[] gap)
        {
            Edge = new Int2(edgeX, edgeY);
            Facing = new Int2(facingX, facingY);
            Gap = gap;
        }

        public Int2 Edge { get; }
        public Int2 Facing { get; }
        public Int2[] Gap { get; }
    }

    /// <summary>
    /// A handcrafted piece of a Region, as data: a patch of ground tiles, Building spots, spawn markers and road ends,
    /// inside a rectangle of <see cref="Width"/> by <see cref="Height"/> cells (lower-left at 0,0). The wall, when it
    /// has one, goes in the ring of cells around the rectangle. The generator places pieces, varying where they go and
    /// how they face, so the layout varies while each place stays designed.
    /// </summary>
    public sealed class SettlementPiece
    {
        private readonly string[] _tiles;

        /// <param name="tileRows">The ground patch drawn top row first, one char per cell: 'p' a path tile, anything else leaves the grass.</param>
        public SettlementPiece(string name, SettlementKind kind, int width, int height, WallStyle wall, string[] tileRows,
            BuildingSpot[] spots, SpawnMarker[] markers, RoadEnd[] roadEnds)
        {
            Name = name;
            Kind = kind;
            Width = width;
            Height = height;
            Wall = wall;
            _tiles = tileRows ?? Array.Empty<string>();
            Spots = spots ?? Array.Empty<BuildingSpot>();
            Markers = markers ?? Array.Empty<SpawnMarker>();
            RoadEnds = roadEnds ?? Array.Empty<RoadEnd>();
        }

        public string Name { get; }
        public SettlementKind Kind { get; }
        public int Width { get; }
        public int Height { get; }
        public WallStyle Wall { get; }
        public IReadOnlyList<BuildingSpot> Spots { get; }
        public IReadOnlyList<SpawnMarker> Markers { get; }
        public IReadOnlyList<RoadEnd> RoadEnds { get; }

        /// <summary>The ground tile the patch paints at a cell of the piece, or null where it leaves the grass.</summary>
        public GroundTile? TileAt(int x, int y)
        {
            int row = Height - 1 - y;
            if (row < 0 || row >= _tiles.Length || x < 0 || x >= _tiles[row].Length) return null;
            return _tiles[row][x] == 'p' ? GroundTile.Path : null;
        }
    }

    /// <summary>The small set of pieces the outer ring is made from.</summary>
    public static class SettlementPieces
    {
        /// <summary>
        /// A fort around the keep: stone walls with a gate on the south side (flipped to face the map's middle), the
        /// keep at the far end, guards in the courtyard. The Seat's reserved spot is the courtyard's centre cell.
        /// </summary>
        public static SettlementPiece SeatFort { get; } = new SettlementPiece(
            "Seat fort", SettlementKind.Seat, 11, 11, WallStyle.Castle,
            new[]
            {
                "ppppppppppp",
                "ppppppppppp",
                "ppppppppppp",
                "ppppppppppp",
                "ppppppppppp",
                "ppppppppppp",
                "ppppppppppp",
                "ppppppppppp",
                "ppppppppppp",
                "ppppppppppp",
                "ppppppppppp",
            },
            new[]
            {
                new BuildingSpot(MortalBuilding.Keep, 4, 7),
                new BuildingSpot(MortalBuilding.CastleGate, 5, -1),
                new BuildingSpot(MortalBuilding.Watchtower, 1, 1),
                new BuildingSpot(MortalBuilding.Watchtower, 9, 1),
            },
            new[]
            {
                new SpawnMarker(MarkerKind.Guard, 3, 5),
                new SpawnMarker(MarkerKind.Guard, 7, 5),
                new SpawnMarker(MarkerKind.Guard, 4, 3),
                new SpawnMarker(MarkerKind.Guard, 6, 3),
                new SpawnMarker(MarkerKind.Guard, 2, 9),
                new SpawnMarker(MarkerKind.Guard, 8, 9),
                new SpawnMarker(MarkerKind.Priest, 5, 6),
            },
            new[] { new RoadEnd(5, 0, 0, -1, new Int2(5, -1), new Int2(6, -1)) });

        /// <summary>The cell of the Seat fort (relative to its lower-left) that sits on the Seat's reserved spot: the courtyard's centre.</summary>
        public static Int2 SeatAnchor => new Int2(5, 5);

        /// <summary>A walled village: a town hall among peasant houses, a road in on two sides.</summary>
        public static SettlementPiece Village { get; } = new SettlementPiece(
            "Village", SettlementKind.Village, 12, 10, WallStyle.Palisade,
            new[]
            {
                "............",
                "............",
                "............",
                "...ppppppp..",
                "...ppppppp..",
                ".....pp.....",
                ".....pp.....",
                ".....pp.....",
                ".....pp.....",
                ".....pp.....",
            },
            new[]
            {
                new BuildingSpot(MortalBuilding.TownHall, 4, 4),
                new BuildingSpot(MortalBuilding.PeasantHouse, 1, 6),
                new BuildingSpot(MortalBuilding.PeasantHouse, 8, 6),
                new BuildingSpot(MortalBuilding.PeasantHouse, 1, 1),
                new BuildingSpot(MortalBuilding.PeasantHouse, 9, 1),
            },
            new[]
            {
                new SpawnMarker(MarkerKind.Peasant, 3, 5),
                new SpawnMarker(MarkerKind.Peasant, 7, 5),
                new SpawnMarker(MarkerKind.Peasant, 3, 3),
                new SpawnMarker(MarkerKind.Peasant, 8, 3),
                new SpawnMarker(MarkerKind.Guard, 5, 1),
                new SpawnMarker(MarkerKind.Guard, 11, 5),
                new SpawnMarker(MarkerKind.Priest, 5, 3),
            },
            new[]
            {
                new RoadEnd(5, 0, 0, -1, new Int2(5, -1)),
                new RoadEnd(11, 4, 1, 0, new Int2(12, 4)),
            });

        /// <summary>A farm: a farmhouse beside four fenced crop fields, a road in from the west. No wall.</summary>
        public static SettlementPiece Farm { get; } = new SettlementPiece(
            "Farm", SettlementKind.Farm, 10, 6, WallStyle.None,
            new[]
            {
                "..........",
                "..........",
                "pppp......",
                "pppp......",
                "..........",
                "..........",
            },
            new[]
            {
                new BuildingSpot(MortalBuilding.Farmhouse, 1, 2),
                new BuildingSpot(MortalBuilding.CropField, 4, 0),
                new BuildingSpot(MortalBuilding.CropField, 7, 0),
                new BuildingSpot(MortalBuilding.CropField, 4, 3),
                new BuildingSpot(MortalBuilding.CropField, 7, 3),
            },
            new[]
            {
                new SpawnMarker(MarkerKind.Peasant, 3, 2),
                new SpawnMarker(MarkerKind.Peasant, 3, 4),
            },
            new[] { new RoadEnd(0, 3, -1, 0, new Int2(-1, 3)) });

        /// <summary>A camp: three tents round a campfire, a road in from the south. No wall.</summary>
        public static SettlementPiece Camp { get; } = new SettlementPiece(
            "Camp", SettlementKind.Camp, 7, 6, WallStyle.None,
            new[]
            {
                ".......",
                ".......",
                ".......",
                "....p..",
                "....p..",
                "....p..",
            },
            new[]
            {
                new BuildingSpot(MortalBuilding.Tent, 0, 3),
                new BuildingSpot(MortalBuilding.Tent, 5, 3),
                new BuildingSpot(MortalBuilding.Tent, 2, 0),
            },
            new[]
            {
                new SpawnMarker(MarkerKind.Guard, 3, 4),
                new SpawnMarker(MarkerKind.Guard, 4, 2),
                new SpawnMarker(MarkerKind.Guard, 1, 1),
            },
            new[] { new RoadEnd(4, 0, 0, -1, new Int2(4, -1)) });

        /// <summary>Every piece, for checking that the set is well-formed.</summary>
        public static IReadOnlyList<SettlementPiece> All { get; } = new[] { SeatFort, Village, Farm, Camp };

        /// <summary>The piece a settlement of <paramref name="kind"/> is made from.</summary>
        public static SettlementPiece For(SettlementKind kind) => kind switch
        {
            SettlementKind.Seat => SeatFort,
            SettlementKind.Village => Village,
            SettlementKind.Farm => Farm,
            _ => Camp,
        };
    }
}
