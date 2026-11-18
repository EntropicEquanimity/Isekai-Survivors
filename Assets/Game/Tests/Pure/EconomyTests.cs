using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Isekai.Tests
{
    public class StockpileTests
    {
        [Test]
        public void Starts_empty_and_adds_up()
        {
            var pile = new Stockpile();
            Assert.AreEqual(0, pile.Get(ResourceType.Stone));
            pile.Add(ResourceType.Stone, 3);
            pile.Add(ResourceType.Stone, 2);
            Assert.AreEqual(5, pile.Get(ResourceType.Stone));
            Assert.AreEqual(0, pile.Get(ResourceType.Wood), "types are separate");
        }

        [Test]
        public void TrySpend_takes_only_when_affordable()
        {
            var pile = new Stockpile();
            pile.Add(ResourceType.Wood, 4);
            Assert.IsFalse(pile.TrySpend(ResourceType.Wood, 5));
            Assert.AreEqual(4, pile.Get(ResourceType.Wood), "a failed spend takes nothing");
            Assert.IsTrue(pile.TrySpend(ResourceType.Wood, 4));
            Assert.AreEqual(0, pile.Get(ResourceType.Wood));
        }

        [Test]
        public void Changed_reports_the_new_amount_and_skips_no_ops()
        {
            var pile = new Stockpile();
            var seen = new List<(ResourceType, int)>();
            pile.Changed += (t, a) => seen.Add((t, a));
            pile.Add(ResourceType.Gold, 2);
            pile.Add(ResourceType.Gold, 0);
            pile.TrySpend(ResourceType.Gold, 5);
            pile.TrySpend(ResourceType.Gold, 1);
            CollectionAssert.AreEqual(new[] { (ResourceType.Gold, 2), (ResourceType.Gold, 1) }, seen);
        }

        [Test]
        public void Negative_amounts_are_refused()
        {
            var pile = new Stockpile();
            Assert.Throws<ArgumentOutOfRangeException>(() => pile.Add(ResourceType.Iron, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => pile.TrySpend(ResourceType.Iron, -1));
        }
    }

    public class MiningProgressTests
    {
        [Test]
        public void Fractions_carry_over_between_ticks()
        {
            var mining = new MiningProgress();
            Assert.AreEqual(0, mining.Tick(0.3f, 2f, 10), "0.6 of a unit");
            Assert.AreEqual(1, mining.Tick(0.3f, 2f, 10), "1.2 units in total");
            Assert.AreEqual(0.2f, mining.Partial, 1e-4f);
        }

        [Test]
        public void Never_pays_more_than_the_node_has()
        {
            var mining = new MiningProgress();
            Assert.AreEqual(3, mining.Tick(10f, 2f, 3));
            Assert.AreEqual(0f, mining.Partial);
            Assert.AreEqual(0, mining.Tick(1f, 2f, 0), "an empty node pays nothing");
        }

        [Test]
        public void Reset_drops_partial_progress()
        {
            var mining = new MiningProgress();
            mining.Tick(0.4f, 2f, 10);
            mining.Reset();
            Assert.AreEqual(0, mining.Tick(0.2f, 2f, 10), "0.4 again, not 1.2");
        }
    }
}
