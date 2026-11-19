using System.Collections.Generic;
using Blondie.Core;
using Blondie.FlowFields;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// Where Buildings may go in a Region and placing them: footprints sit on the horde's flow-field cells, must be on
    /// open ground inside a working Base's area, and cost Resources from the Region's stockpile. Placed Buildings block
    /// their cells in the flow field (enemies walk up to them) and unblock them when destroyed.
    /// </summary>
    public sealed class RegionBuildings : MonoBehaviour
    {
        public enum Verdict { Ok, Blocked, OutsideBase, TooExpensive }

        private readonly Dictionary<Building, byte[]> _savedCosts = new();
        private readonly List<Building> _pending = new();
        private BuildingGrid<Building> _grid;

        public static RegionBuildings Current { get; private set; }

        private FlowFieldGrid Field => HordeDirector.Current != null ? HordeDirector.Current.Grid : null;

        private BuildingGrid<Building> Grid => _grid ??= new BuildingGrid<Building>(cell => Field != null && Field.IsWalkable(cell));

        private void OnEnable() => Current = this;

        private void OnDisable()
        {
            if (Current == this) Current = null;
        }

        /// <summary>World size of one ground cell.</summary>
        public float CellSize => Field != null ? Field.CellSize : 1f;

        public Int2 CellAt(Vector3 world) => Field.WorldToCell(new Float3(world.x, world.y, 0f));

        /// <summary>The world centre of a footprint.</summary>
        public Vector3 CentreOf(Int2 origin, Vector2Int size)
        {
            Float3 a = Field.CellToWorld(origin);
            Float3 b = Field.CellToWorld(new Int2(origin.x + size.x - 1, origin.y + size.y - 1));
            return new Vector3((a.x + b.x) * 0.5f, (a.y + b.y) * 0.5f, 0f);
        }

        public Building At(Int2 cell) => Grid.At(cell);

        public Verdict Check(BuildingAsset asset, Int2 origin)
        {
            Int2 size = new(asset.Size.x, asset.Size.y);
            if (Field == null || !Grid.CanPlace(origin, size)) return Verdict.Blocked;
            if (!BaseArea.Covers(CentreOf(origin, asset.Size))) return Verdict.OutsideBase;
            Stockpile pile = RegionEconomy.Current != null ? RegionEconomy.Current.Stockpile : null;
            if (pile == null || !pile.CanAfford(asset.Costs)) return Verdict.TooExpensive;
            return Verdict.Ok;
        }

        /// <summary>Pays for and places <paramref name="asset"/> with its footprint's corner at <paramref name="origin"/>; null if it can't.</summary>
        public Building Place(BuildingAsset asset, Int2 origin)
        {
            if (Check(asset, origin) != Verdict.Ok) return null;
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
            // The footprint whose centre is the Building's position: its lower-left cell holds the lower-left corner point.
            float cs = Field.CellSize;
            Vector3 corner = building.transform.position - new Vector3((size.x - 1) * 0.5f * cs, (size.y - 1) * 0.5f * cs, 0f);
            Int2 origin = CellAt(corner);
            if (!Grid.CanPlace(origin, new Int2(size.x, size.y)))
            {
                Debug.LogWarning($"{building.name} stands on ground that is blocked or taken; it won't block the horde.", building);
                return;
            }
            Occupy(building, origin, size);
        }

        /// <summary>After the ground was baked again: block every placed Building's cells again, remembering the new ground under them.</summary>
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
