using System;
using System.Collections.Generic;
using Blondie.Core;
using Blondie.Stats;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// What the Run's Anchor does in the Region. At the start the War Throne puts Vigilance at Uneasy. A few times a
    /// second it checks whether a hidden Shrine has been seen by the mortals (<see cref="RegionVision"/>): being found
    /// raises Vigilance a full stage, once, and makes it a horde target (<see cref="Anchor.Reveal"/>). While a War
    /// Throne stands, your demons within its aura carry a Damage Bonus (Blondie Stats), put on and taken off as they
    /// move in and out. After the Anchor is lost, <see cref="TryRaise"/> raises a new one of the Run's kind: one at a
    /// time, on corrupted ground, at the asset's cost. Its corruption source is <see cref="AnchorTuning.Source"/>.
    /// </summary>
    [RegionSystem]
    public sealed class RegionAnchor : MonoBehaviour, IRegionPopulate
    {
        private const int SearchRadius = 6;
        private static readonly string Stat = nameof(StatId.DamageBonus);

        private readonly HashSet<RegionEntity> _buffed = new();
        private float _next;
        private bool _started;

        public static RegionAnchor Current { get; private set; }

        int IRegionPopulate.PopulateOrder => RegionOrder.Patrols + 50;

        /// <summary>How many times a hidden Shrine has been found in this Region.</summary>
        public int TimesFound { get; private set; }

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

        void IRegionPopulate.Populate(RegionContext region)
        {
            if (_started) return;
            _started = true;
            VigilanceStage stage = VigilanceStage.Unaware;
            foreach (Anchor anchor in Anchor.All)
                if (anchor.Building.IsPlayers && AnchorTuning.StartStage(anchor.Kind) > stage) stage = AnchorTuning.StartStage(anchor.Kind);
            if (stage > VigilanceStage.Unaware && VigilanceDirector.Current != null) VigilanceDirector.Current.StartAt(stage);
        }

        /// <summary>Whether <paramref name="unit"/> carries the War Throne's aura now.</summary>
        public bool IsBuffed(RegionEntity unit) => _buffed.Contains(unit);

        /// <summary>The Anchor that stands (finished or still going up), or null when none does.</summary>
        public static Building Standing()
        {
            foreach (Building b in Building.All)
                if (b != null && b.IsMainBase && b.IsPlayers && b.IsAlive) return b;
            return null;
        }

        /// <summary>A Shrine was seen: Vigilance rises a full stage (once) and the mortals have a new target. False if it was not hidden.</summary>
        public bool Find(Anchor anchor)
        {
            if (anchor == null || !anchor.Reveal()) return false;
            TimesFound++;
            if (VigilanceDirector.Current != null) VigilanceDirector.Current.RaiseStage();
            return true;
        }

        private void Update()
        {
            if (Time.time < _next) return;
            _next = Time.time + AnchorTuning.CheckSeconds;
            WatchHidden();
            TickAura();
        }

        private void WatchHidden()
        {
            RegionVision vision = RegionVision.Current;
            RegionGrid grid = RegionGrid.Current;
            if (vision == null || !vision.IsReady || !RegionVision.IsOn || grid == null) return;
            IReadOnlyList<Anchor> all = Anchor.All;
            for (int i = all.Count - 1; i >= 0; i--)
            {
                Anchor anchor = all[i];
                if (!anchor.IsHidden || !anchor.Building.IsAlive || !SeenByMortals(vision, grid, anchor)) continue;
                Find(anchor);
            }
        }

        private static bool SeenByMortals(RegionVision vision, RegionGrid grid, Anchor anchor)
        {
            Building b = anchor.Building;
            Vector2Int size = b.Asset != null ? b.Asset.Size : Vector2Int.one;
            Int2 origin = grid.FootprintOrigin(b.transform.position, size);
            for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
                if (vision.CanSee(Side.Mortals, new Int2(origin.x + x, origin.y + y))) return true;
            return false;
        }

        private void TickAura()
        {
            IReadOnlyList<RegionEntity> living = RegionEntities.Living;
            bool anyAura = false;
            foreach (Anchor a in Anchor.All)
                if (HasAura(a)) { anyAura = true; break; }
            if (!anyAura && _buffed.Count == 0) return;
            for (int i = 0; i < living.Count; i++)
            {
                RegionEntity unit = living[i];
                if (unit.Kind != UnitKind.Demon && unit.Kind != UnitKind.Character) continue;
                if (unit.Side != Side.Yours) continue;
                bool inside = anyAura && InAura(unit.GameObject.transform.position);
                if (inside == _buffed.Contains(unit)) continue;
                if (inside) Buff(unit);
                else Unbuff(unit);
            }
        }

        private static bool HasAura(Anchor a) =>
            AnchorTuning.HasAura(a.Kind) && !a.IsHidden && a.Building.IsAlive && a.Building.IsBuilt && a.Building.IsPlayers;

        private static bool InAura(Vector3 at)
        {
            float r2 = AnchorTuning.AuraRadius * AnchorTuning.AuraRadius;
            foreach (Anchor a in Anchor.All)
                if (HasAura(a) && ((Vector2)(a.transform.position - at)).sqrMagnitude <= r2) return true;
            return false;
        }

        private void Buff(RegionEntity unit)
        {
            _buffed.Add(unit);
            IStatModifierTarget stats = unit.Damageable.Stats;
            if (stats == null) return;
            // Units without a Damage Bonus of their own (the Imp) get one at 0 to carry the aura.
            if (!stats.HasStat(Stat) && stats is StatBlock block) block.AddStat(Stat, 0f);
            stats.AddStatModifier(Stat, StatModifierKind.Flat, AnchorTuning.AuraDamageBonus, this);
        }

        private void Unbuff(RegionEntity unit)
        {
            _buffed.Remove(unit);
            unit.Damageable.Stats?.RemoveStatModifiers(this);
        }

        private void OnDied(UnitDeath death)
        {
            if (_buffed.Contains(death.Unit)) Unbuff(death.Unit);
        }

        /// <summary>
        /// Raises a new Anchor of the Run's kind beside Jack (or his wisp): only when none stands, on a free spot of
        /// corrupted ground, paid for in the asset's Souls and Stone. Null, with the reason, when it can't.
        /// </summary>
        public Building TryRaise(out string reason)
        {
            reason = null;
            if (Standing() != null) { reason = "An Anchor already stands"; return null; }
            RegionBuildings buildings = RegionBuildings.Current;
            BuildingAsset asset = AnchorCatalog.Asset(AnchorCatalog.Chosen);
            if (buildings == null || asset == null || RegionEconomy.Current == null) { reason = "No ground to raise it on"; return null; }
            Int2 centre = buildings.CellAt(Caller());
            string firstReason = null, blockedReason = null;
            for (int r = 0; r <= SearchRadius; r++)
            for (int dx = -r; dx <= r; dx++)
            for (int dy = -r; dy <= r; dy++)
            {
                if (r > 0 && Math.Abs(dx) != r && Math.Abs(dy) != r) continue;   // the ring only
                var origin = new Int2(centre.x + dx, centre.y + dy);
                Placement check = buildings.Check(asset, origin);
                if (!check.IsOk)
                {
                    // Why ground that is free is refused says more than "Blocked" for ground that is not.
                    if (check.Verdict == Verdict.Blocked) blockedReason ??= check.Reason;
                    else firstReason ??= check.Reason;
                    continue;
                }
                Building placed = buildings.Place(asset, origin);
                if (placed != null) return placed;
            }
            reason = firstReason ?? blockedReason ?? "No room";
            return null;
        }

        private static Vector3 Caller()
        {
            Wisp wisp = RegionSouls.Current != null ? RegionSouls.Current.Wisp : null;
            if (wisp != null) return wisp.transform.position;
            CharacterControl body = Player.Character;
            return body != null ? body.transform.position : Vector3.zero;
        }
    }
}
