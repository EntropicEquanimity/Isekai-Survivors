using System;
using System.Collections.Generic;
using Blondie.Combat;
using Blondie.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Isekai
{
    /// <summary>
    /// Builds the mortals' places on the generated Region: the Seat's fort, villages, farms and camps from the layout's
    /// <see cref="SettlementPlan"/>, in the spec's order (the Seat, the rest, then the roads, then the walls that wrap
    /// them). Each Building is a mortal <see cref="Building"/> (the Mortals' team, damageable, a <see cref="HordeTarget"/>
    /// so the Region lists it as a Building) that blocks its cells until razed. The spawn markers are published for the
    /// mortal units to come (<see cref="Settlements"/>).
    /// <para>The two rules that end things live in <see cref="SettlementLedger"/>: destroying the Seat's keep captures
    /// the Region (<see cref="RegionEvents.End"/>), and razing a village, farm or camp opens an Item draft.</para>
    /// <para>Lives on the scene's Terrain object, where its Building prefabs and the path tile are set.</para>
    /// </summary>
    public sealed class RegionSettlements : MonoBehaviour, IRegionPopulate
    {
        [Serializable]
        public struct BuildingEntry
        {
            public MortalBuilding Kind;
            public BuildingAsset Asset;
        }

        /// <summary>Where a mortal unit of some kind will start.</summary>
        public readonly struct SpawnPoint
        {
            public SpawnPoint(MarkerKind kind, Int2 cell, Vector3 position)
            {
                Kind = kind;
                Cell = cell;
                Position = position;
            }

            public MarkerKind Kind { get; }
            public Int2 Cell { get; }
            public Vector3 Position { get; }
        }

        /// <summary>One settlement in the Region: what the layout decided and the Buildings made from it.</summary>
        public sealed class Settlement
        {
            private readonly Building[] _buildings;

            internal Settlement(PlacedSettlement plan, SpawnPoint[] markers)
            {
                Plan = plan;
                _buildings = new Building[plan.Buildings.Count];
                Markers = markers;
            }

            internal void Set(int index, Building building) => _buildings[index] = building;

            public PlacedSettlement Plan { get; }
            public SettlementKind Kind => Plan.Kind;
            public bool IsSeat => Plan.IsSeat;

            /// <summary>The Buildings, in the same order as <see cref="PlacedSettlement.Buildings"/> (null where no prefab was set).</summary>
            public IReadOnlyList<Building> Buildings => _buildings;

            /// <summary>Where its peasants, guards and priests start (the units are spawned by the mortal units ticket).</summary>
            public IReadOnlyList<SpawnPoint> Markers { get; }

            /// <summary>Every Building that is not fence has been destroyed.</summary>
            public bool IsRazed { get; internal set; }

            /// <summary>The Seat's keep (null for any other settlement).</summary>
            public Building Keep
            {
                get
                {
                    if (!IsSeat) return null;
                    for (int i = 0; i < Buildings.Count; i++)
                        if (Plan.Buildings[i].Kind == MortalBuilding.Keep) return Buildings[i];
                    return null;
                }
            }
        }

        [SerializeField] private BuildingEntry[] _buildings = Array.Empty<BuildingEntry>();
        [Tooltip("Laid on the roads and on the ground patches inside settlements.")]
        [SerializeField] private TileBase _pathTile;

        private readonly List<Settlement> _settlements = new();
        private readonly Dictionary<Building, (int settlement, int building)> _owner = new();
        private SettlementLedger _ledger;
        private Transform _root;

        public static RegionSettlements Current { get; private set; }

        /// <summary>Every settlement, the Seat first (empty until the Region is populated).</summary>
        public IReadOnlyList<Settlement> Settlements => _settlements;

        public Settlement Seat => _settlements.Count > 0 ? _settlements[0] : null;

        /// <summary>The Seat's keep has fallen.</summary>
        public bool SeatCaptured => _ledger != null && _ledger.SeatCaptured;

        /// <summary>A village, farm or camp was razed (every Building in it that is not fence was destroyed).</summary>
        public event Action<Settlement> Razed;

        int IRegionPopulate.PopulateOrder => RegionOrder.Settlements;

        private void OnEnable() => Current = this;

        private void OnDisable()
        {
            if (Current == this) Current = null;
        }

        void IRegionPopulate.Populate(RegionContext region)
        {
            RegionGrid grid = RegionGrid.Current;
            RegionLayout layout = RegionTerrain.Current != null ? RegionTerrain.Current.Layout : null;
            if (grid == null || layout == null || RegionBuildings.Current == null) return;

            SettlementPlan plan = layout.Settlements;
            _ledger = new SettlementLedger(plan.Settlements);
            _root = new GameObject("Settlements").transform;
            _root.SetParent(transform, false);

            foreach (PlacedSettlement placed in plan.Settlements)
            {
                var markers = new SpawnPoint[placed.Markers.Count];
                for (int i = 0; i < markers.Length; i++)
                    markers[i] = new SpawnPoint(placed.Markers[i].Kind, placed.Markers[i].Cell, grid.CentreOf(placed.Markers[i].Cell));
                _settlements.Add(new Settlement(placed, markers));
            }

            // The spec's order: the Seat, then the rest (the plan lists them so), then roads, then the walls that wrap them.
            foreach (Settlement s in _settlements) Build(grid, s, walls: false);
            Paint(grid, plan);
            foreach (Settlement s in _settlements) Build(grid, s, walls: true);
        }

        /// <summary>Destroys the Seat's keep, capturing the Region the way a fight would (the debug menu).</summary>
        public bool DestroySeat() => Kill(Seat?.Keep);

        /// <summary>Destroys everything in the next standing village, farm or camp that is not fence, razing it (the debug menu).</summary>
        public bool RazeNext()
        {
            foreach (Settlement s in _settlements)
            {
                if (s.IsSeat || s.IsRazed) continue;
                for (int i = 0; i < s.Buildings.Count; i++)
                    if (!s.Plan.Buildings[i].IsWall) Kill(s.Buildings[i]);
                return true;
            }
            return false;
        }

        private static bool Kill(Building building)
        {
            if (building == null || !building.IsAlive) return false;
            CombatUnit unit = building.Damageable.Unit;
            unit.TakeDamage(unit.MaxHealth * 10f, DamageRules.TrueType);
            return true;
        }

        private BuildingAsset AssetFor(MortalBuilding kind)
        {
            foreach (BuildingEntry e in _buildings)
                if (e.Kind == kind) return e.Asset;
            return null;
        }

        private void Build(RegionGrid grid, Settlement settlement, bool walls)
        {
            PlacedSettlement placed = settlement.Plan;
            for (int i = 0; i < placed.Buildings.Count; i++)
            {
                PlacedBuilding spot = placed.Buildings[i];
                if (spot.IsWall != walls) continue;
                BuildingAsset asset = AssetFor(spot.Kind);
                if (asset == null || asset.Prefab == null) continue;
                var size = new Vector2Int(spot.Size.x, spot.Size.y);
                GameObject go = Instantiate(asset.Prefab, grid.CentreOf(spot.Origin, size), Quaternion.identity, _root);
                var building = go.GetComponent<Building>();
                RegionBuildings.Current.ClaimNow(building);
                settlement.Set(i, building);
                _owner[building] = (placed.Index, i);
                building.Destroyed += OnDestroyed;
            }
        }

        // Roads and the ground patches inside settlements are laid as path tiles over the grass.
        private void Paint(RegionGrid grid, SettlementPlan plan)
        {
            Tilemap ground = null;
            foreach (Tilemap map in FindObjectsByType<Tilemap>(FindObjectsInactive.Exclude))
                if (map.name == "Ground") { ground = map; break; }
            if (ground == null || _pathTile == null) return;

            void Lay(Int2 cell)
            {
                if (grid.Contains(cell)) ground.SetTile(ground.WorldToCell(grid.CentreOf(cell)), _pathTile);
            }

            foreach (PlacedSettlement s in plan.Settlements)
            {
                foreach (KeyValuePair<Int2, GroundTile> tile in s.Tiles) Lay(tile.Key);
                foreach (PlacedRoadEnd end in s.RoadEnds)
                    if (end.InUse || s.IsSeat)
                        foreach (Int2 g in end.Gap) Lay(g);
            }
            foreach (Road road in plan.Roads)
                foreach (Int2 cell in road.Cells) Lay(cell);
        }

        private void OnDestroyed(Building building)
        {
            building.Destroyed -= OnDestroyed;
            if (_ledger == null || !_owner.TryGetValue(building, out (int settlement, int building) at)) return;
            SettlementEffect effect = _ledger.Destroyed(at.settlement, at.building);
            if (effect.HasFlag(SettlementEffect.Razed)) OnRazed(_settlements[at.settlement]);
            if (effect.HasFlag(SettlementEffect.SeatCaptured)) RegionEvents.End(RegionEnd.Captured);
        }

        // Razing a settlement is a reward: an Item draft (unless the Region is already over or drafts are switched off).
        private void OnRazed(Settlement settlement)
        {
            settlement.IsRazed = true;
            Razed?.Invoke(settlement);
            if (RegionEvents.HasEnded) return;
            var drafts = FindAnyObjectByType<ItemDrafts>();
            if (drafts != null && drafts.isActiveAndEnabled) drafts.Present();
        }
    }
}
