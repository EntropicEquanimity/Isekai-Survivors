using System;
using System.Collections.Generic;

namespace Isekai
{
    /// <summary>
    /// The Trait table of the spec, as data: for each Trait its behaviour weights, its buff, and who can have it.
    /// Weights are on the shared <see cref="BehaviourAction"/>s (a flee threshold is a term on Health), so demons, mortals
    /// and wildlife all use the same rows through their own profiles.
    /// </summary>
    public static class TraitTable
    {
        private const int FleeGroup = 1;

        private static readonly Side[] s_anySide = { Side.Yours, Side.Mortals, Side.Neutral, Side.Hostile, Side.Rogue };
        private static readonly UnitKind[] s_fighters = { UnitKind.Demon, UnitKind.Mortal, UnitKind.Wildlife };
        private static readonly TraitDef[] s_all = Build();

        /// <summary>Every Trait, in <see cref="TraitId"/> order.</summary>
        public static IReadOnlyList<TraitDef> All => s_all;

        public static TraitDef Get(TraitId id) => s_all[(int)id];

        /// <summary>The Traits a unit of this side and kind can have.</summary>
        public static List<TraitDef> Eligible(Side side, UnitKind kind)
        {
            var list = new List<TraitDef>();
            foreach (TraitDef def in s_all)
                if (def.CanHave(side, kind)) list.Add(def);
            return list;
        }

        /// <summary>A copy of <paramref name="profile"/> weighted by every one of <paramref name="traits"/>, in order.</summary>
        public static BehaviourProfile Apply(BehaviourProfile profile, IReadOnlyList<TraitId> traits)
        {
            for (int i = 0; i < traits.Count; i++) profile = Get(traits[i]).Apply(profile);
            return profile;
        }

        private static BehaviourTerm Health(float weight, float from, float to) => new(BehaviourFactor.Health, weight, from, to);

