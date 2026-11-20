namespace Isekai
{
    /// <summary>
    /// What a unit can be doing, shared by mortals and (later) demons. A brain does not code behaviour per unit type: it
    /// scores each of these from its <see cref="BehaviourProfile"/> and the situation, and does the best one.
    /// </summary>
    public enum BehaviourAction
    {
        /// <summary>Stand about. The fallback: scores a little so it wins only when nothing else does.</summary>
        Idle,

        /// <summary>Go about its business near home (a peasant in the fields).</summary>
        Work,

        /// <summary>Walk a route round its settlement and the roads near it.</summary>
        Patrol,

        /// <summary>Go for whoever it sees, along the horde's path field, and hit them.</summary>
        Fight,

        /// <summary>Run to the nearest stronghold.</summary>
        Flee,

        /// <summary>Walk back to where it belongs.</summary>
        ReturnHome,

        /// <summary>Remove corruption from the ground nearby.</summary>
        Purify,
    }

    /// <summary>Everything a weight can look at: a number from 0 to 1 (or more) read from the situation (<see cref="BehaviourContext.Value"/>).</summary>
    public enum BehaviourFactor
    {
        /// <summary>Always 1: a constant.</summary>
        Always,

        /// <summary>Health fraction, 1 full to 0 dead. A trait's "flees below 75% health" is a term on this.</summary>
        Health,

        /// <summary>1 while something hostile is in sight, else 0.</summary>
        Threat,

        /// <summary>How near the threat in sight is: 1 beside it, 0 at the edge of sight (0 with no threat).</summary>
        ThreatClose,

        /// <summary>Fright that lingers: 1 when it saw a threat or was hit, fading to 0 over a few seconds.</summary>
        Fear,

        /// <summary>How far from home it is as a share of its leash: 0 at home, 1 at the leash, more beyond.</summary>
        Far,

        /// <summary>1 at night, else 0 (the day-and-night cycle sets it).</summary>
        Night,

        /// <summary>1 when there is corruption within reach to remove, else 0.</summary>
        Corruption,

        /// <summary>1 while it has seen or heard an alarm (raised by the alarm system), else 0.</summary>
        Alarm,
    }
}
