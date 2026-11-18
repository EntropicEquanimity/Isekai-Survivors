using System.Collections.Generic;
using Blondie.Core;
using NUnit.Framework;

namespace Isekai.Tests
{
    public class StockpileCostTests
    {
        [Test]
        public void Multi_line_costs_are_all_or_nothing()
        {
            var pile = new Stockpile();
            pile.Add(ResourceType.Stone, 10);
            pile.Add(ResourceType.Wood, 2);
            var price = new[] { new ResourceCost(ResourceType.Stone, 5), new ResourceCost(ResourceType.Wood, 3) };
            Assert.IsFalse(pile.TrySpend(price));
            Assert.AreEqual(10, pile.Get(ResourceType.Stone), "nothing taken when one line is short");
            pile.Add(ResourceType.Wood, 1);
            Assert.IsTrue(pile.TrySpend(price));
            Assert.AreEqual(5, pile.Get(ResourceType.Stone));
            Assert.AreEqual(0, pile.Get(ResourceType.Wood));
        }

        [Test]
        public void Repeated_types_add_up()
        {
            var pile = new Stockpile();
            pile.Add(ResourceType.Stone, 5);
            Assert.IsFalse(pile.CanAfford(new[] { new ResourceCost(ResourceType.Stone, 3), new ResourceCost(ResourceType.Stone, 3) }));
        }
    }

    public class BuildingGridTests
    {
        private static BuildingGrid<string> Grid(params Int2[] walls)
        {
            var blocked = new HashSet<Int2>(walls);
            return new BuildingGrid<string>(c => c.x >= 0 && c.y >= 0 && c.x < 10 && c.y < 10 && !blocked.Contains(c));
        }

        [Test]
        public void Footprints_need_ground_and_free_cells()
        {
            BuildingGrid<string> grid = Grid(new Int2(5, 5));
            Assert.IsTrue(grid.Place("barracks", new Int2(0, 0), new Int2(2, 2)));
            Assert.IsFalse(grid.CanPlace(new Int2(1, 1), new Int2(2, 2)), "overlaps the barracks");
            Assert.IsFalse(grid.CanPlace(new Int2(4, 4), new Int2(2, 2)), "covers a wall");
            Assert.IsFalse(grid.CanPlace(new Int2(9, 9), new Int2(2, 2)), "runs off the map");
            Assert.AreEqual("barracks", grid.At(new Int2(1, 1)));
        }

        [Test]
        public void Remove_frees_the_footprint()
        {
            BuildingGrid<string> grid = Grid();
            grid.Place("tower", new Int2(3, 3), new Int2(1, 1));
            grid.Remove("tower");
            Assert.AreEqual(0, grid.OccupiedCount);
            Assert.IsTrue(grid.CanPlace(new Int2(3, 3), new Int2(1, 1)));
        }

        [Test]
        public void Origin_centres_odd_and_even_footprints()
        {
            Assert.AreEqual(new Int2(4, 4), BuildingGrid<string>.OriginCentredOn(new Int2(5, 5), new Int2(3, 3)));
            Assert.AreEqual(new Int2(5, 5), BuildingGrid<string>.OriginCentredOn(new Int2(5, 5), new Int2(2, 2)));
        }
    }

    public class ConstructionAndProductionTests
    {
        [Test]
        public void Construction_completes_once_and_scales_with_speed()
        {
            var build = new Construction(10f);
            Assert.IsFalse(build.Tick(4f, 2f));
            Assert.AreEqual(0.8f, build.Progress, 1e-4f);
            Assert.IsTrue(build.Tick(1f, 2f), "completes on this tick");
            Assert.IsFalse(build.Tick(1f), "never completes twice");
        }

        [Test]
        public void Production_spawns_each_interval_up_to_the_cap()
        {
            var production = new Production(2f, 2);
            Assert.IsFalse(production.Tick(1.5f, 0));
            Assert.IsTrue(production.Tick(0.5f, 0));
            Assert.IsFalse(production.Tick(5f, 2), "at the cap");
            Assert.IsTrue(production.Tick(0f, 1), "the timer waited full, so the replacement comes at once");
        }
    }

    public class StanceAndEngagementTests
    {
        [Test]
        public void Hold_never_chases_but_still_attacks_in_range()
        {
            float leash = Stance.Hold.Leash();
            Assert.AreEqual(AutoFightOrder.Hold, Engagement.Decide(2f, 2f, 3f, leash, 0f));
            Assert.AreEqual(AutoFightOrder.Hold, Engagement.Decide(5f, 5f, 3f, leash, 0f), "out of range: stays put");
        }

        [Test]
        public void Guard_and_Hunt_chase_within_their_leash()
        {
            Assert.AreEqual(AutoFightOrder.Approach, Engagement.Decide(5f, 5f, 3f, Stance.Guard.Leash(), 0f));
            Assert.AreEqual(AutoFightOrder.Return, Engagement.Decide(10f, 10f, 3f, Stance.Guard.Leash(), 2f), "too far for Guard");
            Assert.AreEqual(AutoFightOrder.Approach, Engagement.Decide(10f, 10f, 3f, Stance.Hunt.Leash(), 2f), "close enough for Hunt");
        }
    }

    public class CloningBayTests
    {
        [Test]
        public void Each_death_uses_a_pod_and_clones_after_the_delay()
        {
            var bay = new CloningBay(pods: 1, cloneSeconds: 5f);
            Assert.IsTrue(bay.CharacterDied());
            Assert.AreEqual(0, bay.Pods);
            Assert.IsFalse(bay.Tick(4f));
            Assert.IsTrue(bay.Tick(1f));
            Assert.IsFalse(bay.CharacterDied(), "no pods left: stays dead");
        }

        [Test]
        public void Added_pods_allow_more_clones()
        {
            var bay = new CloningBay(0, 1f);
            Assert.IsFalse(bay.CharacterDied());
            bay.AddPods(2);
            Assert.IsTrue(bay.CharacterDied());
            Assert.AreEqual(1, bay.Pods);
        }
    }
}
