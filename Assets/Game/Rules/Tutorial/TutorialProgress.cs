using System;
using System.Collections.Generic;

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
            new(TutorialStepId.MoveAndFight, "Move Jack with WASD and kill a Mortal. Stand still and he fights whatever is near."),
            new(TutorialStepId.MineStoneAndWood, "Walk Jack into a grey rock for Stone and into a dead tree for Wood. The arrow marks the nearest."),
            new(TutorialStepId.BuildPortalOnCorruption, "Press 1, then click corrupted (dark purple) ground near the Anchor to build a Portal."),
            new(TutorialStepId.SetPortalOrderAndRally, "Click your Portal. Pick an order other than Guard, then set its rally point."),
            new(TutorialStepId.WatchCorruptionSpread, "Hold ground until corruption spreads."),
            new(TutorialStepId.LearnVigilance, "Stay hidden. Kills and sightings raise Vigilance."),
            new(TutorialStepId.StopWitnessRider, "A witness can send a rider. Stop the rider before the Seat."),
            new(TutorialStepId.RecoverFromDeath, "Press {body} to leave Jack's body (or let it die). As a wisp, fly over the Anchor or one of your demons and press {body}."),
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

        /// <summary>
        /// What the current step waits for and which of it is done, when it needs more than one thing (Stone, Wood); empty
        /// for a one-thing step or once complete. Shown under the prompt, so a half-done step never looks stuck.
        /// </summary>
        public IReadOnlyList<(string Label, bool Done)> Checklist()
        {
            if (IsComplete) return Nothing;
            return Current.Id switch
            {
                TutorialStepId.MoveAndFight => Both("Moved", TutorialFact.Moved, "Killed a Mortal", TutorialFact.Fought),
                TutorialStepId.MineStoneAndWood => Both("Stone", TutorialFact.MinedStone, "Wood", TutorialFact.MinedWood),
                TutorialStepId.SetPortalOrderAndRally => Both("Order", TutorialFact.PortalOrderSet, "Rally point", TutorialFact.PortalRallySet),
                TutorialStepId.StopWitnessRider => Both("Rider sent", TutorialFact.RiderSent, "Rider stopped", TutorialFact.RiderStopped),
                TutorialStepId.RecoverFromDeath => Both("Wisp", TutorialFact.BecameWisp, "New body", TutorialFact.ReturnedFromWisp),
                _ => Nothing,
            };
        }

        private static readonly (string Label, bool Done)[] Nothing = Array.Empty<(string, bool)>();

        private IReadOnlyList<(string Label, bool Done)> Both(string a, TutorialFact factA, string b, TutorialFact factB) =>
            new[] { (a, Has(factA)), (b, Has(factB)) };

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
