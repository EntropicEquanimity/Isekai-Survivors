using NUnit.Framework;

namespace Isekai.Tests
{
    public class VigilanceMachineTests
    {
        [Test]
        public void Starts_unaware()
        {
            var machine = new VigilanceMachine();
            Assert.AreEqual(VigilanceStage.Unaware, machine.Stage);
            Assert.AreEqual(0f, machine.Points);
        }

        [Test]
        public void A_mortal_kill_raises_a_little_and_a_witnessed_kill_more()
        {
            var unseen = new VigilanceMachine();
            var witnessed = new VigilanceMachine();
            Assert.IsTrue(unseen.OnKill(UnitKind.Mortal, witnessed: false));
            Assert.IsTrue(witnessed.OnKill(UnitKind.Mortal, witnessed: true));

            Assert.Greater(unseen.Points, 0f);
            Assert.Greater(witnessed.Points, unseen.Points);
            Assert.Less(unseen.Points, VigilanceTuning.Threshold(VigilanceStage.Uneasy) , "one kill is only a little");
        }

        [TestCase(UnitKind.Wildlife)]
        [TestCase(UnitKind.Demon)]
        [TestCase(UnitKind.Building)]
        [TestCase(UnitKind.Character)]
        public void Only_mortal_kills_count(UnitKind kind)
        {
            var machine = new VigilanceMachine();
            Assert.IsFalse(machine.OnKill(kind, witnessed: true));
            Assert.AreEqual(0f, machine.Points);
        }

        [Test]
        public void Enough_kills_reach_the_next_stage()
        {
            var machine = new VigilanceMachine();
            while (machine.Stage == VigilanceStage.Unaware) machine.OnKill(UnitKind.Mortal, witnessed: false);
            Assert.AreEqual(VigilanceStage.Uneasy, machine.Stage);
            Assert.AreEqual(VigilanceStage.Uneasy, machine.Highest);
        }

        [Test]
        public void A_rider_reaching_the_Seat_raises_a_full_stage()
        {
            var machine = new VigilanceMachine();
            machine.OnKill(UnitKind.Mortal, witnessed: false);

            Assert.IsTrue(machine.OnRiderReachedSeat());
            Assert.AreEqual(VigilanceStage.Uneasy, machine.Stage);
            Assert.IsTrue(machine.OnRiderReachedSeat());
            Assert.AreEqual(VigilanceStage.Alarmed, machine.Stage);
            Assert.AreEqual(VigilanceTuning.Threshold(VigilanceStage.Alarmed), machine.Points, 1e-4f);
        }

        [Test]
        public void A_rider_changes_nothing_at_Crusade()
        {
            var machine = new VigilanceMachine();
            for (int i = 0; i < 4; i++) machine.RaiseStage();
            Assert.AreEqual(VigilanceStage.Crusade, machine.Stage);
            Assert.IsFalse(machine.OnRiderReachedSeat());
            Assert.AreEqual(VigilanceStage.Crusade, machine.Stage);
        }

        [Test]
        public void Being_spotted_raises_it()
        {
            var machine = new VigilanceMachine();
            Assert.IsTrue(machine.OnSpotted());
            Assert.Greater(machine.Points, 0f);
        }

        [Test]
        public void Progress_to_the_next_stage_runs_from_zero_to_one()
        {
            var machine = new VigilanceMachine();
            Assert.AreEqual(0f, machine.ProgressToNext, 1e-4f);
            machine.OnKill(UnitKind.Mortal, witnessed: false);
            Assert.That(machine.ProgressToNext, Is.GreaterThan(0f).And.LessThan(1f));
            for (int i = 0; i < 4; i++) machine.RaiseStage();
            Assert.AreEqual(1f, machine.ProgressToNext, "Crusade has no next stage");
        }

        [Test]
        public void Nothing_decays_until_nothing_has_been_seen_for_a_while()
        {
            var machine = new VigilanceMachine();
            machine.OnKill(UnitKind.Mortal, witnessed: false);
            float before = machine.Points;
            machine.Tick(VigilanceTuning.DecayGraceSeconds * 0.5f);
            Assert.AreEqual(before, machine.Points, "still fresh");
        }

