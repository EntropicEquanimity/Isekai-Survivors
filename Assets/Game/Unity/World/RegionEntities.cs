using System;
using System.Collections.Generic;
using Blondie.Combat;
using Blondie.Core;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// One living thing the Region keeps track of (a horde member, a Unit, the Character, a Building). The owning
    /// component creates it once, calls <see cref="Enter"/> when it enables and <see cref="Leave"/> when it disables;
    /// it lists itself while alive and raises <see cref="RegionEntities.Died"/> when it dies. Side and cell are read when
    /// asked, so a moving unit costs nothing per frame.
    /// </summary>
    public sealed class RegionEntity
    {
        private bool _entered;

        public RegionEntity(Damageable damageable, UnitKind kind)
        {
            Damageable = damageable;
            Kind = kind;
        }

        public Damageable Damageable { get; }
        public UnitKind Kind { get; set; }
        public GameObject GameObject => Damageable.gameObject;

        /// <summary>The side it fights for now (from its Blondie Combat team, so a revolt changes it).</summary>
        public Side Side => TeamIds.SideOf(Damageable.Unit.Team);

        /// <summary>The ground cell it stands in now.</summary>
        public Int2 Cell => RegionGrid.Current != null ? RegionGrid.Current.CellAt(Damageable.transform.position) : default;

        /// <summary>Listed in <see cref="RegionEntities.Living"/>.</summary>
        public bool IsListed => Index >= 0;

        internal int Index { get; set; } = -1;

        /// <summary>The component enabled: list the unit and watch it die and come back.</summary>
        public void Enter()
        {
            if (_entered) return;
            _entered = true;
            Damageable.Died += OnDied;
            Damageable.Revived += OnRevived;
            Damageable.Damaged += OnDamaged;
            if (!Damageable.IsDead) RegionEntities.Add(this);
        }

        /// <summary>The component disabled (or despawned): unlist the unit and stop watching.</summary>
        public void Leave()
        {
            if (!_entered) return;
            _entered = false;
            if (Damageable != null)
            {
                Damageable.Died -= OnDied;
                Damageable.Revived -= OnRevived;
                Damageable.Damaged -= OnDamaged;
            }
            RegionEntities.Remove(this);
        }

        private void OnDied(DamageResult result)
        {
            if (!IsListed) return;
            Side? killer = result.Info?.Source != null ? TeamIds.SideOf(result.Info.Source.Team) : null;
            var death = new UnitDeath(this, Side, Kind, killer, Cell);
            RegionEntities.Remove(this);
            RegionEntities.Raise(death);
        }

        private void OnDamaged(DamageResult result) => RegionEntities.RaiseDamaged(this, result);

        private void OnRevived()
        {
            if (!IsListed) RegionEntities.Add(this);
        }
    }

    /// <summary>One death: who died, what side and kind it was, whose side killed it (null when unknown) and where.</summary>
    public readonly struct UnitDeath
    {
        public UnitDeath(RegionEntity unit, Side side, UnitKind kind, Side? killerSide, Int2 cell)
        {
            Unit = unit;
            Side = side;
            Kind = kind;
            KillerSide = killerSide;
            Cell = cell;
        }

        public RegionEntity Unit { get; }
        public Side Side { get; }
        public UnitKind Kind { get; }
        public Side? KillerSide { get; }
        public Int2 Cell { get; }
    }

    /// <summary>
    /// The Region-wide list of living units and the one place deaths are announced, so Souls, Vigilance, bones and the
    /// map read one source instead of searching the scene. Cleared when a Region starts.
    /// </summary>
    public static class RegionEntities
    {
        private static readonly List<RegionEntity> s_living = new();

        static RegionEntities() => StaticReset.Register(Reset);

        /// <summary>Every living registered unit (order changes as units come and go).</summary>
        public static IReadOnlyList<RegionEntity> Living => s_living;

        /// <summary>A registered unit died; it is no longer in <see cref="Living"/>. Cleared at each Region start, so subscribers subscribe after the bootstrap's Awake (OnEnable or Start).</summary>
        public static event Action<UnitDeath> Died;
        /// <summary>A unit was listed in <see cref="Living"/> (new spawn, revive, or pooled respawn).</summary>
        public static event Action<RegionEntity> Entered;
        /// <summary>A listed unit took a hit (every hit, so handlers must be cheap). Cleared at each Region start.</summary>
        public static event Action<RegionEntity, DamageResult> Damaged;

        /// <summary>How many living units a side has.</summary>
        public static int Count(Side side)
        {
            int n = 0;
            foreach (RegionEntity u in s_living)
                if (u.Side == side) n++;
            return n;
        }

        /// <summary>A new Region is starting.</summary>
        public static void Reset()
        {
            foreach (RegionEntity u in s_living) u.Index = -1;
            s_living.Clear();
            Died = null;
            Entered = null;
            Damaged = null;
        }

        internal static void RaiseDamaged(RegionEntity unit, DamageResult result) => Damaged?.Invoke(unit, result);

        internal static void Add(RegionEntity unit)
        {
            if (unit.IsListed) return;
            unit.Index = s_living.Count;
            s_living.Add(unit);
            try { Entered?.Invoke(unit); }
            catch (Exception e) { UnityEngine.Debug.LogException(e); }
        }

        internal static void Remove(RegionEntity unit)
        {
            int i = unit.Index;
            if (i < 0) return;
            unit.Index = -1;
            int last = s_living.Count - 1;
            if (i != last)
            {
                s_living[i] = s_living[last];
                s_living[i].Index = i;
            }
            s_living.RemoveAt(last);
        }

        internal static void Raise(UnitDeath death)
        {
            try { Died?.Invoke(death); }
            catch (Exception e) { UnityEngine.Debug.LogException(e); }
        }
    }
}