        private static TraitDef[] Build()
        {
            var defs = new TraitDef[Enum.GetValues(typeof(TraitId)).Length];
            void Add(TraitDef d) => defs[(int)d.Id] = d;

            // Flee thresholds are steep terms on Health: the weight beats any fight or patrol score the moment health is past them.
            Add(new TraitDef(TraitId.Cowardly, "Flees below 75% health. +30% max health.", s_anySide, s_fighters,
                new[] { new TraitWeight(BehaviourAction.Flee, TraitWeightMode.AddOrGrant, 0f, Health(8f, 0.75f, 0.74f)) },
                new[] { new TraitBuff(TraitStat.HealthMax, BuffKind.PercentAdd, 0.30f) }, group: FleeGroup));

            Add(new TraitDef(TraitId.Berserk, "Never flees. +20% damage.", s_anySide, s_fighters,
                new[] { new TraitWeight(BehaviourAction.Flee, TraitWeightMode.Off) },
                new[] { new TraitBuff(TraitStat.DamageBonus, BuffKind.Flat, 0.20f) }, group: FleeGroup));

            Add(new TraitDef(TraitId.Brave, "Does not flee until 10% health. +10% armour.", s_anySide, s_fighters,
                new[] { new TraitWeight(BehaviourAction.Flee, TraitWeightMode.Replace, 0f, Health(8f, 0.10f, 0.09f)) },
                new[] { new TraitBuff(TraitStat.Armor, BuffKind.Flat, 10f) }, group: FleeGroup));   // Armor 10 takes about 9% off a physical hit

            Add(new TraitDef(TraitId.Sneaky, "Invisible until it attacks. +25% damage on its first hit.", s_anySide, s_fighters,
                Array.Empty<TraitWeight>(),
                new[] { new TraitBuff(TraitStat.DamageBonus, BuffKind.Flat, 0.25f, BuffWhen.WhileConcealed) },
                TraitEffect.Concealed));

            Add(new TraitDef(TraitId.Ambusher, "Backs off after 3 kills. +30% move speed while hidden.", s_anySide, s_fighters,
                Array.Empty<TraitWeight>(),
                new[] { new TraitBuff(TraitStat.Speed, BuffKind.PercentAdd, 0.30f, BuffWhen.WhileUnseen) },
                TraitEffect.BacksOff));

            Add(new TraitDef(TraitId.Unruly, "Ignores orders, wanders off to cause trouble. +15% attack speed.", new[] { Side.Yours }, new[] { UnitKind.Demon },
                new[] { new TraitWeight(BehaviourAction.Fight, TraitWeightMode.Add, 0f, new BehaviourTerm(BehaviourFactor.Threat, 0.5f)) },
                new[] { new TraitBuff(TraitStat.AttackSpeed, BuffKind.PercentAdd, 0.15f) },
                TraitEffect.IgnoresOrders));

            Add(new TraitDef(TraitId.Vigilant, "Raises alarms faster. +50% vision.", new[] { Side.Mortals }, new[] { UnitKind.Mortal },
                Array.Empty<TraitWeight>(),
                new[] { new TraitBuff(TraitStat.Vision, BuffKind.PercentAdd, 0.50f) },
                TraitEffect.QuickAlarm));

            Add(new TraitDef(TraitId.Protector, "Stays near its settlement or post. +20% health near it.", new[] { Side.Yours, Side.Mortals }, new[] { UnitKind.Demon, UnitKind.Mortal },
                new[] { new TraitWeight(BehaviourAction.ReturnHome, TraitWeightMode.Add, 0f, new BehaviourTerm(BehaviourFactor.Far, 2f, 0.3f, 0.6f)) },
                new[] { new TraitBuff(TraitStat.HealthMax, BuffKind.PercentAdd, 0.20f, BuffWhen.NearPost) }));

            // A Purifier removes corruption the way a priest does (the shared Purify action) and mends allies near it. No demons.
            Add(new TraitDef(TraitId.Purifier, "Removes corruption, heals nearby allies slowly.", new[] { Side.Mortals }, new[] { UnitKind.Mortal },
                new[] { new TraitWeight(BehaviourAction.Purify, TraitWeightMode.GrantIfMissing, 0f, new BehaviourTerm(BehaviourFactor.Corruption, 2f)) },
                Array.Empty<TraitBuff>(),
                TraitEffect.HealsAllies));

            return defs;
        }
    }

    /// <summary>
    /// Rolling a unit's Traits: the odds by rank (ordinary 10% for one Trait, elite 25%, a Hero always 2 or 3), among the
    /// Traits its side and kind may have, no repeats and no two that contradict (<see cref="TraitDef.Group"/>).
    /// </summary>
    public static class TraitRoller
    {
        private static readonly TraitId[] s_none = Array.Empty<TraitId>();

        /// <summary>The chance a unit of this rank has any Trait (a Hero always does).</summary>
        public static float Odds(TraitRank rank) => rank switch
        {
            TraitRank.Hero => 1f,
            TraitRank.Elite => TraitTuning.EliteOdds,
            _ => TraitTuning.OrdinaryOdds,
        };

        /// <summary>The Traits rolled for a unit; empty for most. The same random stream rolls the same Traits.</summary>
        public static IReadOnlyList<TraitId> Roll(Random random, TraitRank rank, Side side, UnitKind kind)
        {
            int count = 1;
            if (rank == TraitRank.Hero) count = TraitTuning.HeroMin + random.Next(TraitTuning.HeroMax - TraitTuning.HeroMin + 1);
            else if (random.NextDouble() >= Odds(rank)) return s_none;

            List<TraitDef> pool = TraitTable.Eligible(side, kind);
            var rolled = new List<TraitId>(count);
            while (rolled.Count < count && pool.Count > 0)
            {
                TraitDef pick = pool[random.Next(pool.Count)];
                rolled.Add(pick.Id);
                pool.RemoveAll(d => d.Id == pick.Id || (pick.Group != 0 && d.Group == pick.Group));
            }
            return rolled;
        }
    }
}
