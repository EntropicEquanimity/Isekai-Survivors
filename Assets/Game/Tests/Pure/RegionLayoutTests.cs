using System;
using System.Linq;
using Blondie.Core;
using NUnit.Framework;

namespace Isekai.Tests
{
    public class RegionLayoutTests
    {
        private static readonly int[] Seeds = { 1, 2, 3, 42, 777, 2026, 99999, -5 };

        private static RegionLayout Small(int seed) => RegionLayout.Generate(new RegionRecipe(seed, 64, 48));

        private static string Signature(RegionLayout l) =>
            string.Join(";", new[] { l.Rocks, l.Cliffs, l.StoneNodes, l.WoodNodes, l.WildlifeSpots }
                .Select(list => string.Join(",", list.Select(c => c.x + ":" + c.y)))) + $"|{l.Corner}|{l.Seat.x}:{l.Seat.y}";

        private static int Reach(RegionLayout l, Int2 c) => Math.Max(Math.Abs(c.x - l.Anchor.x), Math.Abs(c.y - l.Anchor.y));

        [Test]
        public void The_same_seed_gives_the_same_layout()
        {
            foreach (int seed in Seeds) Assert.AreEqual(Signature(Small(seed)), Signature(Small(seed)), $"seed {seed}");
        }

        [Test]
        public void Different_seeds_give_different_layouts()
        {
            string[] signatures = Seeds.Select(s => Signature(Small(s))).ToArray();
            Assert.AreEqual(signatures.Length, signatures.Distinct().Count());
        }

        [Test]
        public void The_default_map_is_about_160_by_110_cells()
        {
            var recipe = new RegionRecipe(5);
            Assert.AreEqual(160, recipe.Width);
            Assert.AreEqual(110, recipe.Height);
            RegionLayout layout = RegionLayout.Generate(recipe);
            Assert.AreEqual(160, layout.Width);
            Assert.Greater(layout.Rocks.Count, 100);
            Assert.Greater(layout.Cliffs.Count, 50);
            Assert.GreaterOrEqual(layout.StoneNodes.Count, 10);
            Assert.GreaterOrEqual(layout.WoodNodes.Count, 10);
            Assert.GreaterOrEqual(layout.WildlifeSpots.Count, 5);
        }

        [Test]
        public void A_test_recipe_gives_a_small_map()
        {
            RegionLayout layout = Small(1);
            Assert.AreEqual(64, layout.Width);
            Assert.AreEqual(48, layout.Height);
            Assert.GreaterOrEqual(layout.StoneNodes.Count, 3);
            Assert.GreaterOrEqual(layout.WoodNodes.Count, 3);
        }

