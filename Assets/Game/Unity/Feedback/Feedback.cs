using System;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// The named moments gameplay announces so sound (and the visual effects) can answer them without gameplay code
    /// knowing about either. Add a cue here, raise it from <see cref="FeedbackRaiser"/>, give it a sound in
    /// <see cref="SfxCues"/> and the <see cref="SfxLibrary"/>. New values go at the end: the library asset stores the numbers.
    /// </summary>
    public enum Cue
    {
        Hit,
        Death,
        BuildPlaced,
        BuildDone,
        Mined,
        SoulsGained,
        Wisp,
        Revive,
        Possess,
        Summon,
        Alarm,
        Rider,
        Wave,
        VigilanceStage,
        CorruptionSpread,
        Capture,
        UiClick,
        UiHover,
    }

    /// <summary>One raised cue: what happened and, for something in the world, where.</summary>
    public readonly struct FeedbackCue
    {
        public FeedbackCue(Cue kind, Vector3 position, bool hasPosition)
        {
            Kind = kind;
            Position = position;
            HasPosition = hasPosition;
        }

        public Cue Kind { get; }

        /// <summary>World position of what happened; meaningful only when <see cref="HasPosition"/>.</summary>
        public Vector3 Position { get; }

        public bool HasPosition { get; }
    }

    /// <summary>
    /// The one place feedback cues are raised and heard. Gameplay (through <see cref="FeedbackRaiser"/>) raises; sound
    /// (<see cref="SfxPlayer"/>) and visual effects subscribe. Subscribers outlive Regions, so each one unsubscribes itself.
    /// </summary>
    public static class Feedback
    {
        /// <summary>A cue was raised. Handlers must be cheap: a horde fight raises many a frame.</summary>
        public static event Action<FeedbackCue> Raised;

        /// <summary>Raises a cue that happened at <paramref name="position"/>.</summary>
        public static void Raise(Cue kind, Vector3 position) => Send(new FeedbackCue(kind, position, true));

        /// <summary>Raises a cue with no place (an interface click, a Wave on its way).</summary>
        public static void Raise(Cue kind) => Send(new FeedbackCue(kind, default, false));

        private static void Send(FeedbackCue cue)
        {
            try { Raised?.Invoke(cue); }
            catch (Exception e) { Debug.LogException(e); }
        }
    }
}
