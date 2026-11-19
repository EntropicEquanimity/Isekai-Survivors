using System;
using System.Collections.Generic;
using System.Linq;
using Blondie.Core;
using NUnit.Framework;

namespace Isekai.Tests
{
    public class SettlementTests
    {
        private static readonly int[] Seeds = { 1, 2, 3, 42, 777, 2026, 99999, -5, 20261119 };

        private static RegionLayout Full(int seed) => RegionLayout.Generate(new RegionRecipe(seed));

        private static RegionLayout Small(int seed) => RegionLayout.Generate(new RegionRecipe(seed, 64, 48));

        private static RegionLayout Tiny(int seed) => RegionLayout.Generate(new RegionRecipe(seed, 32, 32));

        private static IEnumerable<RegionLayout> Layouts() => Seeds.SelectMany(s => new[] { Full(s), Small(s), Tiny(s) });

        private static IEnumerable<Int2> Cells(PlacedBuilding b)
        {
            for (int x = 0; x < b.Size.x; x++)
            for (int y = 0; y < b.Size.y; y++)
                yield return new Int2(b.Origin.x + x, b.Origin.y + y);
        }

        private static string Signature(RegionLayout l) => string.Join(";", l.Settlements.Settlements.Select(s =>
            $"{s.Kind}@{s.Origin.x}:{s.Origin.y}/{s.FlipX}{s.FlipY}[{string.Join(",", s.Buildings.Select(b => $"{b.Kind}{b.Origin.x}:{b.Origin.y}"))}]"))
            + "|" + string.Join(";", l.Settlements.Roads.Select(r => $"{r.Start.x}:{r.Start.y}>{r.End.x}:{r.End.y}#{r.Cells.Count}"));

        // ---- the pieces ----

        [Test]
        public void Every_piece_is_well_formed()
        {
            foreach (SettlementPiece piece in SettlementPieces.All)
            {
                var taken = new HashSet<Int2>();
                foreach (BuildingSpot spot in piece.Spots)
                    for (int x = 0; x < spot.Size.x; x++)
                    for (int y = 0; y < spot.Size.y; y++)
                    {
                        var c = new Int2(spot.Offset.x + x, spot.Offset.y + y);
                        Assert.IsTrue(taken.Add(c), $"{piece.Name}: {spot.Kind} overlaps at {c.x},{c.y}");
                        bool inside = c.x >= 0 && c.y >= 0 && c.x < piece.Width && c.y < piece.Height;
                        bool inRing = c.x >= -1 && c.y >= -1 && c.x <= piece.Width && c.y <= piece.Height;
                        Assert.IsTrue(inside || (inRing && MortalBuildings.IsWall(spot.Kind)), $"{piece.Name}: {spot.Kind} at {c.x},{c.y} is outside the piece");
                    }
                foreach (SpawnMarker m in piece.Markers)
                {
                    Assert.IsTrue(m.Offset.x >= 0 && m.Offset.y >= 0 && m.Offset.x < piece.Width && m.Offset.y < piece.Height, $"{piece.Name}: marker outside");
                    Assert.IsFalse(taken.Contains(m.Offset), $"{piece.Name}: a {m.Kind} marker stands inside a Building");
                }
                Assert.IsNotEmpty(piece.RoadEnds, piece.Name);
                foreach (RoadEnd end in piece.RoadEnds)
                {
                    Assert.IsFalse(taken.Contains(end.Edge), $"{piece.Name}: a road end is inside a Building");
                    Assert.IsTrue(end.Edge.x == 0 || end.Edge.y == 0 || end.Edge.x == piece.Width - 1 || end.Edge.y == piece.Height - 1, $"{piece.Name}: a road end is not on the edge");
                    Assert.IsNotEmpty(end.Gap, piece.Name);
                }
            }
            foreach (BuildingSpot spot in SettlementPieces.SeatFort.Spots)
            {
                Int2 a = SettlementPieces.SeatAnchor;
                Assert.IsFalse(a.x >= spot.Offset.x && a.x < spot.Offset.x + spot.Size.x && a.y >= spot.Offset.y && a.y < spot.Offset.y + spot.Size.y, "the Seat's spot is open ground");
            }
        }

