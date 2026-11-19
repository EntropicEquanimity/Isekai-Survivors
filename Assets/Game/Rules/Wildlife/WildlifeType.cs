using System;
using System.Collections.Generic;

namespace Isekai
{
    /// <summary>The wildlife of a Region (spec: "Humans only, plus wildlife"): creatures that belong to no settlement.</summary>
    public enum WildlifeType
    {
        Slime,
        Werewolf,
        Werebear,
    }

    /// <summary>
    /// A kind of wildlife as data: which side it is on, how it decides (<see cref="Behaviour"/>, the same weighted actions
    /// the mortals use), how far it strays from where it spawned and how fast it moves. Health and damage are on the
    /// type's prefab, where every horde unit keeps them.
    /// </summary>
    public sealed class WildlifeProfile
    {
        public WildlifeProfile(WildlifeType type, Side side, BehaviourProfile behaviour, float leashCells, float chaseSpeed)
        {
            Type = type;
            Side = side;
            Behaviour = behaviour;
            LeashCells = leashCells;
            ChaseSpeed = chaseSpeed;
        }

        public WildlifeType Type { get; }

        /// <summary><see cref="Side.Neutral"/> attacks no one unless attacked; <see cref="Side.Hostile"/> attacks everything on sight.</summary>
        public Side Side { get; }

        public BehaviourProfile Behaviour { get; }

        /// <summary>How far from its spot it strays before it feels it should be back, in cells.</summary>
        public float LeashCells { get; }

        /// <summary>Cells per second when going for a target or running from one.</summary>
        public float ChaseSpeed { get; }

        /// <summary>It attacks only what attacked it.</summary>
        public bool OnlyFightsBack => Side == Side.Neutral;
    }

    /// <summary>Every number of the wildlife's behaviour in one place, to tune.</summary>
    public static class WildlifeTuning
    {
        /// <summary>A creature thinks (re-scores its actions) this often, staggered across frames.</summary>
        public const float ThinkSeconds = 0.3f;

        /// <summary>Fear after a hit fades over this long.</summary>
        public const float FearSeconds = 6f;

        /// <summary>A neutral creature keeps its attacker in mind this long after the last hit it took.</summary>
        public const float GrudgeSeconds = 8f;

        /// <summary>Speed of a creature strolling about its spot, and the share of its chase speed it runs away at.</summary>
        public const float WanderSpeed = 0.8f;
        public const float FleeShare = 1.2f;

        /// <summary>It wanders to spots within this many cells of where it spawned.</summary>
        public const int WanderRadius = 5;

        /// <summary>Seconds a creature stands at a spot before the next.</summary>
        public const float PauseMin = 1.5f;
        public const float PauseMax = 5f;

        /// <summary>Within this of a spot (cells) counts as there.</summary>
        public const float ArriveDistance = 0.3f;

        /// <summary>How long a stuck creature tries before choosing somewhere else, in seconds.</summary>
        public const float StuckSeconds = 0.6f;
    }

    /// <summary>
    /// The wildlife's profiles. Slimes are Neutral: harmless until hit, then they fight back, and flee when badly hurt.
    /// Werewolves are Hostile and flee when hurt; werebears are Hostile and fight to the death. Weights are tuned so
    /// a number of about 1 is a mild urge and 3 a strong one.
    /// </summary>
    public static class WildlifeProfiles
    {
        private static readonly Dictionary<WildlifeType, WildlifeProfile> s_all = Build();

        public static WildlifeProfile For(WildlifeType type) => s_all[type];

        /// <summary>Every type, in roster order.</summary>
        public static IEnumerable<WildlifeType> Roster => (WildlifeType[])Enum.GetValues(typeof(WildlifeType));

        private static BehaviourTerm T(BehaviourFactor factor, float weight, float from = 0f, float to = 1f) => new(factor, weight, from, to);

        private static Dictionary<WildlifeType, WildlifeProfile> Build()
        {
            const BehaviourAction Idle = BehaviourAction.Idle, Work = BehaviourAction.Work, Fight = BehaviourAction.Fight,
                Flee = BehaviourAction.Flee, Return = BehaviourAction.ReturnHome;
            const BehaviourFactor Health = BehaviourFactor.Health, Threat = BehaviourFactor.Threat, Fear = BehaviourFactor.Fear, Far = BehaviourFactor.Far;

            // Strolls about (Work), goes for a threat, and turns back once far from its spot. A creature that flees does so
            // only when hurt AND frightened: the base cancels the health term while it is not afraid.
            BehaviourProfile Creature(string name, float fightWeight, float fleeBelow)
            {
                BehaviourProfile p = new BehaviourProfile(name)
                    .Allow(Idle, 0.2f)
                    .Allow(Work, 0.6f)
                    .Allow(Fight, 0f, T(Threat, fightWeight))
                    .Allow(Return, 0f, T(Far, 3.5f, 1.5f, 2.5f));
                return fleeBelow > 0f ? p.Allow(Flee, -3f, T(Fear, 3f), T(Health, 3.5f, fleeBelow, fleeBelow * 0.5f)) : p;
            }

            return new Dictionary<WildlifeType, WildlifeProfile>
            {
                [WildlifeType.Slime] = new WildlifeProfile(WildlifeType.Slime, Side.Neutral, Creature("Slime", 2.5f, 0.35f), 8f, 1.2f),
                [WildlifeType.Werewolf] = new WildlifeProfile(WildlifeType.Werewolf, Side.Hostile, Creature("Werewolf", 3f, 0.3f), 14f, 3f),
                [WildlifeType.Werebear] = new WildlifeProfile(WildlifeType.Werebear, Side.Hostile, Creature("Werebear", 3f, 0f), 12f, 1.8f),
            };
        }
    }

    /// <summary>
    /// What a wildlife spot produces: a weighted kind, then a group of it (slimes and wolves come in twos and threes, a
    /// bear alone). Slimes are the commonest and the safest to meet; bears are rare.
    /// </summary>
    public static class WildlifeSpawns
    {
        public readonly struct Choice
        {
            public Choice(WildlifeType type, int weight, int groupMin, int groupMax)
            {
                Type = type;
                Weight = weight;
                GroupMin = groupMin;
                GroupMax = groupMax;
            }

            public WildlifeType Type { get; }
            public int Weight { get; }
            public int GroupMin { get; }
            public int GroupMax { get; }
        }

        private static readonly Choice[] s_choices =
        {
            new(WildlifeType.Slime, 4, 2, 3),
            new(WildlifeType.Werewolf, 3, 2, 3),
            new(WildlifeType.Werebear, 2, 1, 1),
        };

        public static IReadOnlyList<Choice> Choices => s_choices;

        /// <summary>The kind for a spot, picked by <paramref name="roll"/> in [0, 1) over the weights.</summary>
        public static Choice Pick(double roll)
        {
            int total = 0;
            foreach (Choice c in s_choices) total += c.Weight;
            double at = Math.Min(Math.Max(roll, 0d), 0.999999d) * total;
            foreach (Choice c in s_choices)
            {
                at -= c.Weight;
                if (at < 0d) return c;
            }
            return s_choices[s_choices.Length - 1];
        }

        /// <summary>How many of a kind appear at one spot, picked by <paramref name="roll"/> in [0, 1).</summary>
        public static int GroupSize(Choice choice, double roll)
        {
            int span = choice.GroupMax - choice.GroupMin + 1;
            int n = (int)(Math.Min(Math.Max(roll, 0d), 0.999999d) * span);
            return choice.GroupMin + n;
        }
    }
}
