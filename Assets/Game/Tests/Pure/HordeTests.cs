using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Isekai.Tests
{
    public class SpatialHashTests
    {
        [Test]
        public void Query_returns_exactly_the_points_in_the_circle_across_cells()
        {
            var hash = new SpatialHash<int>(1f);
            var random = new Random(7);
            var points = new List<(int id, float x, float y)>();
            for (int i = 0; i < 500; i++)
            {
                float x = (float)(random.NextDouble() * 40 - 20), y = (float)(random.NextDouble() * 40 - 20);
                points.Add((i, x, y));
                hash.Insert(i, x, y);
            }
            var found = new List<int>();
            hash.Query(1.3f, -2.7f, 4.5f, found);

            int[] expected = points.Where(p => (p.x - 1.3f) * (p.x - 1.3f) + (p.y + 2.7f) * (p.y + 2.7f) <= 4.5f * 4.5f).Select(p => p.id).ToArray();
            CollectionAssert.AreEquivalent(expected, found);
        }

        [Test]
        public void Negative_coordinates_land_in_their_own_cells()
        {
            var hash = new SpatialHash<string>(2f);
            hash.Insert("a", -0.5f, -0.5f);
            hash.Insert("b", 0.5f, 0.5f);
            var found = new List<string>();
            hash.Query(-0.5f, -0.5f, 0.1f, found);
            CollectionAssert.AreEqual(new[] { "a" }, found);
        }

        [Test]
        public void Clear_empties_it_and_it_grows_past_its_start_size()
        {
            var hash = new SpatialHash<int>(1f);
            for (int i = 0; i < 1000; i++) hash.Insert(i, 0f, 0f);
            Assert.AreEqual(1000, hash.Count);
            hash.Clear();
            var found = new List<int>();
            hash.Query(0f, 0f, 5f, found);
            Assert.IsEmpty(found);
        }

        [Test]
        public void TryNearest_skips_rejected_items_and_respects_the_radius()
        {
            var hash = new SpatialHash<int>(1f);
            hash.Insert(1, 1f, 0f);
            hash.Insert(2, 2f, 0f);
            hash.Insert(3, 9f, 0f);
            Assert.IsTrue(hash.TryNearest(0f, 0f, 5f, id => id != 1, out int nearest, out float distance));
            Assert.AreEqual(2, nearest);
            Assert.AreEqual(2f, distance, 1e-4f);
            Assert.IsFalse(hash.TryNearest(0f, 0f, 5f, id => id == 3, out _, out _), "3 is out of range");
        }
    }

    public class ContactAttackTests
    {
        [Test]
        public void Hits_once_per_interval()
        {
            var attack = new ContactAttack(5f, 1f, 0.5f);
            Assert.IsTrue(attack.TryHit(0f));
            Assert.IsFalse(attack.TryHit(0.5f));
            Assert.IsTrue(attack.TryHit(1f));
            Assert.AreEqual(2f, attack.ReadyAt);
        }
    }
}