        [Test]
        public void The_pieces_hold_what_the_spec_says()
        {
            Assert.IsTrue(SettlementPieces.SeatFort.Spots.Any(s => s.Kind == MortalBuilding.Keep));
            Assert.IsTrue(SettlementPieces.SeatFort.Spots.Any(s => s.Kind == MortalBuilding.CastleGate));
            Assert.AreEqual(WallStyle.Castle, SettlementPieces.SeatFort.Wall);
            Assert.IsTrue(SettlementPieces.Village.Spots.Any(s => s.Kind == MortalBuilding.TownHall));
            Assert.IsTrue(SettlementPieces.Village.Spots.Any(s => s.Kind == MortalBuilding.PeasantHouse));
            Assert.AreEqual(WallStyle.Palisade, SettlementPieces.Village.Wall);
            Assert.IsTrue(SettlementPieces.Farm.Spots.Any(s => s.Kind == MortalBuilding.Farmhouse));
            Assert.IsTrue(SettlementPieces.Farm.Spots.Any(s => s.Kind == MortalBuilding.CropField));
            Assert.IsTrue(SettlementPieces.Camp.Spots.Any(s => s.Kind == MortalBuilding.Tent));
            foreach (SettlementPiece piece in SettlementPieces.All)
            {
                Assert.IsNotEmpty(piece.Markers, piece.Name);
                bool anyTile = false;
                for (int y = 0; y < piece.Height; y++)
                for (int x = 0; x < piece.Width; x++) anyTile |= piece.TileAt(x, y).HasValue;
                Assert.IsTrue(anyTile, $"{piece.Name} has a ground patch");
            }
        }

        // ---- the layout ----

        [Test]
        public void The_same_seed_gives_the_same_places_and_roads()
        {
            foreach (int seed in Seeds) Assert.AreEqual(Signature(Full(seed)), Signature(Full(seed)), $"seed {seed}");
        }

        [Test]
        public void Different_seeds_place_things_differently()
        {
            string[] signatures = Seeds.Select(s => Signature(Full(s))).ToArray();
            Assert.AreEqual(signatures.Length, signatures.Distinct().Count());
        }

        [Test]
        public void Everything_is_placed_in_the_fixed_order_Seat_villages_farms_camps_roads_walls_then_rocks_nodes_wildlife()
        {
            var expected = new[]
            {
                LayoutStep.Seat, LayoutStep.Villages, LayoutStep.Farms, LayoutStep.Camps, LayoutStep.Roads, LayoutStep.Walls,
                LayoutStep.Rocks, LayoutStep.Nodes, LayoutStep.Wildlife,
            };
            foreach (RegionLayout l in Layouts()) CollectionAssert.AreEqual(expected, l.PlacementOrder);
            foreach (RegionLayout l in Layouts())
            {
                // The Seat is the first settlement, then the villages, then the farms, then the camps.
                Assert.AreEqual(SettlementKind.Seat, l.Settlements.Settlements[0].Kind);
                var kinds = l.Settlements.Settlements.Skip(1).Select(s => (int)s.Kind).ToList();
                CollectionAssert.AreEqual(kinds.OrderBy(k => k).ToList(), kinds);
            }
        }

        [Test]
        public void A_full_size_map_has_the_Seat_and_three_to_ten_villages_farms_and_camps_with_all_pieces_placed()
        {
            foreach (int seed in Seeds.Concat(Enumerable.Range(200, 40)))
            {
                RegionLayout l = Full(seed);
                IReadOnlyList<PlacedSettlement> all = l.Settlements.Settlements;
                Assert.That(all.Count - 1, Is.InRange(SettlementPlan.MinCount, SettlementPlan.MaxCount), $"seed {seed}");
                Assert.IsEmpty(l.Settlements.Unplaced, $"seed {seed}: every piece found room");
                foreach (SettlementKind kind in new[] { SettlementKind.Village, SettlementKind.Camp })
                    Assert.IsTrue(all.Any(s => s.Kind == kind), $"seed {seed}: a {kind}");
                foreach (PlacedSettlement s in all)
                {
                    Assert.AreEqual(s.Piece.Spots.Count, s.Buildings.Count(b => !b.IsWall) + s.Piece.Spots.Count(sp => MortalBuildings.IsWall(sp.Kind)), $"seed {seed}: all of {s.Kind}'s Building spots are used");
                    Assert.AreEqual(s.Piece.Markers.Count, s.Markers.Count, $"seed {seed}: spawn markers");
                }
            }
        }

