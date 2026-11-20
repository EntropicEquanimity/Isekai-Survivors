using NUnit.Framework;

namespace Isekai.Tests
{
    /// <summary>The day-night clock's rules: timing, darkness and vision scaling.</summary>
    public class DayNightCycleTests
    {
        [Test]
        public void The_default_cycle_is_8_minutes_with_3_minutes_of_night()
        {
            var cycle = new DayNightCycle();
            Assert.AreEqual(480f, cycle.CycleSeconds, 1e-4f);
            Assert.AreEqual(180f, cycle.NightSeconds, 1e-4f);
            Assert.AreEqual(300f, cycle.NightStart, 1e-4f);
        }

        [Test]
        public void It_is_day_before_night_start_and_night_after_it()
        {
            var cycle = new DayNightCycle(480f, 180f, 0f);
            cycle.SetTimeInCycle(299.9f);
            Assert.IsFalse(cycle.IsNight);
            cycle.SetTimeInCycle(300f);
            Assert.IsTrue(cycle.IsNight);
            cycle.SetTimeInCycle(0.5f);
            Assert.IsFalse(cycle.IsNight);
        }

        [Test]
        public void The_clock_reports_time_until_the_next_change()
        {
            var cycle = new DayNightCycle(480f, 180f, 0f);
            cycle.SetTimeInCycle(120f);
            Assert.AreEqual(180f, cycle.SecondsUntilChange, 1e-4f);
            cycle.SetTimeInCycle(360f);
            Assert.AreEqual(120f, cycle.SecondsUntilChange, 1e-4f);
        }

        [Test]
        public void Darkness_eases_at_dusk_and_dawn()
        {
            var cycle = new DayNightCycle(100f, 40f, 10f);
            cycle.SetTimeInCycle(48f);
            Assert.AreEqual(0f, cycle.Darkness01, 1e-4f);
            cycle.SetTimeInCycle(55f);
            Assert.Greater(cycle.Darkness01, 0f);
            Assert.Less(cycle.Darkness01, 1f);
            cycle.SetTimeInCycle(70f);
            Assert.AreEqual(1f, cycle.Darkness01, 1e-4f);
            cycle.SetTimeInCycle(5f);
            Assert.Greater(cycle.Darkness01, 0f);
            Assert.Less(cycle.Darkness01, 1f);
            cycle.SetTimeInCycle(12f);
            Assert.AreEqual(0f, cycle.Darkness01, 1e-4f);
        }

        [Test]
        public void Vision_is_full_by_day_and_half_by_full_night()
        {
            var cycle = new DayNightCycle(480f, 180f, 0f);
            cycle.SetTimeInCycle(120f);
            Assert.AreEqual(1f, cycle.VisionMultiplier, 1e-4f);
            cycle.SetTimeInCycle(360f);
            Assert.AreEqual(0.5f, cycle.VisionMultiplier, 1e-4f);
        }

        [Test]
        public void Advancing_wraps_around_the_cycle()
        {
            var cycle = new DayNightCycle(12f, 4f, 0f);
            cycle.SetTimeInCycle(11.5f);
            cycle.Advance(1f);
            Assert.Less(cycle.TimeInCycle, 1f);
            Assert.IsFalse(cycle.IsNight);
        }
    }
}
