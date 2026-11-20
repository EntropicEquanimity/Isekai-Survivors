using System.Collections.Generic;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// The Traits one unit has now: a small component the <see cref="TraitSystem"/> puts on a mortal, a wildlife creature or a
    /// demon that rolled any, and refreshes for each new life of a pooled one. Other systems read it (the ruler's Traits
    /// in #24, tooltips in #29, the brains' speed and sight): <see cref="Traits"/>, <see cref="Has"/>, <see cref="Of"/>.
    /// </summary>
    public sealed class UnitTraits : MonoBehaviour
    {
        private readonly List<TraitId> _traits = new();

        /// <summary>What the unit has, in the order rolled; empty for most units.</summary>
        public IReadOnlyList<TraitId> Traits => _traits;

        public bool Any => _traits.Count > 0;
        public TraitRank Rank { get; internal set; }

        public bool Has(TraitId id) => _traits.Contains(id);

        /// <summary>The Trait row of each (names and descriptions for a tooltip).</summary>
        public IEnumerable<TraitDef> Defs()
        {
            foreach (TraitId id in _traits) yield return TraitTable.Get(id);
        }

        /// <summary>A Sneaky unit that is unseen now (it has not attacked, or its 3 seconds of being seen are over).</summary>
        public bool IsConcealed { get; internal set; }

        /// <summary>The other side cannot see the unit now (concealed, or out of their sight).</summary>
        public bool IsUnseen { get; internal set; }

        /// <summary>Within a Protector's reach of its settlement or post.</summary>
        public bool IsNearPost { get; internal set; }

        /// <summary>When a Sneaky unit that attacked is unseen again (game seconds).</summary>
        public float VisibleUntil { get; internal set; }

        /// <summary>Kills an Ambusher has made since it last backed off, and when its backing off ends (0 when it is not).</summary>
        public int Kills { get; internal set; }
        public float BackOffUntil { get; internal set; }
        public bool IsBackingOff => BackOffUntil > 0f;

        /// <summary>How fast a brain without a motor walks, against its type (1 unchanged).</summary>
        public float SpeedFactor { get; internal set; } = 1f;

        /// <summary>How far it sees against its kind (1 unchanged, 1.5 Vigilant).</summary>
        public float SightFactor { get; internal set; } = 1f;

        /// <summary>The share of the usual delay before this witness sends its alarm (1 unchanged).</summary>
        public float AlarmDelay { get; internal set; } = 1f;

        internal RegionEntity Entity;
        internal BehaviourProfile Weighted;
        internal float BaseMaxHealth;
        internal float AppliedMaxHealth;
        internal float BaseInterval;
        internal float AppliedInterval;
        internal float NextHeal;

        internal void Set(IReadOnlyList<TraitId> traits, TraitRank rank)
        {
            _traits.Clear();
            for (int i = 0; i < traits.Count; i++) _traits.Add(traits[i]);
            Rank = rank;
        }

        internal bool Is(TraitEffect effect)
        {
            foreach (TraitId id in _traits)
                if (TraitTable.Get(id).Has(effect)) return true;
            return false;
        }

        /// <summary>The Traits of a unit, or null when it has none.</summary>
        public static UnitTraits Of(GameObject unit) => unit != null && unit.TryGetComponent(out UnitTraits t) && t.Any ? t : null;

        // What the brains ask: cheap, and 1 for a unit without Traits.
        internal static float SpeedOf(Component brain) => brain.TryGetComponent(out UnitTraits t) ? t.SpeedFactor : 1f;
        internal static float SightOf(Component brain) => brain.TryGetComponent(out UnitTraits t) ? t.SightFactor : 1f;
        internal static float AlarmDelayShare(Component brain) => brain.TryGetComponent(out UnitTraits t) ? t.AlarmDelay : 1f;
    }
}