        [Test]
        public void The_Seat_stands_on_its_reserved_spot_opposite_the_start_with_its_gate_toward_the_middle()
        {
            foreach (RegionLayout l in Layouts())
            {
                PlacedSettlement seat = l.Settlements.Seat;
                Assert.IsTrue(seat.IsSeat);
                Assert.AreEqual(l.Seat, seat.MapCell(SettlementPieces.SeatAnchor), "the courtyard's centre is the reserved spot");
                Assert.IsNull(l.Settlements.BuildingAt(l.Seat), "the reserved spot is open ground");
                Assert.IsTrue(l.Contains(new Int2(seat.Reserved.MinX, seat.Reserved.MinY)) && l.Contains(new Int2(seat.Reserved.MaxX, seat.Reserved.MaxY)), "the fort is on the map");
                Assert.IsFalse(seat.Reserved.Overlaps(new CellRect(l.Anchor.x - 10, l.Anchor.y - 10, l.Anchor.x + 11, l.Anchor.y + 11)), "far from the start");
                PlacedRoadEnd gate = seat.RoadEnds.Single();
                Assert.AreEqual(Math.Sign(l.Height / 2 - l.Seat.y), gate.Facing.y, "the gate faces the map's middle");
            }
        }

        [Test]
        public void Nothing_overlaps_a_Building_a_wall_a_road_or_the_start()
        {
            foreach (RegionLayout l in Layouts())
            {
                var seen = new HashSet<Int2>();
                foreach (PlacedSettlement s in l.Settlements.Settlements)
                    foreach (PlacedBuilding b in s.Buildings)
                        foreach (Int2 c in Cells(b))
                        {
                            Assert.IsTrue(seen.Add(c), $"{l.Recipe}: {b.Kind} overlaps another Building at {c.x},{c.y}");
                            Assert.IsTrue(s.Reserved.Contains(c), $"{l.Recipe}: {b.Kind} leaves its settlement");
                            Assert.IsFalse(l.InStartArea(c), $"{l.Recipe}: {b.Kind} in the start area");
                            Assert.IsTrue(l.Contains(c), $"{l.Recipe}: {b.Kind} off the map");
                        }
                foreach (Int2 c in l.Settlements.RoadCells)
                {
                    Assert.IsFalse(seen.Contains(c), $"{l.Recipe}: a road runs through a Building at {c.x},{c.y}");
                    Assert.IsFalse(l.InStartArea(c), $"{l.Recipe}: a road in the start area");
                    Assert.IsFalse(l.Settlements.IsSettled(c), $"{l.Recipe}: a road inside a settlement's ring at {c.x},{c.y}");
                }
                IReadOnlyList<PlacedSettlement> all = l.Settlements.Settlements;
                for (int i = 0; i < all.Count; i++)
                    for (int j = i + 1; j < all.Count; j++)
                        Assert.IsFalse(all[i].Reserved.Inflate(SettlementPlan.Spacing - 1).Overlaps(all[j].Reserved), $"{l.Recipe}: settlements {i} and {j} crowd each other");
            }
        }

        [Test]
        public void On_a_seed_where_a_piece_does_not_fit_it_is_left_out_and_the_Seat_is_still_placed()
        {
            int leftOut = 0;
            foreach (int seed in Seeds.Concat(Enumerable.Range(300, 20)))
            {
                RegionLayout l = Tiny(seed);
                Assert.AreEqual(SettlementKind.Seat, l.Settlements.Seat.Kind, $"seed {seed}: the Seat");
                leftOut += l.Settlements.Unplaced.Count;
                Assert.IsTrue(l.CanWalk(l.Character, l.Seat), $"seed {seed}: Jack can still walk to the Seat");
            }
            Assert.Greater(leftOut, 0, "a map this small has no room for every piece");
        }

        [Test]
        public void Walls_wrap_what_is_placed_and_stay_open_where_a_road_leaves()
        {
            foreach (RegionLayout l in Layouts())
                foreach (PlacedSettlement s in l.Settlements.Settlements)
                {
                    var walls = new HashSet<Int2>(s.Buildings.Where(b => b.IsWall).SelectMany(Cells));
                    if (s.Piece.Wall == WallStyle.None)
                    {
                        Assert.IsEmpty(walls, $"{l.Recipe}: a {s.Kind} has no wall");
                        continue;
                    }
                    var gaps = new HashSet<Int2>(s.RoadEnds.Where(e => e.InUse || s.IsSeat).SelectMany(e => e.Gap));
                    if (gaps.Count == 0) gaps.UnionWith(s.RoadEnds[0].Gap);   // a village no road reached keeps one gap
                    CellRect ring = s.Reserved;
                    for (int x = ring.MinX; x <= ring.MaxX; x++)
                    for (int y = ring.MinY; y <= ring.MaxY; y++)
                    {
                        if (x != ring.MinX && x != ring.MaxX && y != ring.MinY && y != ring.MaxY) continue;
                        var c = new Int2(x, y);
                        bool gate = s.Buildings.Any(b => b.Kind == MortalBuilding.CastleGate && b.Covers(c));
                        if (gaps.Contains(c)) Assert.IsFalse(walls.Contains(c) && !gate, $"{l.Recipe}: a wall in a road's gap at {x},{y}");
                        else Assert.IsTrue(walls.Contains(c), $"{l.Recipe}: {s.Kind} {s.Index} has a hole in its wall at {x},{y}");
                        Assert.IsTrue(!gate || gaps.Contains(c), $"{l.Recipe}: the gate is in a gap");
                    }
                    foreach (Int2 w in walls)
                        Assert.IsTrue(w.x == ring.MinX || w.x == ring.MaxX || w.y == ring.MinY || w.y == ring.MaxY, $"{l.Recipe}: a wall inside the settlement at {w.x},{w.y}");
                    foreach (PlacedBuilding b in s.Buildings.Where(b => !b.IsWall))
                        foreach (Int2 c in Cells(b)) Assert.IsTrue(s.Interior.Contains(c), $"{l.Recipe}: {b.Kind} is outside the wall");
                }
        }

