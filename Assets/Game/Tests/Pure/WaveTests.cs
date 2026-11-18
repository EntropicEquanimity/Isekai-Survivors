using System.Collections.Generic;
using NUnit.Framework;

namespace Isekai.Tests
{
    public class WaveScheduleTests
    {
        [Test]
        public void Releases_each_wave_once_in_time_order()
        {
            var schedule = new WaveSchedule(new[]
            {
                new Wave(120f, WaveType.Mix, 30, isFinal: true),
                new Wave(30f, WaveType.Horde, 10),
                new Wave(60f, WaveType.Squad, 5),
            });
            var due = new List<Wave>();
            schedule.Release(10f, due);
            Assert.IsEmpty(due);
            Assert.AreEqual(20f, schedule.SecondsUntilNext(10f));

            schedule.Release(65f, due);
            CollectionAssert.AreEqual(new[] { WaveType.Horde, WaveType.Squad }, due.ConvertAll(w => w.Type));
            schedule.Release(65f, due);
            Assert.AreEqual(2, due.Count, "not released twice");

            schedule.Release(500f, due);
            Assert.IsTrue(due[2].IsFinal);
            Assert.IsTrue(schedule.IsFinished);
            Assert.AreEqual(float.PositiveInfinity, schedule.SecondsUntilNext(500f));
        }
    }

    public class EscalationTests
    {
        [Test]
        public void Grows_with_run_time_and_depth_and_is_capped()
        {
            var e = new Escalation { PerMinute = 0.1f, PerDepth = 0.5f, Max = 3f };
            Assert.AreEqual(1f, e.Multiplier(0f, 0), 1e-4f);
            Assert.AreEqual(2f, e.Multiplier(600f, 0), 1e-4f, "10 minutes at 0.1 per minute");
            Assert.AreEqual(2.5f, e.Multiplier(600f, 1), 1e-4f);
            Assert.AreEqual(3f, e.Multiplier(6000f, 4), 1e-4f, "capped");
        }

        [Test]
        public void Scaled_counts_round_and_keep_at_least_one()
        {
            Assert.AreEqual(15, Escalation.Scale(10, 1.5f));
            Assert.AreEqual(1, Escalation.Scale(1, 0.2f));
            Assert.AreEqual(0, Escalation.Scale(0, 3f));
        }
    }

    public class RegionCaptureTests
    {
        [Test]
        public void Needs_both_the_bases_and_the_final_wave_in_either_order()
        {
            var capture = new RegionCapture();
            capture.AddEnemyBase();
            capture.AddEnemyBase();
            capture.FinalWaveSpawned(2);
            capture.FinalWaveEnemyDied();
            capture.FinalWaveEnemyDied();
            Assert.IsTrue(capture.FinalWaveDefeated);
            Assert.IsFalse(capture.IsCaptured, "bases still stand");
            capture.EnemyBaseDestroyed();
            capture.EnemyBaseDestroyed();
            Assert.IsTrue(capture.IsCaptured);
        }

        [Test]
        public void The_final_wave_only_counts_once_released()
        {
            var capture = new RegionCapture();
            capture.AddEnemyBase();
            capture.EnemyBaseDestroyed();
            Assert.IsFalse(capture.IsCaptured, "the final wave hasn't come yet");
            capture.FinalWaveSpawned(1);
            Assert.IsFalse(capture.IsCaptured);
            capture.FinalWaveEnemyDied();
            Assert.IsTrue(capture.IsCaptured);
        }
    }
}
