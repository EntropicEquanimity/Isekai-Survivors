using NUnit.Framework;

namespace Isekai.Tests
{
    public class TutorialRulesTests
    {
        [Test]
        public void A_two_part_step_shows_which_part_is_still_missing()
        {
            var progress = new TutorialProgress();
            progress.Note(TutorialFact.Moved);
            progress.Note(TutorialFact.Fought);
            Assert.AreEqual(TutorialStepId.MineStoneAndWood, progress.Current.Id);
            CollectionAssert.AreEqual(new[] { ("Stone", false), ("Wood", false) }, progress.Checklist());
            progress.Note(TutorialFact.MinedWood);
            CollectionAssert.AreEqual(new[] { ("Stone", false), ("Wood", true) }, progress.Checklist(), "Wood mined, Stone still to do");
            progress.Note(TutorialFact.MinedStone);
            Assert.AreEqual(TutorialStepId.BuildPortalOnCorruption, progress.Current.Id);
            Assert.IsEmpty(progress.Checklist(), "a one-part step has no checklist");
        }

        [Test]
        public void Steps_advance_only_when_their_own_conditions_are_met()
        {
            var progress = new TutorialProgress();

            progress.Note(TutorialFact.MinedStone);
            Assert.AreEqual(TutorialStepId.MoveAndFight, progress.Current.Id);

            progress.Note(TutorialFact.Moved);
            Assert.AreEqual(TutorialStepId.MoveAndFight, progress.Current.Id, "move alone is not enough");

            progress.Note(TutorialFact.Fought);
            Assert.AreEqual(TutorialStepId.MineStoneAndWood, progress.Current.Id);

            progress.Note(TutorialFact.MinedWood);
            Assert.AreEqual(TutorialStepId.BuildPortalOnCorruption, progress.Current.Id);
        }

        [Test]
        public void Rider_step_needs_both_a_sent_and_stopped_rider()
        {
            var progress = ReadyFor(TutorialStepId.StopWitnessRider);

            progress.Note(TutorialFact.RiderSent);
            Assert.AreEqual(TutorialStepId.StopWitnessRider, progress.Current.Id);

            progress.Note(TutorialFact.RiderStopped);
            Assert.AreEqual(TutorialStepId.RecoverFromDeath, progress.Current.Id);
        }

        [Test]
        public void Wisp_recovery_needs_death_then_return()
        {
            var progress = ReadyFor(TutorialStepId.RecoverFromDeath);

            progress.Note(TutorialFact.ReturnedFromWisp);
            Assert.AreEqual(TutorialStepId.RecoverFromDeath, progress.Current.Id, "returning first is not enough");

            progress.Note(TutorialFact.BecameWisp);
            Assert.AreEqual(TutorialStepId.DestroySeat, progress.Current.Id);
        }

        [Test]
        public void Sequence_completes_after_seat_destruction()
        {
            var progress = ReadyFor(TutorialStepId.DestroySeat);
            Assert.IsFalse(progress.IsComplete);
            progress.Note(TutorialFact.SeatDestroyed);
            Assert.IsTrue(progress.IsComplete);
            Assert.AreEqual(TutorialSteps.Count, progress.StepIndex);
        }

        private static TutorialProgress ReadyFor(TutorialStepId step)
        {
            var progress = new TutorialProgress();
            if (step == TutorialStepId.MoveAndFight) return progress;
            progress.Note(TutorialFact.Moved);
            progress.Note(TutorialFact.Fought);
            if (step == TutorialStepId.MineStoneAndWood) return progress;
            progress.Note(TutorialFact.MinedStone);
            progress.Note(TutorialFact.MinedWood);
            if (step == TutorialStepId.BuildPortalOnCorruption) return progress;
            progress.Note(TutorialFact.PortalBuilt);
            if (step == TutorialStepId.SetPortalOrderAndRally) return progress;
            progress.Note(TutorialFact.PortalOrderSet);
            progress.Note(TutorialFact.PortalRallySet);
            if (step == TutorialStepId.WatchCorruptionSpread) return progress;
            progress.Note(TutorialFact.CorruptionSpread);
            if (step == TutorialStepId.LearnVigilance) return progress;
            progress.Note(TutorialFact.VigilanceRaised);
            if (step == TutorialStepId.StopWitnessRider) return progress;
            progress.Note(TutorialFact.RiderSent);
            progress.Note(TutorialFact.RiderStopped);
            if (step == TutorialStepId.RecoverFromDeath) return progress;
            progress.Note(TutorialFact.BecameWisp);
            progress.Note(TutorialFact.ReturnedFromWisp);
            return progress;
        }
    }
}