        [Test]
        public void Palisades_wrap_villages_and_stone_walls_with_a_gate_wrap_the_Seat()
        {
            RegionLayout l = Full(2026);
            PlacedSettlement village = l.Settlements.Settlements.First(s => s.Kind == SettlementKind.Village);
            Assert.IsTrue(village.Buildings.Any(b => b.IsWall));
            Assert.IsTrue(village.Buildings.Where(b => b.IsWall).All(b => b.Kind.ToString().StartsWith("Palisade")));
            Assert.IsTrue(l.Settlements.Seat.Buildings.Where(b => b.IsWall).All(b => b.Kind.ToString().StartsWith("Castle")));
            Assert.AreEqual(1, l.Settlements.Seat.Buildings.Count(b => b.Kind == MortalBuilding.CastleGate));
            Assert.AreEqual(4, l.Settlements.Seat.Buildings.Count(b => b.Kind == MortalBuilding.CastleTower), "a tower at each corner");
        }

        [Test]
        public void Roads_join_every_settlement_to_the_Seat_between_road_ends()
        {
            foreach (int seed in Seeds.Concat(Enumerable.Range(200, 20)))
            {
                SettlementPlan plan = Full(seed).Settlements;
                Assert.AreEqual(plan.Settlements.Count - 1, plan.Roads.Count, $"seed {seed}: one road for each settlement beyond the Seat");
                var joined = new HashSet<PlacedSettlement> { plan.Seat };
                foreach (Road road in plan.Roads)
                {
                    Assert.IsTrue(road.From.RoadEnds.Any(e => e.Outside.Equals(road.Start) && e.InUse), $"seed {seed}: starts at a road end of the joined settlement");
                    Assert.IsTrue(road.To.RoadEnds.Any(e => e.Outside.Equals(road.End) && e.InUse), $"seed {seed}: ends at a road end of the new one");
                    Assert.AreEqual(road.Start, road.Cells[0]);
                    Assert.AreEqual(road.End, road.Cells[road.Cells.Count - 1]);
                    for (int i = 1; i < road.Cells.Count; i++)
                        Assert.AreEqual(1, Math.Abs(road.Cells[i].x - road.Cells[i - 1].x) + Math.Abs(road.Cells[i].y - road.Cells[i - 1].y), $"seed {seed}: a road is unbroken");
                    Assert.IsTrue(joined.Contains(road.From), $"seed {seed}: roads grow out from the Seat");
                    joined.Add(road.To);
                }
                Assert.AreEqual(plan.Settlements.Count, joined.Count, $"seed {seed}: every settlement is joined");
            }
        }

        [Test]
        public void Rocks_nodes_and_wildlife_keep_clear_of_settlements_and_roads()
        {
            foreach (RegionLayout l in Layouts())
                foreach (Int2 c in l.Rocks.Concat(l.Cliffs).Concat(l.StoneNodes).Concat(l.WoodNodes).Concat(l.WildlifeSpots))
                    Assert.IsFalse(l.Settlements.IsKeepOut(c), $"{l.Recipe}: {c.x},{c.y} is on a settlement or road");
        }

        [Test]
        public void Jack_can_still_walk_to_the_Seat_and_every_node_with_places_on_the_map()
        {
            foreach (int seed in Seeds)
            {
                RegionLayout l = Full(seed);
                Assert.IsTrue(l.CanWalk(l.Character, l.Seat), $"seed {seed}: the Seat");
                foreach (Int2 c in l.StoneNodes.Concat(l.WoodNodes).Concat(l.WildlifeSpots))
                    Assert.IsTrue(l.CanWalk(l.Character, c), $"seed {seed}: {c.x},{c.y}");
            }
        }
    }

