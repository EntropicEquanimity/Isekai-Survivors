using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Blondie.Combat;
using Blondie.Pooling;
using UnityEngine;

namespace Isekai
{
    /// <summary>Debug rows for spawning and editing units.</summary>
    [RegionSystem]
    public sealed class UnitsDebugSection : MonoBehaviour
    {
        private static readonly SpawnChoice[] Choices = BuildChoices();
        private static readonly FieldInfo s_unitPrefabField = typeof(UnitProducer).GetField("_unitPrefab", BindingFlags.Instance | BindingFlags.NonPublic);

        private const int MaxSpawnLevel = 10;

        // Trait choices for a spawn: leave the roll to chance, none at all, or one chosen Trait.
        private static readonly string[] TraitLabels = BuildTraitLabels();

        private int _choiceIndex;
        private int _sideIndex;
        private int _traitIndex;   // 0 random, 1 none, 2.. = TraitId
        private int _level = LoyaltyTuning.StartingLevel;

        /// <summary>The section in this Region (tests drive spawns through it).</summary>
        public static UnitsDebugSection Current { get; private set; }

        private void OnEnable()
        {
            Current = this;
            DebugActions.Button(this, "Units", "Unit type prev", () => StepChoice(-1), "pick");
            DebugActions.Button(this, "Units", "Unit type next", () => StepChoice(+1), "pick");
            DebugActions.Button(this, "Units", "Spawn side prev", () => StepSide(-1), "side");
            DebugActions.Button(this, "Units", "Spawn side next", () => StepSide(+1), "side");
            DebugActions.Button(this, "Units", "Spawn trait prev", () => StepTrait(-1), "trait");
            DebugActions.Button(this, "Units", "Spawn trait next", () => StepTrait(+1), "trait");
            DebugActions.Button(this, "Units", "Spawn level -", () => _level = Mathf.Max(LoyaltyTuning.StartingLevel, _level - 1), "level");
            DebugActions.Button(this, "Units", "Spawn level +", () => _level = Mathf.Min(MaxSpawnLevel, _level + 1), "level");
            DebugActions.Button(this, "Units", "Spawn unit at mouse", SpawnAtMouse);
            DebugActions.Button(this, "Units", "Select hovered unit", DebugUnitSelection.SelectHovered);
            DebugActions.Button(this, "Units", "Clear unit selection", DebugUnitSelection.ClearSelection);
            DebugActions.Button(this, "Units", "Kill selection", KillSelection);
            DebugActions.Button(this, "Units", "Heal selection", HealSelection);
            DebugActions.Button(this, "Units", "Selected demon -> Rogue", MakeSelectedRogue);
            DebugActions.Info(this, "Units", Readout);
        }

        private void OnDisable()
        {
            if (Current == this) Current = null;
            DebugActions.Remove(this);
        }

        private static string[] BuildTraitLabels()
        {
            var labels = new List<string> { "random traits", "no traits" };
            foreach (TraitId id in Enum.GetValues(typeof(TraitId))) labels.Add(id.ToString());
            return labels.ToArray();
        }

        private void StepTrait(int delta)
        {
            _traitIndex = (_traitIndex + delta) % TraitLabels.Length;
            if (_traitIndex < 0) _traitIndex += TraitLabels.Length;
        }

        /// <summary>Picks what the next spawn is: a unit type by label (e.g. "Imp", "Soldier"), a side, a Trait (null: random, "none": none) and a level.</summary>
        public bool Pick(string unitLabel, Side side, string trait, int level)
        {
            int unit = Array.FindIndex(Choices, c => c.Label == unitLabel);
            int traitIndex = trait == null ? 0 : Array.IndexOf(TraitLabels, trait == "none" ? "no traits" : trait);
            if (unit < 0 || traitIndex < 0) return false;
            _choiceIndex = unit;
            _sideIndex = Array.IndexOf(SideRelations.All, side);
            _traitIndex = traitIndex;
            _level = Mathf.Clamp(level, LoyaltyTuning.StartingLevel, MaxSpawnLevel);
            return true;
        }

