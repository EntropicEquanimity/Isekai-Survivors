using System;

namespace Isekai
{
    /// <summary>
    /// The situation a brain scores its actions in. Each brain keeps one and refills it every think, so deciding
    /// allocates nothing. Use <see cref="Calm"/> for a unit in full health with nothing going on.
    /// </summary>
    public struct BehaviourContext
    {
        /// <summary>Health fraction, 0 to 1.</summary>
        public float Health;

        /// <summary>Something hostile is in sight, and how near it is (0 far, 1 beside it).</summary>
        public bool ThreatSeen;
        public float ThreatCloseness;

        /// <summary>Lingering fright, 0 to 1.</summary>
        public float Fear;

        /// <summary>Distance from home and the distance it likes to stay within, in cells.</summary>
        public float DistanceHome;
        public float Leash;

        public bool Night;
        public bool CorruptionNear;
        public bool AlarmSeen;

        /// <summary>A unit at full health, at home, with nothing in sight and nothing wrong.</summary>
        public static BehaviourContext Calm => new() { Health = 1f, Leash = 10f };

        /// <summary>The value of <paramref name="factor"/> in this situation.</summary>
        public readonly float Value(BehaviourFactor factor) => factor switch
        {
            BehaviourFactor.Always => 1f,
            BehaviourFactor.Health => Health,
            BehaviourFactor.Threat => ThreatSeen ? 1f : 0f,
            BehaviourFactor.ThreatClose => ThreatSeen ? ThreatCloseness : 0f,
            BehaviourFactor.Fear => Fear,
            BehaviourFactor.Far => Leash > 0f ? DistanceHome / Leash : 0f,
            BehaviourFactor.Night => Night ? 1f : 0f,
            BehaviourFactor.Corruption => CorruptionNear ? 1f : 0f,
            BehaviourFactor.Alarm => AlarmSeen ? 1f : 0f,
            _ => throw new ArgumentOutOfRangeException(nameof(factor)),
        };
    }
}
