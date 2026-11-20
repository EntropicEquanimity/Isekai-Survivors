using System;

namespace Isekai
{
    /// <summary>How a Trait changes one action of a unit's <see cref="BehaviourProfile"/>.</summary>
    public enum TraitWeightMode
    {
        /// <summary>Adds terms to the action if the unit does it; nothing otherwise.</summary>
        Add,
        /// <summary>Adds terms to the action, and makes a unit that does not do it do it (a Cowardly demon can flee).</summary>
        AddOrGrant,
        /// <summary>Gives a unit that does not do the action these weights; changes nothing for one that does (a Purifier priest).</summary>
        GrantIfMissing,
        /// <summary>Replaces the action's weights altogether (Brave flees only at the very end).</summary>
        Replace,
        /// <summary>Switches the action off.</summary>
        Off,
    }

    /// <summary>One weight a Trait puts on a shared behaviour: data, applied with <see cref="TraitDef.Apply"/>.</summary>
    public readonly struct TraitWeight
    {
        public TraitWeight(BehaviourAction action, TraitWeightMode mode, float baseScore = 0f, params BehaviourTerm[] terms)
        {
            Action = action;
            Mode = mode;
            Base = baseScore;
            Terms = terms ?? Array.Empty<BehaviourTerm>();
        }

        public BehaviourAction Action { get; }
        public TraitWeightMode Mode { get; }
        public float Base { get; }
        public BehaviourTerm[] Terms { get; }

        internal BehaviourProfile ApplyTo(BehaviourProfile profile)
        {
            bool does = profile.Of(Action).Enabled;
            switch (Mode)
            {
                case TraitWeightMode.Off: return profile.With(BehaviourChange.Off(Action));
                case TraitWeightMode.Replace: return profile.Allow(Action, Base, Terms);
                case TraitWeightMode.GrantIfMissing: return does ? profile : profile.Allow(Action, Base, Terms);
                case TraitWeightMode.AddOrGrant when !does: return profile.Allow(Action, Base, Terms);
                default: return does ? profile.With(BehaviourChange.Add(Action, Terms), BehaviourChange.Bias(Action, Base)) : profile;
            }
        }
    }

    /// <summary>The stats a Trait's buff changes. <see cref="Vision"/> is not a Blondie stat: it scales how far the unit sees.</summary>
    public enum TraitStat
    {
        HealthMax,
        Armor,
        DamageBonus,
        AttackSpeed,
        Speed,
        Vision,
    }

    /// <summary>How a buff's number combines with the stat: added (a flat 10 armour, +0.2 damage bonus) or as a percent (0.3 is +30%).</summary>
    public enum BuffKind
    {
        Flat,
        PercentAdd,
    }

    /// <summary>When a buff holds.</summary>
    public enum BuffWhen
    {
        Always,
        /// <summary>While a Sneaky unit is still unseen (not yet attacked, or 3 seconds since its last attack have passed): the first hit.</summary>
        WhileConcealed,
        /// <summary>While the other side cannot see the unit (concealed, or out of their sight).</summary>
        WhileUnseen,
        /// <summary>Within <see cref="TraitTuning.ProtectorNearCells"/> of its settlement or post.</summary>
        NearPost,
    }

    /// <summary>One buff that comes with a Trait.</summary>
    public readonly struct TraitBuff
    {
        public TraitBuff(TraitStat stat, BuffKind kind, float value, BuffWhen when = BuffWhen.Always)
        {
            Stat = stat;
            Kind = kind;
            Value = value;
            When = when;
        }

        public TraitStat Stat { get; }
        public BuffKind Kind { get; }
        public float Value { get; }
        public BuffWhen When { get; }
    }

    /// <summary>What a Trait does that is not a weight or a stat: each is a switch the Region's Trait system reads.</summary>
    [Flags]
    public enum TraitEffect
    {
        None = 0,
        /// <summary>Invisible to the other side until it attacks, then visible for <see cref="TraitTuning.VisibleSeconds"/>.</summary>
        Concealed = 1,
        /// <summary>A demon that ignores its Portal's order and wanders toward trouble.</summary>
        IgnoresOrders = 2,
        /// <summary>Backs off (flees) for a while after <see cref="TraitTuning.BackOffKills"/> kills.</summary>
        BacksOff = 4,
        /// <summary>A witness raises its alarm after a shorter delay.</summary>
        QuickAlarm = 8,
        /// <summary>Heals allies nearby, slowly.</summary>
        HealsAllies = 16,
    }

    /// <summary>
    /// A Trait as data: who can have it, how it weights the shared behaviours, which stats it buffs and the few switches
    /// the Region reads. Traits are weights on shared behaviours, not separate AIs, so a new Trait is a row here.
    /// </summary>
    public sealed class TraitDef
    {
        private readonly int _sides;
        private readonly int _kinds;

        internal TraitDef(TraitId id, string description, Side[] sides, UnitKind[] kinds, TraitWeight[] weights, TraitBuff[] buffs,
            TraitEffect effects = TraitEffect.None, int group = 0)
        {
            Id = id;
            Name = id.ToString();
            Description = description;
            Weights = weights;
            Buffs = buffs;
            Effects = effects;
            Group = group;
            foreach (Side s in sides) _sides |= 1 << (int)s;
            foreach (UnitKind k in kinds) _kinds |= 1 << (int)k;
        }

        public TraitId Id { get; }
        public string Name { get; }

        /// <summary>The one line a tooltip shows.</summary>
        public string Description { get; }

        public TraitWeight[] Weights { get; }
        public TraitBuff[] Buffs { get; }
        public TraitEffect Effects { get; }

        /// <summary>Traits of the same non-zero group contradict each other (how a unit flees): a unit has at most one.</summary>
        public int Group { get; }

        public bool Has(TraitEffect effect) => (Effects & effect) != 0;

        /// <summary>Whether a unit of this side and kind can have the Trait.</summary>
        public bool CanHave(Side side, UnitKind kind) => (_sides & (1 << (int)side)) != 0 && (_kinds & (1 << (int)kind)) != 0;

        /// <summary>A copy of <paramref name="profile"/> with this Trait's weights; the original is not touched.</summary>
        public BehaviourProfile Apply(BehaviourProfile profile)
        {
            foreach (TraitWeight w in Weights) profile = w.ApplyTo(profile);
            return profile;
        }
    }
}
