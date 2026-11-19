using System;
using Blondie.Core;
using NUnit.Framework;

namespace Isekai.Tests
{
    public class SpawnPlacementTests
    {
        [Test]
        public void Picks_only_walkable_accepted_cells()
        {
            var random = new Random(1);
            bool found = SpawnPlacement.TryPick(random, SpawnPlacement.Anywhere(20, 20), c => c.x >= 10, c => c.y < 5, out Int2 cell, 200);
            Assert.IsTrue(found);
            Assert.GreaterOrEqual(cell.x, 10);
            Assert.Less(cell.y, 5);
        }

        [Test]
        public void Gives_up_when_nothing_fits()
        {
            Assert.IsFalse(SpawnPlacement.TryPick(new Random(1), SpawnPlacement.Anywhere(5, 5), _ => false, null, out _, 10));
        }

        [Test]
        public void Shapes_stay_where_they_say()
        {
            var random = new Random(3);
            for (int i = 0; i < 200; i++)
            {
                Int2 ring = SpawnPlacement.Ring(new Int2(50, 50), 10f, 20f)(random);
                double d = Math.Sqrt((ring.x - 50) * (ring.x - 50) + (ring.y - 50) * (ring.y - 50));
                Assert.That(d, Is.InRange(9.0, 21.0), "ring");

                Int2 edge = SpawnPlacement.EdgeBand(40, 30, 3)(random);
                bool nearEdge = edge.x < 3 || edge.y < 3 || edge.x >= 37 || edge.y >= 27;
                Assert.IsTrue(nearEdge, $"edge band {edge}");

                Int2 inside = SpawnPlacement.Anywhere(40, 30, margin: 5)(random);
                Assert.That(inside.x, Is.InRange(5, 34));
                Assert.That(inside.y, Is.InRange(5, 24));
            }
        }
    }

    public class StableHashTests
    {
        [Test]
        public void Same_text_same_hash_and_streams_differ()
        {
            Assert.AreEqual(StableHash.Of("thornvale"), StableHash.Of("thornvale"));
            Assert.AreNotEqual(StableHash.Of("thornvale"), StableHash.Of("duskmoor"));
            Assert.AreNotEqual(StableHash.Seed(7, "region/a/wilds"), StableHash.Seed(7, "region/a/waves"));
            Assert.AreEqual(-2128831035, StableHash.Of(""), "FNV-1a offset basis: the value never changes between runs");
        }
    }
}
