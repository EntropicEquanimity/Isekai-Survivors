using System;
using Blondie.Core;
using NUnit.Framework;

namespace Isekai.Tests
{
    public class CorruptionTests
    {
        private CorruptionField _field;

        [SetUp]
        public void Make() => _field = new CorruptionField(new CellLayer("Corruption", 40, 40));

        [Test]
        public void Levels_stay_between_0_and_4()
        {
            var cell = new Int2(5, 5);
            Assert.IsFalse(_field.Purify(cell), "nothing to purify at level 0");
            Assert.AreEqual(0, _field.Level(cell));
            for (int i = 0; i < 6; i++) _field.Raise(cell);
            Assert.AreEqual(4, _field.Level(cell), "raising stops at level 4");
            Assert.IsFalse(_field.Raise(cell));
            Assert.IsFalse(_field.Raise(new Int2(-1, 99)), "off the map does nothing");
        }

        [Test]
        public void Purifying_lowers_one_level_and_stops_at_0()
        {
            var cell = new Int2(5, 5);
            _field.Set(cell, 4);
            Assert.IsTrue(_field.Purify(cell));
            Assert.AreEqual(3, _field.Level(cell));
            _field.Set(cell, 1);
            Assert.IsTrue(_field.Purify(cell));
            Assert.AreEqual(0, _field.Level(cell));
            Assert.IsFalse(_field.Purify(cell));
        }

        [Test]
        public void A_source_spreads_outwards_nearer_cells_first_and_caps_at_4()
        {
            var centre = new Int2(20, 20);
            var spreader = new CorruptionSpreader(new CorruptionSource(radius: 5, secondsPerStep: 1f), centre);
            spreader.Tick(1f, _field);
            Assert.AreEqual(1, _field.Level(centre));
            Assert.AreEqual(0, _field.Level(new Int2(21, 20)), "the front has not reached the next ring");
            spreader.Tick(1f, _field);
            Assert.AreEqual(2, _field.Level(centre));
            Assert.AreEqual(1, _field.Level(new Int2(21, 21)));
            Assert.AreEqual(0, _field.Level(new Int2(23, 20)));
            spreader.Tick(30f, _field);
            Assert.AreEqual(4, _field.Level(centre), "the heart is capped at 4");
            Assert.AreEqual(1, _field.Level(new Int2(25, 20)), "the edge of the radius reaches level 1");
            Assert.AreEqual(0, _field.Level(new Int2(26, 20)), "beyond the radius nothing spreads");
            Assert.Greater(_field.Level(new Int2(21, 20)), _field.Level(new Int2(24, 20)), "nearer ground is more corrupted");
        }

        [Test]
        public void A_faster_source_takes_ground_sooner()
        {
            var slow = new CorruptionSpreader(new CorruptionSource(3, 4f), new Int2(10, 10));
            var fast = new CorruptionSpreader(new CorruptionSource(3, 1f), new Int2(30, 30));
            slow.Tick(4f, _field);
            fast.Tick(4f, _field);
            Assert.Greater(_field.Level(new Int2(30, 30)), _field.Level(new Int2(10, 10)));
        }

        [Test]
        public void Spreading_never_lowers_a_cell()
        {
            var centre = new Int2(20, 20);
            _field.Set(centre, 4);
            new CorruptionSpreader(new CorruptionSource(3, 1f), centre).Tick(1f, _field);
            Assert.AreEqual(4, _field.Level(centre));
        }

        [Test]
        public void Spreading_off_the_edge_of_the_map_is_safe()
        {
            var spreader = new CorruptionSpreader(new CorruptionSource(4, 1f), new Int2(0, 0));
            Assert.DoesNotThrow(() => spreader.Tick(10f, _field));
            Assert.AreEqual(4, _field.Level(new Int2(0, 0)));
        }

        [Test]
        public void Seeding_gives_ground_at_least_the_level_and_keeps_higher_ones()
        {
            _field.Set(new Int2(20, 20), 4);
            _field.Seed(new Int2(20, 20), 2, 3);
            Assert.AreEqual(4, _field.Level(new Int2(20, 20)));
            Assert.AreEqual(3, _field.Level(new Int2(22, 22)));
            Assert.AreEqual(0, _field.Level(new Int2(23, 20)));
        }

        [Test]
        public void Building_needs_every_footprint_cell_at_level_2_or_more()
        {
            var origin = new Int2(10, 10);
            var size = new Int2(2, 2);
            Assert.IsFalse(_field.CanBuild(origin, size), "level 0");
            _field.Seed(origin, 1, 2);
            Assert.IsTrue(_field.CanBuild(origin, size), "level 2 is enough");
            _field.Set(new Int2(11, 11), 1);
            Assert.IsFalse(_field.CanBuild(origin, size), "one weak cell refuses it");
            _field.Set(new Int2(11, 11), 4);
            Assert.IsTrue(_field.CanBuild(origin, size), "level 4 is fine too");
            Assert.IsFalse(_field.CanBuild(new Int2(39, 39), size), "half off the map is not corrupted");
        }

        [Test]
        public void A_dying_demon_spreads_about_a_quarter_of_the_time_and_always_or_never_at_the_extremes()
        {
            var random = new Random(12345);
            int spread = 0;
            for (int i = 0; i < 4000; i++)
                if (CorruptionRules.DyingDemonSpreads(random)) spread++;
            Assert.That(spread, Is.InRange(850, 1150));
            Assert.IsTrue(CorruptionRules.DyingDemonSpreads(new Random(1), 1f));
            Assert.IsFalse(CorruptionRules.DyingDemonSpreads(new Random(1), 0f));
        }

        [Test]
        public void Demons_are_20_percent_weaker_below_level_2()
        {
            Assert.AreEqual(0.8f, CorruptionRules.DamageMultiplier(0), 1e-6f);
            Assert.AreEqual(0.8f, CorruptionRules.DamageMultiplier(1), 1e-6f);
            Assert.AreEqual(1f, CorruptionRules.DamageMultiplier(2), 1e-6f);
            Assert.AreEqual(1f, CorruptionRules.DamageMultiplier(4), 1e-6f);
        }

        [Test]
        public void A_purify_clock_is_due_once_per_interval()
        {
            var clock = new PurifyClock(20f);
            Assert.AreEqual(0, clock.Tick(19f));
            Assert.AreEqual(1, clock.Tick(1f));
            Assert.AreEqual(2, clock.Tick(40f));
        }
    }
}
