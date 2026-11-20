using System;
using System.Collections.Generic;

namespace Isekai
{
    /// <summary>The human mortals of a Region (spec: "Humans only"): the Mortal units, plus the peasants who work the land.</summary>
    public enum MortalType
    {
        Peasant,
        Soldier,
        Swordsman,
        Archer,
        ArmoredAxeman,
        Knight,
        Lancer,
        Priest,
    }

    /// <summary>What a mortal is for: it decides which spawn markers it can come from and what the Region treats it as.</summary>
    public enum MortalRole { Civilian, Guard, Priest }

    /// <summary>
    /// A mortal type as data: its role, how it decides (<see cref="Behaviour"/>), how far it strays from home, and whether
    /// it shoots from range. Stats (health, damage, speed, reach) are on the type's prefab, where the horde's mortals
    /// already keep them. Traits will not edit this: they make their own copy of <see cref="Behaviour"/>.
    /// </summary>
    public sealed class MortalProfile
    {
        public MortalProfile(MortalType type, MortalRole role, BehaviourProfile behaviour, float leashCells, bool ranged = false)
        {
            Type = type;
            Role = role;
            Behaviour = behaviour;
            LeashCells = leashCells;
            Ranged = ranged;
        }

        public MortalType Type { get; }
        public MortalRole Role { get; }
        public BehaviourProfile Behaviour { get; }

        /// <summary>How far from home it goes before it feels it should be back, in cells.</summary>
        public float LeashCells { get; }

        /// <summary>Shoots from reach and holds still while it does, instead of walking up to its target.</summary>
        public bool Ranged { get; }
    }

    /// <summary>Every number of the mortals' behaviour in one place, to tune.</summary>
    public static class MortalTuning
    {
        /// <summary>A mortal thinks (re-scores its actions) this often, staggered across frames.</summary>
        public const float ThinkSeconds = 0.3f;

        /// <summary>Fear after a fright fades over this long.</summary>
        public const float FearSeconds = 8f;

        /// <summary>Speeds in cells per second (a fighting mortal walks the horde's crowd speed).</summary>
        public const float WalkSpeed = 1f;
        public const float ReturnSpeed = 1.4f;
        public const float FleeSpeed = 2.4f;

        /// <summary>Within this of a spot (cells) counts as there.</summary>
        public const float ArriveDistance = 0.3f;

        /// <summary>Peasants and priests wander this far from where they start while working, in cells.</summary>
        public const int WorkRadius = 4;

        /// <summary>Seconds a worker stands at a spot before the next, and a patrol at a waypoint.</summary>
        public const float WorkPauseMin = 1f;
        public const float WorkPauseMax = 4f;
        public const float PatrolPauseMin = 0.5f;
        public const float PatrolPauseMax = 2f;

        /// <summary>How far a priest looks for corruption to remove, and how close it gets to purify it, in cells.</summary>
        public const int PurifyScanRadius = 5;
        public const float PurifyReach = 1.5f;

        /// <summary>The wisp's drain doubles within this many cells of a living priest.</summary>
        public const float PriestAuraCells = 7f;

        /// <summary>A mortal fleeing is safe inside a stronghold; a patrol that wanders this far from home turns back sooner.</summary>
        public const float RangedHoldShare = 0.85f;

        /// <summary>How many cells of a road beside a settlement its guards patrol.</summary>
        public const int PatrolRoadCells = 14;

        /// <summary>How long a stuck mortal tries before choosing somewhere else, in seconds.</summary>
        public const float StuckSeconds = 0.6f;
    }

    /// <summary>The mortals' profiles: how each type decides. Weights are tuned so a number of about 1 is a mild urge and 3 a strong one.</summary>
    public static class MortalProfiles
    {
        private static readonly Dictionary<MortalType, MortalProfile> s_all = Build();

        /// <summary>The profile of a type.</summary>
        public static MortalProfile For(MortalType type) => s_all[type];

        /// <summary>Every type, in roster order.</summary>
        public static IEnumerable<MortalType> Roster => (MortalType[])Enum.GetValues(typeof(MortalType));