        [Test]
        public void Decay_is_slow_and_stops_at_the_floor_one_stage_below_the_highest()
        {
            var machine = new VigilanceMachine();
            for (int i = 0; i < 4; i++) machine.RaiseStage();   // Crusade
            machine.Tick(VigilanceTuning.DecayGraceSeconds);
            machine.Tick(1f);
            Assert.Less(machine.Points, VigilanceTuning.Threshold(VigilanceStage.Crusade));
            Assert.Greater(machine.Points, VigilanceTuning.Threshold(VigilanceStage.Crusade) - 5f, "slowly");

            machine.Tick(100000f);
            Assert.AreEqual(VigilanceStage.Mobilized, machine.Stage);
            Assert.AreEqual(VigilanceTuning.Threshold(VigilanceStage.Mobilized), machine.Points, 1e-4f);
            Assert.AreEqual(VigilanceStage.Crusade, machine.Highest, "the highest reached is remembered");
        }

        [Test]
        public void An_unaware_machine_never_decays_below_zero()
        {
            var machine = new VigilanceMachine();
            machine.Tick(100000f);
            Assert.AreEqual(0f, machine.Points);
        }

        [Test]
        public void Something_seen_holds_decay_off_again()
        {
            var machine = new VigilanceMachine();
            for (int i = 0; i < 3; i++) machine.RaiseStage();
            machine.Tick(VigilanceTuning.DecayGraceSeconds + 5f);
            Assert.Less(machine.Points, VigilanceTuning.Threshold(VigilanceStage.Mobilized));

            machine.OnSpotted();
            float before = machine.Points;
            machine.Tick(VigilanceTuning.DecayGraceSeconds * 0.5f);
            Assert.AreEqual(before, machine.Points);
        }

        [Test]
        public void Lowering_drops_a_stage_and_resets_the_floor()
        {
            var machine = new VigilanceMachine();
            for (int i = 0; i < 3; i++) machine.RaiseStage();
            Assert.IsTrue(machine.LowerStage());
            Assert.AreEqual(VigilanceStage.Alarmed, machine.Stage);
            Assert.AreEqual(VigilanceStage.Alarmed, machine.Highest);
            Assert.IsFalse(new VigilanceMachine().LowerStage());
        }
    }

    public class VigilanceWavePlannerTests
    {
        private static VigilanceWavePlanner Planner(int cap = 2, float repeat = 10f) => new(cap, repeat);

        [Test]
        public void Nothing_is_owed_at_the_start()
        {
            Assert.IsFalse(Planner().CanSend);
        }

        [Test]
        public void One_wave_is_owed_for_each_new_stage()
        {
            VigilanceWavePlanner planner = Planner();
            planner.Reached(VigilanceStage.Uneasy);
            Assert.AreEqual(1, planner.Pending);
            planner.Reached(VigilanceStage.Uneasy);
            Assert.AreEqual(1, planner.Pending, "the same stage again is not new");
            planner.Reached(VigilanceStage.Mobilized);
            Assert.AreEqual(3, planner.Pending, "skipping stages still owes one for each");
        }

        [Test]
        public void A_stage_lost_to_decay_and_regained_is_not_new()
        {
            var machine = new VigilanceMachine();
            VigilanceWavePlanner planner = Planner();
            machine.RaiseStage();
            machine.RaiseStage();
            planner.Reached(machine.Highest);
            Assert.AreEqual(2, planner.Pending);

            machine.Tick(100000f);                 // back down to Uneasy
            planner.Reached(machine.Highest);
            machine.RaiseStage();                  // Alarmed again
            planner.Reached(machine.Highest);
            Assert.AreEqual(2, planner.Pending, "Alarmed was already reached");
        }