    public class SettlementLedgerTests
    {
        private static SettlementLedger Ledger(out SettlementPlan plan)
        {
            plan = RegionLayout.Generate(new RegionRecipe(2026)).Settlements;
            return new SettlementLedger(plan.Settlements);
        }

        private static int Find(PlacedSettlement s, Func<PlacedBuilding, bool> match) => s.Buildings.Select((b, i) => (b, i)).First(t => match(t.b)).i;

        [Test]
        public void Destroying_the_Seats_keep_captures_the_Region()
        {
            SettlementLedger ledger = Ledger(out SettlementPlan plan);
            Assert.IsFalse(ledger.SeatCaptured);
            SettlementEffect effect = ledger.Destroyed(0, Find(plan.Seat, b => b.Kind == MortalBuilding.Keep));
            Assert.AreEqual(SettlementEffect.SeatCaptured, effect);
            Assert.IsTrue(ledger.SeatCaptured);
        }

        [Test]
        public void Taking_out_the_rulers_is_enough_and_the_rest_of_the_fort_and_the_other_places_may_stand()
        {
            SettlementLedger ledger = Ledger(out SettlementPlan plan);
            Assert.Greater(plan.Seat.Buildings.Count, 10, "a fort with a wall round it");
            ledger.Destroyed(0, Find(plan.Seat, b => b.Kind == MortalBuilding.Keep));
            Assert.IsTrue(ledger.SeatCaptured);
            Assert.Greater(plan.Settlements.Count, 1);
            for (int i = 1; i < plan.Settlements.Count; i++) Assert.IsFalse(ledger.IsRazed(i), "nothing else needed to fall");
        }

        [Test]
        public void Destroying_other_Seat_buildings_does_not_capture_it()
        {
            SettlementLedger ledger = Ledger(out SettlementPlan plan);
            for (int i = 0; i < plan.Seat.Buildings.Count; i++)
            {
                if (plan.Seat.Buildings[i].Kind == MortalBuilding.Keep) continue;
                Assert.AreEqual(SettlementEffect.None, ledger.Destroyed(0, i));
            }
            Assert.IsFalse(ledger.SeatCaptured, "walls, gate, towers and posts falling leave the rulers alone");
        }

        [Test]
        public void The_Seat_is_captured_not_razed()
        {
            SettlementLedger ledger = Ledger(out SettlementPlan plan);
            SettlementEffect all = SettlementEffect.None;
            for (int i = 0; i < plan.Seat.Buildings.Count; i++) all |= ledger.Destroyed(0, i);
            Assert.AreEqual(SettlementEffect.SeatCaptured, all);
            Assert.IsFalse(ledger.IsRazed(0));
            Assert.AreEqual(0, ledger.RazedCount);
        }

        [Test]
        public void A_settlement_is_razed_when_its_last_building_that_is_not_fence_falls()
        {
            SettlementLedger ledger = Ledger(out SettlementPlan plan);
            PlacedSettlement village = plan.Settlements.First(s => s.Kind == SettlementKind.Village);
            var proper = Enumerable.Range(0, village.Buildings.Count).Where(i => !village.Buildings[i].IsWall).ToList();
            Assert.AreEqual(proper.Count, ledger.Standing(village.Index));
            for (int k = 0; k < proper.Count - 1; k++) Assert.AreEqual(SettlementEffect.None, ledger.Destroyed(village.Index, proper[k]));
            Assert.IsFalse(ledger.IsRazed(village.Index));
            Assert.AreEqual(SettlementEffect.Razed, ledger.Destroyed(village.Index, proper[proper.Count - 1]));
            Assert.IsTrue(ledger.IsRazed(village.Index));
            Assert.AreEqual(1, ledger.RazedCount);
            Assert.IsFalse(ledger.SeatCaptured, "razing a village captures nothing");
        }

        [Test]
        public void Walls_do_not_count_towards_razing_and_a_building_is_counted_once()
        {
            SettlementLedger ledger = Ledger(out SettlementPlan plan);
            PlacedSettlement village = plan.Settlements.First(s => s.Kind == SettlementKind.Village);
            int wall = Find(village, b => b.IsWall);
            int hall = Find(village, b => b.Kind == MortalBuilding.TownHall);
            int standing = ledger.Standing(village.Index);
            Assert.AreEqual(SettlementEffect.None, ledger.Destroyed(village.Index, wall));
            Assert.AreEqual(standing, ledger.Standing(village.Index));
            ledger.Destroyed(village.Index, hall);
            Assert.AreEqual(SettlementEffect.None, ledger.Destroyed(village.Index, hall), "a second report changes nothing");
            Assert.AreEqual(standing - 1, ledger.Standing(village.Index));
        }
    }
}