        [Test]
        public void Too_small_a_map_is_refused()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => RegionLayout.Generate(new RegionRecipe(1, 16, 16)));
        }

        [Test]
        public void The_Seat_spot_is_on_the_opposite_side_from_the_start()
        {
            foreach (int seed in Seeds)
            {
                RegionLayout l = Small(seed);
                // Mirrored through the map's centre: the start corner and the Seat's corner are diagonal opposites.
                Assert.AreEqual(l.Width - 1 - l.Anchor.x, l.Seat.x, $"seed {seed}");
                Assert.AreEqual(l.Height - 1 - l.Anchor.y, l.Seat.y, $"seed {seed}");
                Assert.AreNotEqual(Math.Sign(l.Anchor.x - l.Width / 2), Math.Sign(l.Seat.x - l.Width / 2));
                Assert.AreNotEqual(Math.Sign(l.Anchor.y - l.Height / 2), Math.Sign(l.Seat.y - l.Height / 2));
                Assert.Greater(Math.Abs(l.Seat.x - l.Anchor.x), l.Width / 2, "far apart");
            }
        }

        [Test]
        public void Every_corner_gets_used_by_some_seed()
        {
            MapCorner[] corners = Enumerable.Range(0, 60).Select(s => Small(s).Corner).Distinct().ToArray();
            Assert.AreEqual(4, corners.Length);
        }

        [Test]
        public void Everything_is_inside_the_map()
        {
            foreach (int seed in Seeds)
            {
                RegionLayout l = RegionLayout.Generate(new RegionRecipe(seed, 160, 110));
                Assert.IsTrue(l.Contains(l.Anchor) && l.Contains(new Int2(l.Anchor.x + 1, l.Anchor.y + 1)), "the Anchor's footprint");
                Assert.IsTrue(l.Contains(l.Character));
                Assert.IsTrue(l.Contains(l.Seat));
                foreach (Int2 c in l.Rocks.Concat(l.Cliffs).Concat(l.StoneNodes).Concat(l.WoodNodes).Concat(l.WildlifeSpots))
                    Assert.IsTrue(l.Contains(c), $"seed {seed}: {c.x},{c.y}");
            }
        }

        [Test]
        public void Nothing_lands_on_the_start_area_or_the_Seats_area()
        {
            foreach (int seed in Seeds)
            {
                RegionLayout l = Small(seed);
                Assert.IsTrue(l.InStartArea(l.Character) && l.InStartArea(l.Anchor));
                foreach (Int2 c in l.Rocks.Concat(l.Cliffs).Concat(l.StoneNodes).Concat(l.WoodNodes).Concat(l.WildlifeSpots))
                {
                    Assert.IsFalse(l.InStartArea(c), $"seed {seed}: {c.x},{c.y} in the start area");
                    Assert.IsFalse(l.InSeatArea(c), $"seed {seed}: {c.x},{c.y} in the Seat's area");
                }
            }
        }

        [Test]
        public void Nothing_shares_a_cell()
        {
            foreach (int seed in Seeds)
            {
                RegionLayout l = Small(seed);
                var all = l.Rocks.Concat(l.Cliffs).Concat(l.StoneNodes).Concat(l.WoodNodes).ToList();
                Assert.AreEqual(all.Count, all.Distinct().Count(), $"seed {seed}");
                foreach (Int2 w in l.WildlifeSpots) Assert.IsFalse(all.Contains(w));
            }
        }

        [Test]
        public void Rocks_and_cliffs_block_and_nodes_do_not_count_as_blockers()
        {
            RegionLayout l = Small(7);
            foreach (Int2 c in l.Rocks.Concat(l.Cliffs)) Assert.IsTrue(l.IsBlocked(c));
            foreach (Int2 c in l.StoneNodes) Assert.IsFalse(l.IsBlocked(c));
            Assert.IsFalse(l.IsBlocked(l.Anchor));
        }

        [Test]
        public void Jack_can_walk_to_the_Seat_and_to_every_node_and_wildlife_spot()
        {
            foreach (int seed in Seeds.Concat(Enumerable.Range(100, 30)))
            {
                RegionLayout l = RegionLayout.Generate(new RegionRecipe(seed, 64, 48));
                Assert.IsTrue(l.CanWalk(l.Character, l.Seat), $"seed {seed}: the Seat");
                foreach (Int2 c in l.StoneNodes.Concat(l.WoodNodes).Concat(l.WildlifeSpots))
                    Assert.IsTrue(l.CanWalk(l.Character, c), $"seed {seed}: {c.x},{c.y}");
            }
        }

        [Test]
        public void A_few_nodes_of_each_kind_lie_near_the_start()
        {
            foreach (int seed in Seeds)
            {
                RegionLayout l = Small(seed);
                int near = RegionLayout.StartClearRadius + 14;
                Assert.GreaterOrEqual(l.StoneNodes.Count(c => Reach(l, c) <= near), 3, $"seed {seed}: Stone");
                Assert.GreaterOrEqual(l.WoodNodes.Count(c => Reach(l, c) <= near), 3, $"seed {seed}: Wood");
            }
        }

        [Test]
        public void Wildlife_keeps_away_from_the_start()
        {
            foreach (int seed in Seeds)
            {
                RegionLayout l = Small(seed);
                foreach (Int2 w in l.WildlifeSpots) Assert.GreaterOrEqual(Reach(l, w), 20, $"seed {seed}");
            }
        }
    }
}
