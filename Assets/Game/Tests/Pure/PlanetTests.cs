using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Isekai.Tests
{
    public class PlanetTests
    {
        private static readonly int[] Rings = { 6, 4, 3 };
        private static readonly string[] Buffs = { "a", "b", "c" };

        private static Planet Make(int seed = 1) => Planet.Generate(seed, Rings, Buffs);

        [Test]
        public void Rings_surround_a_single_Capital()
        {
            Planet p = Make();
            Assert.AreEqual(6 + 4 + 3 + 1, p.Regions.Count);
            Assert.AreEqual(3, p.Capital.Depth);
            Assert.AreEqual(6, p.Edge.Count());
            Assert.IsTrue(p.Regions.Where(r => !r.IsCapital).All(r => !string.IsNullOrEmpty(r.BuffId)));
        }

        [Test]
        public void Links_go_both_ways_and_only_the_inner_ring_touches_the_Capital()
        {
            Planet p = Make();
            foreach (RegionInfo r in p.Regions)
                foreach (string n in r.Neighbours)
                    Assert.Contains(r.Id, p[n].Neighbours, $"{r} <-> {p[n]}");
            Assert.IsTrue(p.Capital.Neighbours.All(id => p[id].Depth == 2));
            Assert.AreEqual(3, p.Capital.Neighbours.Count);
        }

        [Test]
        public void Every_Region_can_reach_the_Capital_from_the_edge()
        {
            Planet p = Make(42);
            var seen = new HashSet<string>(p.Edge.Select(r => r.Id));
            var queue = new Queue<string>(seen);
            while (queue.Count > 0)
                foreach (string n in p[queue.Dequeue()].Neighbours)
                    if (seen.Add(n)) queue.Enqueue(n);
            Assert.AreEqual(p.Regions.Count, seen.Count);
        }

        [Test]
        public void The_same_seed_makes_the_same_Planet()
        {
            CollectionAssert.AreEqual(Make(7).Regions.Select(r => r.Name + r.BuffId), Make(7).Regions.Select(r => r.Name + r.BuffId));
        }
    }

    public class PlanetRunTests
    {
        [Test]
        public void A_Run_lands_on_the_edge_then_spreads_to_neighbours()
        {
            Planet planet = Planet.Generate(1, new[] { 6, 4, 3 }, new[] { "a" });
            var run = new PlanetRun(planet);
            CollectionAssert.AreEquivalent(planet.Edge, run.Available());
            Assert.IsFalse(run.Enter(planet.Capital), "can't land on the Capital");

            RegionInfo first = planet.Edge.First();
            Assert.IsTrue(run.Enter(first));
            Assert.AreEqual(PlanetRunState.InRegion, run.State);
            run.Tick(90f);
            run.CaptureCurrent();
            Assert.AreEqual(PlanetRunState.Choosing, run.State);
            Assert.AreEqual(90f, run.RunSeconds);
            Assert.IsTrue(run.IsCaptured(first));
            CollectionAssert.AreEquivalent(first.Neighbours, run.Available().Select(r => r.Id));
            Assert.AreEqual(first.BonusStone, run.BonusStone);
            CollectionAssert.AreEqual(new[] { "a" }, run.Buffs);
        }

        [Test]
        public void Capturing_the_Capital_wins_and_losing_ends_the_Run()
        {
            Planet planet = Planet.Generate(3, new[] { 1 }, null);
            var run = new PlanetRun(planet);
            run.Enter(planet.Edge.Single());
            run.CaptureCurrent();
            Assert.IsTrue(run.Enter(planet.Capital), "the edge Region touches the Capital");
            run.CaptureCurrent();
            Assert.AreEqual(PlanetRunState.Won, run.State);
            Assert.IsEmpty(run.Available());

            var lost = new PlanetRun(planet);
            lost.Enter(planet.Edge.Single());
            lost.Lose();
            Assert.IsTrue(lost.IsOver);
            Assert.IsFalse(lost.Enter(planet.Capital));
        }

        [Test]
        public void The_clock_only_runs_inside_a_Region()
        {
            Planet planet = Planet.Generate(3, new[] { 2 }, null);
            var run = new PlanetRun(planet);
            run.Tick(10f);
            Assert.AreEqual(0f, run.RunSeconds, "not on the map");
        }

        [Test]
        public void Escaped_rulers_retreat_one_ring_inward_or_the_Capital()
        {
            Planet planet = Planet.Generate(9, new[] { 3, 2 }, new[] { "a" });
            var run = new PlanetRun(planet);
            RegionInfo from = planet.Edge.First();
            RegionInfo to = run.PickRetreatTarget(from);
            Assert.AreEqual(from.Depth + 1, to.Depth, "one ring inward while one is uncaptured");

            Planet tiny = Planet.Generate(5, new[] { 1, 1 }, null);
            var tight = new PlanetRun(tiny);
            RegionInfo edge = tiny.Edge.Single();
            RegionInfo middle = tiny.Regions.First(r => r.Depth == 1 && !r.IsCapital);
            Assert.IsTrue(tight.Enter(edge));
            tight.CaptureCurrent();
            Assert.IsTrue(tight.Enter(middle));
            tight.CaptureCurrent();
            Assert.AreEqual(tiny.Capital, tight.PickRetreatTarget(edge), "when the inward Region is captured, retreat to the Capital");
        }

        [Test]
        public void A_Run_keeps_the_escaped_rulers_record()
        {
            Planet planet = Planet.Generate(2, new[] { 3, 2 }, null);
            var run = new PlanetRun(planet);
            RegionInfo from = planet.Edge.First();
            RegionInfo to = run.PickRetreatTarget(from);
            Assert.IsTrue(run.RecordEscapedRuler(from, to, "Aldric the Stern", new[] { TraitId.Brave, TraitId.Vigilant }));
            Assert.AreEqual(1, run.EscapedRulers.Count);
            EscapedRuler escaped = run.EscapedRulers.Single();
            Assert.AreEqual(from.Id, escaped.FromRegionId);
            Assert.AreEqual(to.Id, escaped.ToRegionId);
            CollectionAssert.AreEqual(new[] { TraitId.Brave, TraitId.Vigilant }, escaped.Traits);
            CollectionAssert.AreEqual(new[] { escaped }, run.EscapedRulersAt(to));
        }
    }
}