        [Test]
        public void A_stage_reached_again_after_lowering_is_new()
        {
            var machine = new VigilanceMachine();
            VigilanceWavePlanner planner = Planner();
            machine.RaiseStage();
            planner.Reached(machine.Highest);
            machine.LowerStage();
            planner.Reached(machine.Highest);
            machine.RaiseStage();
            planner.Reached(machine.Highest);
            Assert.AreEqual(2, planner.Pending);
        }

        [Test]
        public void At_most_the_cap_of_waves_are_alive()
        {
            VigilanceWavePlanner planner = Planner(cap: 2);
            planner.Reached(VigilanceStage.Mobilized);   // three owed

            Assert.IsTrue(planner.Sent());
            Assert.IsTrue(planner.Sent());
            Assert.IsFalse(planner.CanSend);
            Assert.IsFalse(planner.Sent());
            Assert.AreEqual(2, planner.Alive);
            Assert.AreEqual(1, planner.Pending, "the third waits");
        }

        [Test]
        public void A_waiting_wave_goes_when_one_ends()
        {
            VigilanceWavePlanner planner = Planner(cap: 2);
            planner.Reached(VigilanceStage.Mobilized);
            planner.Sent();
            planner.Sent();
            planner.Ended();

            Assert.IsTrue(planner.CanSend);
            Assert.IsTrue(planner.Sent());
            Assert.AreEqual(0, planner.Pending);
        }

        [Test]
        public void Waves_cascade_within_the_cap()
        {
            // Vigilance rises while a Wave is alive: a second stage sends a second Wave, a third waits.
            VigilanceWavePlanner planner = Planner(cap: 2);
            planner.Reached(VigilanceStage.Uneasy);
            planner.Sent();
            planner.Reached(VigilanceStage.Alarmed);
            Assert.IsTrue(planner.Sent());
            planner.Reached(VigilanceStage.Mobilized);
            Assert.IsFalse(planner.CanSend);
            Assert.AreEqual(2, planner.Alive);
        }

        [Test]
        public void Ending_with_none_alive_does_nothing()
        {
            VigilanceWavePlanner planner = Planner();
            planner.Ended();
            Assert.AreEqual(0, planner.Alive);
        }

        [Test]
        public void At_Crusade_a_wave_is_owed_every_repeat_period()
        {
            VigilanceWavePlanner planner = Planner(repeat: 10f);
            planner.Tick(9f, VigilanceStage.Crusade);
            Assert.AreEqual(0, planner.Pending);
            planner.Tick(1f, VigilanceStage.Crusade);
            Assert.AreEqual(1, planner.Pending);
            planner.Sent();
            planner.Ended();
            planner.Tick(10f, VigilanceStage.Crusade);
            Assert.AreEqual(1, planner.Pending);
        }

        [Test]
        public void Below_Crusade_there_is_no_repeat_and_the_clock_restarts()
        {
            VigilanceWavePlanner planner = Planner(repeat: 10f);
            planner.Tick(8f, VigilanceStage.Crusade);
            planner.Tick(100f, VigilanceStage.Mobilized);
            planner.Tick(8f, VigilanceStage.Crusade);
            Assert.AreEqual(0, planner.Pending);
        }

        [Test]
        public void At_Crusade_with_the_cap_reached_nothing_more_is_owed()
        {
            VigilanceWavePlanner planner = Planner(cap: 2, repeat: 5f);
            planner.OweNow();
            planner.OweNow();
            planner.Sent();
            planner.Sent();
            Assert.AreEqual(2, planner.Alive);

            planner.Tick(500f, VigilanceStage.Crusade);
            Assert.AreEqual(0, planner.Pending, "no backlog while the cap is reached");
            Assert.IsFalse(planner.OweNow(), "not even from the debug menu");
            Assert.IsFalse(planner.CanSend);
        }

        [Test]
        public void A_cap_below_one_is_one()
        {
            VigilanceWavePlanner planner = Planner(cap: 0);
            planner.Reached(VigilanceStage.Alarmed);
            Assert.IsTrue(planner.Sent());
            Assert.IsFalse(planner.CanSend);
        }
    }
}
