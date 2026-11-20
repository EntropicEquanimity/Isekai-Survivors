using System;
using System.Linq;
using Blondie.Core;
using NUnit.Framework;

namespace Isekai.Tests
{
    /// <summary>The ring recipe: outer, middle, inner and the Capital reuse one generator with bigger numbers.</summary>
    public class RingRecipeTests
    {
        private static readonly RegionRing[] Rings = { RegionRing.Outer, RegionRing.Middle, RegionRing.Inner, RegionRing.Capital };

        private static void Rises(string what, Func<RingRecipe, float> read)
        {
            float last = float.MinValue;
            foreach (RegionRing ring in Rings)
            {
                float now = read(RingRecipe.Of(ring));
                Assert.Greater(now, last, $"{what} should rise toward the Capital ({ring})");
                last = now;
            }
        }

        [Test]
        public void The_outer_ring_keeps_the_numbers_every_Region_had_before()
        {
            RingRecipe outer = RingRecipe.Of(RegionRing.Outer);
            Assert.AreEqual(160, outer.Width);
            Assert.AreEqual(110, outer.Height);
            Assert.AreEqual(VigilanceTuning.OuterRingWaveCap, outer.WaveCap);
            Assert.AreEqual(1f, outer.TroopStrength);
            Assert.AreEqual(1f, outer.TroopSpeed);
            Assert.AreEqual(SupplyTuning.StartingSupply, outer.StartingSupply);
            Assert.AreEqual(VigilanceStage.Unaware, outer.StartStage);
            Assert.AreEqual(SettlementPieces.SeatFort.Width, outer.SeatSize);
        }

        [Test]
        public void Every_number_rises_toward_the_Capital()
        {
            Rises("map width", r => r.Width);
            Rises("map height", r => r.Height);
            Rises("Seat size", r => r.SeatSize);
            Rises("Seat guards", r => r.SeatGuards);
            Rises("Seat towers", r => r.SeatTowers);
            Rises("keep health", r => r.KeepHealth);
            Rises("elite guard strength", r => r.EliteGuardStrength);
            Rises("most settlements", r => r.MaxSettlements);
            Rises("fewest settlements", r => r.MinSettlements);
            Rises("troop strength", r => r.TroopStrength);
            Rises("troop limit", r => r.TroopLimitScale);
            Rises("troop raising speed", r => r.TroopSpeed);
            Rises("starting Supply", r => r.StartingSupply);
            Rises("Supply rate", r => r.SupplyRate);
            Rises("ruler health", r => r.RulerHealth);
            Rises("ruler damage", r => r.RulerDamage);
        }

        [Test]
        public void The_Wave_cap_rises_ring_by_ring_to_a_full_Planets_Capital()
        {
            Assert.Less(RingRecipe.Of(RegionRing.Outer).WaveCap, RingRecipe.Of(RegionRing.Middle).WaveCap);
            Assert.Less(RingRecipe.Of(RegionRing.Middle).WaveCap, RingRecipe.Of(RegionRing.Inner).WaveCap);
            Planet planet = Planet.Generate(4, new[] { 6, 4, 3 }, new[] { "a" });
            Assert.Greater(RingRecipe.For(planet.Capital, planet, 0).WaveCap, RingRecipe.Of(RegionRing.Inner).WaveCap);
        }

        [Test]
        public void The_Wave_cap_follows_the_glossary()
        {
            Assert.AreEqual(2, RingRecipe.Of(RegionRing.Outer).WaveCap);
            Assert.That(RingRecipe.Of(RegionRing.Middle).WaveCap, Is.InRange(3, 4));
            Assert.That(RingRecipe.Of(RegionRing.Inner).WaveCap, Is.InRange(5, 7));
        }

        [Test]
        public void The_Capital_starts_Alarmed_with_a_Wave_per_Region_not_yet_captured_and_at_least_three()
        {
            Assert.AreEqual(VigilanceStage.Alarmed, RingRecipe.Of(RegionRing.Capital).StartStage);
            Planet planet = Planet.Generate(4, new[] { 6, 4, 3 }, new[] { "a" });
            Assert.AreEqual(13, RingRecipe.For(planet.Capital, planet, 0).WaveCap);
            Assert.AreEqual(8, RingRecipe.For(planet.Capital, planet, 5).WaveCap);
            Assert.AreEqual(3, RingRecipe.For(planet.Capital, planet, 13).WaveCap, "never fewer than three");
            Planet small = Planet.Generate(4, new[] { 1 }, new[] { "a" });
            Assert.AreEqual(3, RingRecipe.For(small.Capital, small, 1).WaveCap);
        }

        [Test]
        public void Regions_get_their_rings_recipe_by_depth()
        {
            Planet planet = Planet.Generate(4, new[] { 6, 4, 3 }, new[] { "a" });
            foreach (RegionInfo r in planet.Regions)
            {
                RegionRing expected = r.IsCapital ? RegionRing.Capital : (RegionRing)r.Depth;
                Assert.AreEqual(expected, RingRecipe.For(r, planet, 0).Ring, r.ToString());
            }
        }

        [Test]
        public void A_small_Planet_still_has_an_outer_and_an_inner_ring()
        {
            Assert.AreEqual(RegionRing.Outer, RingRecipe.RingOf(0, 1));
            Assert.AreEqual(RegionRing.Outer, RingRecipe.RingOf(0, 2));
            Assert.AreEqual(RegionRing.Inner, RingRecipe.RingOf(1, 2));
            Assert.AreEqual(RegionRing.Middle, RingRecipe.RingOf(1, 3));
            Assert.AreEqual(RegionRing.Inner, RingRecipe.RingOf(2, 3));
        }