        private void StepChoice(int delta)
        {
            if (Choices.Length == 0) return;
            _choiceIndex = (_choiceIndex + delta) % Choices.Length;
            if (_choiceIndex < 0) _choiceIndex += Choices.Length;
        }

        private void StepSide(int delta)
        {
            Side[] sides = SideRelations.All;
            _sideIndex = (_sideIndex + delta) % sides.Length;
            if (_sideIndex < 0) _sideIndex += sides.Length;
        }

        private Side PickedSide => SideRelations.All[Mathf.Clamp(_sideIndex, 0, SideRelations.All.Length - 1)];

        private SpawnChoice PickedChoice => Choices[Mathf.Clamp(_choiceIndex, 0, Choices.Length - 1)];

        private void SpawnAtMouse()
        {
            if (Choices.Length == 0) return;
            Vector3 at = DebugUnitSelection.MouseWorld;
            RegionGrid grid = RegionGrid.Current;
            if (grid != null && grid.Contains(grid.CellAt(at))) at = grid.CentreOf(grid.CellAt(at));

            GameObject go = SpawnPicked(at);
            if (go != null) DebugUnitSelection.SelectAt(go.transform.position, 2f);
        }

        /// <summary>Spawns the picked unit type for the picked side with the picked Traits and level (the level only where the unit has levels: demons of Portals).</summary>
        public GameObject SpawnPicked(Vector3 at)
        {
            if (Choices.Length == 0) return null;
            GameObject go = Spawn(PickedChoice, at);
            if (go == null) return null;
            ApplySide(go, PickedSide);
            if (_traitIndex == 1) TraitSystem.Current?.Give(go);
            else if (_traitIndex >= 2) TraitSystem.Current?.Give(go, (TraitId)(_traitIndex - 2));
            if (_level > LoyaltyTuning.StartingLevel) DemonLoyalty.Of(go)?.RaiseToLevel(_level);
            return go;
        }

        private static void KillSelection()
        {
            if (!DebugUnitSelection.TrySelected(out RegionEntity selected)) return;
            selected.Damageable.Kill();
        }

        private static void HealSelection()
        {
            if (!DebugUnitSelection.TrySelected(out RegionEntity selected) || selected.Damageable.IsDead) return;
            selected.Damageable.Heal(selected.Damageable.MaxHealth);
        }

        private static void MakeSelectedRogue()
        {
            if (!DebugUnitSelection.TrySelected(out RegionEntity selected)) return;
            if (selected.Kind is not (UnitKind.Demon or UnitKind.Character)) return;
            ApplySide(selected.GameObject, Side.Rogue);
        }

        private string Readout()
        {
            string selected = DebugUnitSelection.TrySelected(out RegionEntity unit)
                ? $"{unit.GameObject.name} ({unit.Side}, {unit.Kind})"
                : "none";
            return $"Spawn {PickedChoice.Label} as {PickedSide}, {TraitLabels[_traitIndex]}, level {_level} · selected {selected}";
        }

        private static SpawnChoice[] BuildChoices()
        {
            var picks = new System.Collections.Generic.List<SpawnChoice>();
            foreach (MortalType type in Enum.GetValues(typeof(MortalType)))
                picks.Add(SpawnChoice.FromMortal(type));
            foreach (WildlifeType type in Enum.GetValues(typeof(WildlifeType)))
                picks.Add(SpawnChoice.FromWildlife(type));
            picks.Add(SpawnChoice.Special("Imp", SpawnKind.Imp));
            picks.Add(SpawnChoice.Special("Worker", SpawnKind.Worker));
            picks.Add(SpawnChoice.Special("Demon King", SpawnKind.DemonKing));
            return picks.ToArray();
        }

        private static GameObject Spawn(SpawnChoice choice, Vector3 at)
        {
            return choice.Kind switch
            {
                SpawnKind.Mortal => SpawnMortal(choice.Mortal, at),
                SpawnKind.Wildlife => SpawnWildlife(choice.Wildlife, at),
                SpawnKind.Imp => SpawnPrefab(ImpPrefab(), at),
                SpawnKind.Worker => SpawnPrefab(Resources.Load<GameObject>("Worker"), at),
                SpawnKind.DemonKing => SpawnPrefab(Resources.Load<GameObject>(RegionPossession.KingPrefab), at),
                _ => null
            };
        }

