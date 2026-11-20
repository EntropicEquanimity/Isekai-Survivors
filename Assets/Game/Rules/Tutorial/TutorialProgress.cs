using System;

namespace Isekai
{
    /// <summary>The ordered steps of the guided first Region tutorial.</summary>
    public enum TutorialStepId
    {
        MoveAndFight,
        MineStoneAndWood,
        BuildPortalOnCorruption,
        SetPortalOrderAndRally,
        WatchCorruptionSpread,
        LearnVigilance,
        StopWitnessRider,
        RecoverFromDeath,
        DestroySeat,
    }

    /// <summary>One tutorial step: its id and short prompt text.</summary>
    public readonly struct TutorialStepDef
    {
        public TutorialStepDef(TutorialStepId id, string prompt)
        {
            Id = id;
            Prompt = prompt ?? throw new ArgumentNullException(nameof(prompt));
        }

        public TutorialStepId Id { get; }
        public string Prompt { get; }
    }

    /// <summary>All tutorial steps, in order, with prompt text.</summary>
    public static class TutorialSteps
    {
        private static readonly TutorialStepDef[] s_order =
        {
            new(TutorialStepId.MoveAndFight, "Move Jack and kill a Mortal."),
            new(TutorialStepId.MineStoneAndWood, "Mine Stone and Wood."),
            new(TutorialStepId.BuildPortalOnCorruption, "Build a Portal on corruption. Not enough corruption blocks it."),
            new(TutorialStepId.SetPortalOrderAndRally, "Set that Portal's order and rally point."),
            new(TutorialStepId.WatchCorruptionSpread, "Hold ground until corruption spreads."),
            new(TutorialStepId.LearnVigilance, "Stay hidden. Kills and sightings raise Vigilance."),
            new(TutorialStepId.StopWitnessRider, "A witness can send a rider. Stop the rider before the Seat."),
            new(TutorialStepId.RecoverFromDeath, "Let Jack die, then reach the Anchor as a wisp or possess a demon."),
            new(TutorialStepId.DestroySeat, "Destroy the Seat."),
        };

        public static int Count => s_order.Length;

        public static TutorialStepDef At(int index) => s_order[index];
    }

    /// <summary>Facts the tutorial watches from gameplay events and state.</summary>
    public enum TutorialFact
    {
        Moved,
        Fought,
        MinedStone,
        MinedWood,
        PortalBuilt,
        PortalOrderSet,
        PortalRallySet,
        CorruptionSpread,
        VigilanceRaised,
        RiderSent,
        RiderStopped,
        BecameWisp,
        ReturnedFromWisp,
        SeatDestroyed,
    }

    /// <summary>
    /// Engine-free tutorial progression. Feed gameplay facts in any order; it advances through the fixed step order when
    /// each step's conditions are met.
    /// </summary>
    public sealed class TutorialProgress
    {
        private ulong _facts;

        /// <summary>The current step index, 0..Count; Count means complete.</summary>
        public int StepIndex { get; private set; }

        public bool IsComplete => StepIndex >= TutorialSteps.Count;

        public TutorialStepDef Current => IsComplete ? default : TutorialSteps.At(StepIndex);

        public bool Has(TutorialFact fact) => (_facts & BitOf(fact)) != 0UL;

        /// <summary>Records a fact and advances while the next step is satisfied. Returns true when at least one step advanced.</summary>
        public bool Note(TutorialFact fact)
        {
            _facts |= BitOf(fact);
            int before = StepIndex;
            while (!IsComplete && IsSatisfied(TutorialSteps.At(StepIndex).Id)) StepIndex++;
            return StepIndex != before;
        }

        private bool IsSatisfied(TutorialStepId step)
        {
            return step switch
            {
                TutorialStepId.MoveAndFight => Has(TutorialFact.Moved) && Has(TutorialFact.Fought),
                TutorialStepId.MineStoneAndWood => Has(TutorialFact.MinedStone) && Has(TutorialFact.MinedWood),
                TutorialStepId.BuildPortalOnCorruption => Has(TutorialFact.PortalBuilt),
                TutorialStepId.SetPortalOrderAndRally => Has(TutorialFact.PortalOrderSet) && Has(TutorialFact.PortalRallySet),
                TutorialStepId.WatchCorruptionSpread => Has(TutorialFact.CorruptionSpread),
                TutorialStepId.LearnVigilance => Has(TutorialFact.VigilanceRaised),
                TutorialStepId.StopWitnessRider => Has(TutorialFact.RiderSent) && Has(TutorialFact.RiderStopped),
                TutorialStepId.RecoverFromDeath => Has(TutorialFact.BecameWisp) && Has(TutorialFact.ReturnedFromWisp),
                TutorialStepId.DestroySeat => Has(TutorialFact.SeatDestroyed),
                _ => false,
            };
        }

        private static ulong BitOf(TutorialFact fact) => 1UL << (int)fact;
    }
}
