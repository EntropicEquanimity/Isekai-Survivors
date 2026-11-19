using System;
using NUnit.Framework;

namespace Isekai.Tests
{
    public class SideTests
    {
        [Test]
        public void Yours_and_mortals_are_hostile()
        {
            Assert.IsTrue(SideRelations.AreHostile(Side.Yours, Side.Mortals));
            Assert.IsTrue(SideRelations.AreHostile(Side.Mortals, Side.Yours));
        }

        [Test]
        public void Rogue_is_hostile_to_every_other_side_and_friendly_to_its_own()
        {
            foreach (Side other in Enum.GetValues(typeof(Side)))
            {
                if (other == Side.Rogue) continue;
                Assert.IsTrue(SideRelations.AreHostile(Side.Rogue, other), $"rogue vs {other}");
                Assert.IsTrue(SideRelations.AreHostile(other, Side.Rogue), $"{other} vs rogue");
            }
            Assert.AreEqual(SideRelation.Allied, SideRelations.Between(Side.Rogue, Side.Rogue), "rogues don't fight each other");
        }

        [Test]
        public void Hostile_creatures_attack_everyone_neutral_creatures_included()
        {
            Assert.IsTrue(SideRelations.AreHostile(Side.Hostile, Side.Yours));
            Assert.IsTrue(SideRelations.AreHostile(Side.Hostile, Side.Mortals));
            Assert.IsTrue(SideRelations.AreHostile(Side.Hostile, Side.Neutral));
            Assert.IsTrue(SideRelations.AreHostile(Side.Neutral, Side.Hostile));
            Assert.AreEqual(SideRelation.Allied, SideRelations.Between(Side.Hostile, Side.Hostile));
        }

        [Test]
        public void Neutral_creatures_start_no_fights()
        {
            Assert.IsFalse(SideRelations.AreHostile(Side.Neutral, Side.Yours));
            Assert.IsFalse(SideRelations.AreHostile(Side.Neutral, Side.Mortals));
            Assert.AreEqual(SideRelation.Neutral, SideRelations.Between(Side.Neutral, Side.Yours));
        }

        [Test]
        public void The_table_is_symmetric_and_every_side_is_allied_with_itself()
        {
            foreach (Side a in Enum.GetValues(typeof(Side)))
            {
                Assert.AreEqual(SideRelation.Allied, SideRelations.Between(a, a), a.ToString());
                foreach (Side b in Enum.GetValues(typeof(Side)))
                    Assert.AreEqual(SideRelations.Between(a, b), SideRelations.Between(b, a), $"{a} / {b}");
            }
        }

        [Test]
        public void Sides_map_to_team_ids_and_back_and_the_old_names_stay()
        {
            Assert.AreEqual("Players", TeamIds.Of(Side.Yours));
            Assert.AreEqual("Enemies", TeamIds.Of(Side.Mortals));
            foreach (Side side in Enum.GetValues(typeof(Side)))
                Assert.AreEqual(side, TeamIds.SideOf(TeamIds.Of(side)), side.ToString());
            Assert.AreEqual(Side.Neutral, TeamIds.SideOf(null), "no team is neutral");
            Assert.AreEqual(Side.Neutral, TeamIds.SideOf("Something else"));
        }
    }

    public class CellLayerTests
    {
        [Test]
        public void Starts_at_zero_and_reads_and_writes_by_cell()
        {
            var layer = new CellLayer("vision", 4, 3);
            Assert.AreEqual(0, layer.Get(new Blondie.Core.Int2(2, 1)));
            layer.Set(new Blondie.Core.Int2(2, 1), 7);
            Assert.AreEqual(7, layer.Get(new Blondie.Core.Int2(2, 1)));
            Assert.AreEqual(0, layer.Get(new Blondie.Core.Int2(1, 1)));
        }

        [Test]
        public void Outside_the_map_reads_zero_and_ignores_writes()
        {
            var layer = new CellLayer("x", 2, 2);
            Assert.AreEqual(0, layer.Get(new Blondie.Core.Int2(5, 5)));
            Assert.IsFalse(layer.Set(new Blondie.Core.Int2(-1, 0), 1));
            Assert.IsFalse(layer.Contains(new Blondie.Core.Int2(2, 0)));
        }

        [Test]
        public void Tells_listeners_only_about_real_changes()
        {
            var layer = new CellLayer("x", 3, 3);
            int calls = 0;
            Blondie.Core.Int2 lastCell = default;
            byte before = 99, after = 99;
            layer.Changed += (cell, b, a) => { calls++; lastCell = cell; before = b; after = a; };
            Assert.IsFalse(layer.Set(new Blondie.Core.Int2(1, 2), 0), "same value");
            Assert.AreEqual(0, calls);
            Assert.IsTrue(layer.Set(new Blondie.Core.Int2(1, 2), 5));
            Assert.AreEqual(1, calls);
            Assert.AreEqual(new Blondie.Core.Int2(1, 2), lastCell);
            Assert.AreEqual(0, before);
            Assert.AreEqual(5, after);
            Assert.IsFalse(layer.Set(new Blondie.Core.Int2(1, 2), 5), "same value again");
            Assert.AreEqual(1, calls);
        }
    }

    public class RegionRecipeTests
    {
        [Test]
        public void Carries_the_seed_and_map_size()
        {
            var recipe = new RegionRecipe(42, 96, 64);
            Assert.AreEqual(42, recipe.Seed);
            Assert.AreEqual(96, recipe.Width);
            Assert.AreEqual(64, recipe.Height);
        }

        [Test]
        public void The_default_map_is_an_outer_ring_Regions_size()
        {
            var recipe = new RegionRecipe(1);
            Assert.AreEqual(RegionRecipe.DefaultWidth, recipe.Width);
            Assert.AreEqual(RegionRecipe.DefaultHeight, recipe.Height);
            Assert.AreEqual(160, RegionRecipe.DefaultWidth);
            Assert.AreEqual(110, RegionRecipe.DefaultHeight);
        }

        [Test]
        public void Refuses_an_empty_map()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new RegionRecipe(1, 0, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RegionRecipe(1, 10, -1));
        }

        [Test]
        public void Region_steps_have_named_orders_and_the_existing_ones_keep_theirs()
        {
            Assert.AreEqual(0, RegionOrder.Settings, "RegionRun's setup");
            Assert.AreEqual(0, RegionOrder.Wilds, "RegionWilds' populate");
            Assert.Less(RegionOrder.Terrain, RegionOrder.Settlements);
            Assert.Less(RegionOrder.Settlements, RegionOrder.Wilds, "wildlife and nodes go in last");
        }
    }
}