        private static GameObject SpawnMortal(MortalType type, Vector3 at)
        {
            RegionMortals mortals = RegionMortals.Current;
            if (mortals != null && RegionSettlements.Current != null && RegionSettlements.Current.Settlements.Count > 0)
            {
                RegionSettlements.Settlement home = NearestSettlement(at);
                MortalBrain brain = mortals.Spawn(type, home, at);
                return brain != null ? brain.gameObject : null;
            }

            GameObject prefab = Resources.Load<MortalRoster>(MortalRoster.ResourceName)?.PrefabOf(type);
            return SpawnPrefab(prefab, at);
        }

        private static GameObject SpawnWildlife(WildlifeType type, Vector3 at)
        {
            RegionWildlife wildlife = RegionWildlife.Current;
            if (wildlife != null)
            {
                WildlifeBrain brain = wildlife.Spawn(type, at);
                if (brain != null) return brain.gameObject;
            }

            GameObject prefab = Resources.Load<WildlifeRoster>(WildlifeRoster.ResourceName)?.PrefabOf(type);
            return SpawnPrefab(prefab, at);
        }

        private static RegionSettlements.Settlement NearestSettlement(Vector3 at)
        {
            RegionSettlements current = RegionSettlements.Current;
            RegionSettlements.Settlement best = current.Seat;
            float bestDistance = float.MaxValue;
            foreach (RegionSettlements.Settlement settlement in current.Settlements)
            {
                if (settlement.Markers.Count == 0) continue;
                Vector3 target = settlement.Markers[0].Position;
                float sqr = (target - at).sqrMagnitude;
                if (sqr >= bestDistance) continue;
                bestDistance = sqr;
                best = settlement;
            }
            return best ?? current.Seat;
        }

        private static GameObject SpawnPrefab(GameObject prefab, Vector3 at)
        {
            if (prefab == null) return null;
            return Pools.Spawn(prefab, at, Quaternion.identity);
        }

        private static GameObject ImpPrefab()
        {
            BuildMode mode = UnityEngine.Object.FindAnyObjectByType<BuildMode>();
            if (mode == null) return null;
            BuildingAsset portal = mode.Catalogue.FirstOrDefault(a => a != null && a.DisplayName == "Portal");
            if (portal == null || portal.Prefab == null || s_unitPrefabField == null) return null;
            UnitProducer producer = portal.Prefab.GetComponent<UnitProducer>();
            return producer != null ? s_unitPrefabField.GetValue(producer) as GameObject : null;
        }

        private static void ApplySide(GameObject go, Side side)
        {
            if (go == null) return;
            Team team = go.GetComponent<Team>();
            if (team == null) team = go.AddComponent<Team>();
            team.Id = TeamIds.Of(side);
            RegionEntity entity = FindEntity(go);
            if (entity != null) SideOutlineApplier.Reapply(entity);
        }

        private static RegionEntity FindEntity(GameObject go)
        {
            IReadOnlyList<RegionEntity> living = RegionEntities.Living;
            for (int i = 0; i < living.Count; i++)
            {
                RegionEntity entity = living[i];
                if (entity.GameObject == go) return entity;
            }
            return null;
        }

        private enum SpawnKind { Mortal, Wildlife, Imp, Worker, DemonKing }

        private readonly struct SpawnChoice
        {
            private SpawnChoice(string label, SpawnKind kind, MortalType mortal, WildlifeType wildlife)
            {
                Label = label;
                Kind = kind;
                Mortal = mortal;
                Wildlife = wildlife;
            }

            public string Label { get; }
            public SpawnKind Kind { get; }
            public MortalType Mortal { get; }
            public WildlifeType Wildlife { get; }

            public static SpawnChoice FromMortal(MortalType type) => new(type.ToString(), SpawnKind.Mortal, type, default);

            public static SpawnChoice FromWildlife(WildlifeType type) => new(type.ToString(), SpawnKind.Wildlife, default, type);

            public static SpawnChoice Special(string label, SpawnKind kind) => new(label, kind, default, default);
        }
    }
}