        [Test]
        public void A_recipe_carries_its_ring_and_defaults_to_the_outer_one()
        {
            Assert.AreEqual(RegionRing.Outer, new RegionRecipe(1).Ring.Ring);
            RegionRecipe capital = RegionRecipe.For(1, RingRecipe.Of(RegionRing.Capital));
            Assert.AreEqual(RingRecipe.Of(RegionRing.Capital).Width, capital.Width);
            Assert.AreEqual(RingRecipe.Of(RegionRing.Capital).Height, capital.Height);
            Assert.AreEqual(RegionRing.Capital, capital.Ring.Ring);
        }

        [Test]
        public void Each_ring_has_a_Seat_piece_of_its_size_that_grows()
        {
            int lastWalls = 0, lastTowers = 0, lastGuards = 0;
            foreach (RegionRing ring in Rings)
            {
                RingRecipe recipe = RingRecipe.Of(ring);
                SettlementPiece seat = SettlementPieces.SeatFor(recipe);
                Assert.AreEqual(recipe.SeatSize, seat.Width, ring.ToString());
                Assert.AreEqual(recipe.SeatSize, seat.Height, ring.ToString());
                Assert.AreEqual(recipe.SeatTowers, seat.Spots.Count(s => s.Kind == MortalBuilding.Watchtower), ring + " towers");
                Assert.AreEqual(recipe.SeatGuards, seat.Markers.Count(m => m.Kind == MarkerKind.Guard), ring + " guards");
                Assert.AreEqual(1, seat.Spots.Count(s => s.Kind == MortalBuilding.Keep), ring + " keep");
                Assert.AreEqual(WallStyle.Castle, seat.Wall);
                int walls = 4 * (seat.Width + 1);
                Assert.Greater(walls, lastWalls);
                Assert.Greater(recipe.SeatTowers, lastTowers);
                Assert.Greater(recipe.SeatGuards, lastGuards);
                lastWalls = walls;
                lastTowers = recipe.SeatTowers;
                lastGuards = recipe.SeatGuards;
            }
        }

        [Test]
        public void Every_ring_lays_out_a_walkable_Region_with_its_own_Seat()
        {
            foreach (RegionRing ring in Rings)
            foreach (int seed in new[] { 1, 7, 99 })
            {
                RingRecipe recipe = RingRecipe.Of(ring);
                RegionLayout layout = RegionLayout.Generate(RegionRecipe.For(seed, recipe));
                Assert.AreEqual(recipe.Width, layout.Width);
                PlacedSettlement seat = layout.Settlements.Seat;
                Assert.AreEqual(recipe.SeatSize, seat.Piece.Width, $"{ring} {seed}");
                Assert.IsTrue(layout.Contains(new Int2(seat.Reserved.MinX, seat.Reserved.MinY)) && layout.Contains(new Int2(seat.Reserved.MaxX, seat.Reserved.MaxY)),
                    $"{ring} {seed}: the Seat's walls fit on the map");
                Assert.AreEqual(layout.Seat, seat.MapCell(seat.Piece.Anchor));
                int others = layout.Settlements.Settlements.Count - 1 + layout.Settlements.Unplaced.Count;
                Assert.That(others, Is.InRange(recipe.MinSettlements, recipe.MaxSettlements), $"{ring} {seed}: settlements");
                Assert.IsTrue(layout.CanWalk(layout.Character, layout.Seat), $"{ring} {seed}: the Seat can be reached");
            }
        }

        [Test]
        public void Bigger_rings_hold_more_places_and_more_Seat_walls()
        {
            int Walls(RegionRing ring) => RegionLayout.Generate(RegionRecipe.For(5, RingRecipe.Of(ring))).Settlements.Seat.Buildings.Count(b => b.IsWall);
            int Places(RegionRing ring) => RegionLayout.Generate(RegionRecipe.For(5, RingRecipe.Of(ring))).Settlements.Settlements.Count;
            Assert.Greater(Walls(RegionRing.Middle), Walls(RegionRing.Outer));
            Assert.Greater(Walls(RegionRing.Capital), Walls(RegionRing.Inner));
            Assert.Greater(Places(RegionRing.Capital), Places(RegionRing.Outer));
        }

        [Test]
        public void Bigger_rings_raise_more_troops_and_a_Seat_with_a_wider_limit()
        {
            RingRecipe outer = RingRecipe.Of(RegionRing.Outer), capital = RingRecipe.Of(RegionRing.Capital);
            Assert.AreEqual(SupplyRules.TroopLimit(SettlementKind.Seat), SupplyRules.TroopLimit(SettlementKind.Seat, outer.TroopLimitScale));
            Assert.Greater(SupplyRules.TroopLimit(SettlementKind.Seat, capital.TroopLimitScale), SupplyRules.TroopLimit(SettlementKind.Seat));
            Assert.AreEqual(0, SupplyRules.TroopLimit(SettlementKind.Farm, capital.TroopLimitScale), "farms raise none");

            var raiser = new TroopRaiser(SettlementKind.Seat, capital.TroopLimitScale);
            var pool = new SupplyPool(1000f);
            int raised = 0;
            for (int i = 0; i < 400; i++)
                if (raiser.Tick(60f, VigilanceStage.Crusade, true, pool)) raised++;
            Assert.AreEqual(SupplyRules.TroopLimit(SettlementKind.Seat, capital.TroopLimitScale), raised);
        }
    }
}