        private static BehaviourTerm T(BehaviourFactor factor, float weight, float from = 0f, float to = 1f) => new(factor, weight, from, to);

        private static Dictionary<MortalType, MortalProfile> Build()
        {
            const BehaviourAction Idle = BehaviourAction.Idle, Work = BehaviourAction.Work, Patrol = BehaviourAction.Patrol,
                Fight = BehaviourAction.Fight, Flee = BehaviourAction.Flee, Return = BehaviourAction.ReturnHome, Purify = BehaviourAction.Purify;
            const BehaviourFactor Health = BehaviourFactor.Health, Threat = BehaviourFactor.Threat, Fear = BehaviourFactor.Fear,
                Far = BehaviourFactor.Far, Alarm = BehaviourFactor.Alarm, Corruption = BehaviourFactor.Corruption,
                Night = BehaviourFactor.Night;

            // Peasants work, and run at the sight of anyone hostile or a hit; they go home when they have strayed.
            BehaviourProfile peasant = new BehaviourProfile("Peasant")
                .Allow(Idle, 0.2f)
                .Allow(Work, 0.6f)
                .Allow(Flee, 0f, T(Fear, 3f), T(Threat, 1f), T(Alarm, 2f))
                .Allow(Return, 0f, T(Far, 1.5f, 0.5f, 1f));

            // A guard patrols and fights whatever it sees. It flees only when hurt AND scared: the base cancels the
            // health term when it is not afraid, so a wounded guard at peace does not hide forever. At night it stops
            // patrolling and pulls back home ("At night patrols pull back toward settlements"), still fighting what
            // it sees.
            BehaviourProfile Guard(float fightWeight, float fleeBelow) => new BehaviourProfile("Guard")
                .Allow(Idle, 0.2f)
                .Allow(Patrol, 0.6f, T(Night, -0.5f))
                .Allow(Fight, 0f, T(Threat, fightWeight))
                .Allow(Flee, -3f, T(Fear, 3f), T(Health, 3f, fleeBelow, fleeBelow * 0.5f))
                .Allow(Return, 0f, T(Far, 1.5f, 0.6f, 1.2f), T(Night, 0.35f));

            // A priest tends the settlement, purifies corrupted ground nearby and runs from trouble. It never fights.
            BehaviourProfile priest = new BehaviourProfile("Priest")
                .Allow(Idle, 0.3f)
                .Allow(Work, 0.5f)
                .Allow(Purify, 0f, T(Corruption, 2f))
                .Allow(Flee, 0f, T(Fear, 3f), T(Threat, 1f), T(Alarm, 2f))
                .Allow(Return, 0f, T(Far, 1.5f, 0.5f, 1f));

            return new Dictionary<MortalType, MortalProfile>
            {
                [MortalType.Peasant] = new MortalProfile(MortalType.Peasant, MortalRole.Civilian, peasant, 8f),
                [MortalType.Soldier] = new MortalProfile(MortalType.Soldier, MortalRole.Guard, Guard(2.5f, 0.3f), 14f),
                [MortalType.Swordsman] = new MortalProfile(MortalType.Swordsman, MortalRole.Guard, Guard(2.5f, 0.3f), 14f),
                [MortalType.Archer] = new MortalProfile(MortalType.Archer, MortalRole.Guard, Guard(2.5f, 0.5f), 12f, ranged: true),
                [MortalType.ArmoredAxeman] = new MortalProfile(MortalType.ArmoredAxeman, MortalRole.Guard, Guard(2.8f, 0.2f), 14f),
                [MortalType.Knight] = new MortalProfile(MortalType.Knight, MortalRole.Guard, Guard(3f, 0.12f), 14f),
                [MortalType.Lancer] = new MortalProfile(MortalType.Lancer, MortalRole.Guard, Guard(3f, 0.2f), 16f),
                [MortalType.Priest] = new MortalProfile(MortalType.Priest, MortalRole.Priest, priest, 8f),
            };
        }
    }
}
