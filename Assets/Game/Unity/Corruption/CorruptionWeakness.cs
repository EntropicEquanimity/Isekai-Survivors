using System.Collections.Generic;
using Blondie.Combat;
using Blondie.Core;
using Blondie.Stats;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// Demons are about 20% weaker on ground below corruption level 2: a Damage Bonus modifier (Blondie Stats) put on
    /// while one stands there and taken off when it steps onto level 2 or more. It covers your demons and the
    /// Character (Jack's body is a demon too). Cells are checked a few times a second and only a change touches stats,
    /// so a steady army costs no allocations.
    /// </summary>
    [RegionSystem]
    public sealed class CorruptionWeakness : MonoBehaviour
    {
        private const float CheckSeconds = 0.2f;
        private static readonly string Stat = nameof(StatId.DamageBonus);

        private readonly HashSet<RegionEntity> _weak = new();
        private float _next;

        /// <summary>The Damage Bonus a weak demon carries: its damage is multiplied by 1 + this.</summary>
        public static float Penalty => CorruptionRules.DamageMultiplier(0) - 1f;

        public static CorruptionWeakness Current { get; private set; }

        /// <summary>Whether <paramref name="unit"/> carries the weakness now.</summary>
        public bool IsWeak(RegionEntity unit) => _weak.Contains(unit);

        private void OnEnable()
        {
            Current = this;
            RegionEntities.Died += OnDied;
        }

        private void OnDisable()
        {
            if (Current == this) Current = null;
            RegionEntities.Died -= OnDied;
        }

        private void Update()
        {
            RegionCorruption corruption = RegionCorruption.Current;
            if (corruption == null || Time.time < _next) return;
            _next = Time.time + CheckSeconds;
            IReadOnlyList<RegionEntity> living = RegionEntities.Living;
            for (int i = 0; i < living.Count; i++)
            {
                RegionEntity unit = living[i];
                if (unit.Kind != UnitKind.Demon && unit.Kind != UnitKind.Character) continue;
                if (unit.Side != Side.Yours) continue;
                bool weak = CorruptionRules.IsWeak(corruption.Level(unit.Cell));
                if (weak == _weak.Contains(unit)) continue;
                if (weak) Weaken(unit);
                else Restore(unit);
            }
        }

        private void Weaken(RegionEntity unit)
        {
            _weak.Add(unit);
            IStatModifierTarget stats = unit.Damageable.Stats;
            if (stats == null) return;
            // Units without a Damage Bonus of their own (the Imp) get one at 0 to carry the penalty.
            if (!stats.HasStat(Stat) && stats is StatBlock block) block.AddStat(Stat, 0f);
            stats.AddStatModifier(Stat, StatModifierKind.Flat, Penalty, this);
        }

        private void Restore(RegionEntity unit)
        {
            _weak.Remove(unit);
            unit.Damageable.Stats?.RemoveStatModifiers(this);
        }

        // A dead demon keeps no weakness: it comes back (or not) clean and is checked again.
        private void OnDied(UnitDeath death)
        {
            if (_weak.Contains(death.Unit)) Restore(death.Unit);
        }
    }
}
