using System.Collections.Generic;
using Blondie.Core;
using Blondie.FlowFields;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// The Region's Buildings on its cell grid (<see cref="RegionGrid"/>): which cells each one stands on, placing new
    /// ones (checked by the <see cref="Rules"/>, paid from the stockpile) and blocking their cells for the hordes'
    /// flow fields, unblocking them when destroyed. Pre-placed Buildings (the Main Base) take their ground once the
    /// grid has baked.
    /// </summary>
    public sealed class RegionBuildings : MonoBehaviour
    {
        private readonly Dictionary<Building, byte[]> _savedCosts = new();
        private readonly List<Building> _pending = new();
        private readonly List<IPlacementRule> _rules = new() { new FreeGroundRule(), new AffordableRule() };
        private BuildingGrid<Building> _grid;

        public static RegionBuildings Current { get; private set; }

        /// <summary>The placement conditions, checked in order. Add or replace rules to change where Buildings may go.</summary>
        public IList<IPlacementRule> Rules => _rules;

        private static RegionGrid Ground => RegionGrid.Current;
        private FlowFieldGrid Field => Ground != null ? Ground.Field : null;
        private BuildingGrid<Building> Grid => _grid ??= new BuildingGrid<Building>(cell => Ground != null && Ground.IsWalkable(cell));

        private void OnEnable()
        {
            Current = this;
            if (Ground != null) Ground.Baked += Reblock;
        }

        private void OnDisable()
        {
            if (Current == this) Current = null;
            if (Ground != null) Ground.Baked -= Reblock;
        }

        /// <summary>World size of one ground cell.</summary>
        public float CellSize => Ground != null ? Ground.CellSize : 1f;

        public Int2 CellAt(Vector3 world) => Ground.CellAt(world);

        /// <summary>The world centre of a footprint.</summary>
        public Vector3 CentreOf(Int2 origin, Vector2Int size) => Ground.CentreOf(origin, size);

        public Building At(Int2 cell) => Grid.At(cell);

        /// <summary>Whether every cell of the footprint is open ground with no Building on it.</summary>
        public bool IsFree(Int2 origin, Vector2Int size) => Field != null && Grid.CanPlace(origin, new Int2(size.x, size.y));

        /// <summary>Whether <paramref name="asset"/> may go with its footprint's corner at <paramref name="origin"/>, and why not.</summary>
        public Placement Check(BuildingAsset asset, Int2 origin)
        {
            if (Ground == null) return Placement.No(Verdict.Blocked, "No ground");
            var query = new PlacementQuery(asset, origin, Ground, this);
            foreach (IPlacementRule rule in _rules)
            {
                Placement result = rule.Check(query);
                if (!result.IsOk) return result;
            }
            return Placement.Ok;
        }

        /// <summary>Pays for and places <paramref name="asset"/> with its footprint's corner at <paramref name="origin"/>; null if it can't.</summary>
        public Building Place(BuildingAsset asset, Int2 origin)
        {
            if (!Check(asset, origin).IsOk) return null;
            if (!RegionEconomy.Current.Stockpile.TrySpend(asset.Costs)) return null;
            GameObject go = Instantiate(asset.Prefab, CentreOf(origin, asset.Size), Quaternion.identity);
            var building = go.GetComponent<Building>();
            building.BeginConstruction(asset);
            Occupy(building, origin, asset.Size);
            return building;
        }

        /// <summary>A Building already in the scene (the Main Base) takes its ground on the next Update, after the flow grid has baked.</summary>
        internal void RegisterExisting(Building building) => _pending.Add(building);

        private void Update()
        {
            if (_pending.Count == 0 || Field == null) return;
            foreach (Building b in _pending)
                if (b != null) Claim(b);
            _pending.Clear();
        }

        private void Claim(Building building)
        {
            if (Field == null || building.Asset == null) return;
            Vector2Int size = building.Asset.Size;
            Int2 origin = Ground.FootprintOrigin(building.transform.position, size);
            if (!Grid.CanPlace(origin, new Int2(size.x, size.y)))
            {
                Debug.LogWarning($"{building.name} stands on ground that is blocked or taken; it won't block the horde.", building);
                return;
            }
            Occupy(building, origin, size);
        }

        /// <summary>After the walls were baked again: block every placed Building's cells again, remembering the new ground under them.</summary>
        internal void Reblock()
        {
            if (Field == null) return;
            foreach (Building building in new List<Building>(_savedCosts.Keys))
            {
                if (building == null) continue;
                _savedCosts.Remove(building);
                Occupy(building, building.Origin, building.Asset.Size, alreadyInGrid: true);
            }
        }

        internal void Remove(Building building)
        {
            Grid.Remove(building);
            if (!_savedCosts.TryGetValue(building, out byte[] costs) || Field == null) return;
            _savedCosts.Remove(building);
            Vector2Int size = building.Asset.Size;
            for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
                Field.SetCostRect(new Int2(building.Origin.x + x, building.Origin.y + y), new Int2(1, 1), costs[x + y * size.x]);
        }

        private void Occupy(Building building, Int2 origin, Vector2Int size, bool alreadyInGrid = false)
        {
            if (!alreadyInGrid) Grid.Place(building, origin, new Int2(size.x, size.y));
            building.Origin = origin;
            var costs = new byte[size.x * size.y];
            for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
                costs[x + y * size.x] = Field.GetCost(new Int2(origin.x + x, origin.y + y));
            _savedCosts[building] = costs;
            Field.SetCostRect(origin, new Int2(size.x, size.y), FlowFieldGrid.Blocked);
        }
    }
}
