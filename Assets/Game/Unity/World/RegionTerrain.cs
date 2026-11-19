using System.Collections.Generic;
using Blondie.Core;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = System.Random;

namespace Isekai
{
    /// <summary>
    /// Builds the generated Region's ground from the recipe: a <see cref="RegionLayout"/> decided from the seed, made
    /// real in two steps. Setup (<see cref="RegionOrder.Terrain"/>, before the walls are baked): grass for the whole
    /// map, invisible walls around its edge, Jack's Character and the Main Base moved to the start corner, the Seat's
    /// spot reserved. Populate (after settlements and roads, before the wilds): rocks, cliffs, Stone and Wood nodes and
    /// the wildlife spawn spots, skipping any cell a settlement already took.
    /// <para>Rocks and cliffs are <see cref="Obstacle"/>s, so they block bodies and the hordes' paths, and they mark
    /// the <see cref="TerrainLayers.SightBlock"/> layer for vision. A rock mined out clears its mark. Wildlife spots are
    /// <see cref="WildlifeSpots"/> (and the <see cref="TerrainLayers.WildlifeSpot"/> layer); the Seat's spot is
    /// <see cref="SeatSpot"/>.</para>
    /// <para>Put on a scene object it uses that object's prefabs and tiles; without one the bootstrap adds it bare and
    /// it skips what it has no prefab for.</para>
    /// </summary>
    [RegionSystem]
    public sealed class RegionTerrain : MonoBehaviour, IRegionSetup, IRegionPopulate
    {
        // After settlements and roads (a step may take cells), before the wilds' wanderers pick their homes.
        private const int PopulateSlot = RegionOrder.Wilds - 10;

        [Header("Ground")]
        [SerializeField] private TileBase[] _groundTiles;
        [Tooltip("Rarer tiles scattered over the ground (tufts, flowers).")]
        [SerializeField] private TileBase[] _decorTiles;
        [SerializeField, Range(0f, 1f)] private float _decorChance = 0.12f;
        [Header("Features")]
        [SerializeField] private GameObject _rockPrefab;
        [SerializeField] private GameObject _cliffPrefab;
        [SerializeField] private GameObject _stoneNodePrefab;
        [SerializeField] private GameObject _woodNodePrefab;
        [Tooltip("How thick the invisible walls around the map are, in cells.")]
        [SerializeField, Min(1f)] private float _edgeThickness = 4f;

        private readonly Dictionary<ResourceNode, Int2> _sightRocks = new();
        private readonly List<Vector3> _wildlifeSpots = new();
        private Transform _root;
        private CellLayer _sight;

        public static RegionTerrain Current { get; private set; }

        /// <summary>What was decided for this Region (null if its map was too small to lay out).</summary>
        public RegionLayout Layout { get; private set; }

        /// <summary>The Seat's reserved spot (a cell, then its world centre): the Seat settlement builds its fort here.</summary>
        public Int2 SeatCell => Layout.Seat;

        public Vector3 SeatSpot { get; private set; }

        /// <summary>World spots where wildlife may be spawned, once the Region is populated: open ground, far from the start.</summary>
        public IReadOnlyList<Vector3> WildlifeSpots => _wildlifeSpots;

        int IRegionSetup.SetupOrder => RegionOrder.Terrain;

        int IRegionPopulate.PopulateOrder => PopulateSlot;

        private void OnEnable()
        {
            Current = this;
            ResourceNode.Depleted += OnDepleted;
        }

        private void OnDisable()
        {
            if (Current == this) Current = null;
            ResourceNode.Depleted -= OnDepleted;
        }

        void IRegionSetup.Setup(RegionContext region)
        {
            RegionGrid grid = RegionGrid.Current;
            if (grid == null) return;
            RegionRecipe recipe = region.Recipe;
            if (grid.Width != recipe.Width || grid.Height != recipe.Height)
                Debug.LogWarning($"The map grid is {grid.Width}x{grid.Height} but the recipe ({recipe}) asks for another size.");
            if (recipe.Width < RegionLayout.MinSize || recipe.Height < RegionLayout.MinSize)
            {
                Debug.LogWarning($"The recipe ({recipe}) is too small to lay out; the Region keeps its hand-set ground.");
                return;
            }

            Layout = RegionLayout.Generate(recipe);
            _root = new GameObject("Terrain").transform;
            _root.SetParent(transform, false);
            _sight = grid.Layer(TerrainLayers.SightBlock);
            grid.Layer(TerrainLayers.WildlifeSpot);

            PaintGround(grid, region.Random("ground"));
            BuildEdge(grid);
            SeatSpot = grid.CentreOf(Layout.Seat);
            MoveStart(grid);
        }

        void IRegionPopulate.Populate(RegionContext region)
        {
            RegionGrid grid = RegionGrid.Current;
            if (grid == null || Layout == null) return;
            foreach (Int2 cell in Layout.Rocks)
            {
                ResourceNode rock = Place(grid, _rockPrefab, cell)?.GetComponent<ResourceNode>();
                if (rock == null) continue;
                _sightRocks[rock] = cell;
                _sight.Set(cell, 1);
            }
            foreach (Int2 cell in Layout.Cliffs)
                if (Place(grid, _cliffPrefab, cell) != null) _sight.Set(cell, 1);
            foreach (Int2 cell in Layout.StoneNodes) Place(grid, _stoneNodePrefab, cell);
            foreach (Int2 cell in Layout.WoodNodes) Place(grid, _woodNodePrefab, cell);

            CellLayer spots = grid.Layer(TerrainLayers.WildlifeSpot);
            foreach (Int2 cell in Layout.WildlifeSpots)
            {
                if (!grid.IsWalkable(cell)) continue;
                _wildlifeSpots.Add(grid.CentreOf(cell));
                spots.Set(cell, 1);
            }
        }

        // One feature on its cell, unless there is no prefab or something (a settlement) already holds the cell.
        private GameObject Place(RegionGrid grid, GameObject prefab, Int2 cell)
        {
            if (prefab == null || !grid.IsWalkable(cell)) return null;
            return Instantiate(prefab, grid.CentreOf(cell), Quaternion.identity, _root);
        }

        private void OnDepleted(ResourceNode node)
        {
            if (_sight == null || !_sightRocks.TryGetValue(node, out Int2 cell)) return;
            _sightRocks.Remove(node);
            _sight.Set(cell, 0);
        }

        // Grass over every cell of the map, with the odd tuft or flower; whatever the scene's ground held before goes.
        private void PaintGround(RegionGrid grid, Random random)
        {
            Tilemap ground = null;
            foreach (Tilemap map in FindObjectsByType<Tilemap>(FindObjectsInactive.Exclude))
                if (map.name == "Ground") { ground = map; break; }
            if (ground == null || _groundTiles == null || _groundTiles.Length == 0) return;

            ground.ClearAllTiles();
            var cells = new Vector3Int[grid.Width * grid.Height];
            var tiles = new TileBase[cells.Length];
            bool decor = _decorTiles != null && _decorTiles.Length > 0;
            for (int y = 0; y < grid.Height; y++)
            for (int x = 0; x < grid.Width; x++)
            {
                int i = x + y * grid.Width;
                cells[i] = ground.WorldToCell(grid.CentreOf(new Int2(x, y)));
                bool flower = decor && random.NextDouble() < _decorChance;
                tiles[i] = flower ? _decorTiles[random.Next(_decorTiles.Length)] : _groundTiles[random.Next(_groundTiles.Length)];
            }
            ground.SetTiles(cells, tiles);
        }

        // Four invisible walls just outside the map, on the same physics layer as the rocks.
        private void BuildEdge(RegionGrid grid)
        {
            var go = new GameObject("Map Edge");
            go.transform.SetParent(_root, false);
            if (_rockPrefab != null) go.layer = _rockPrefab.layer;
            Vector3 low = grid.CentreOf(new Int2(0, 0)) - new Vector3(0.5f, 0.5f, 0f) * grid.CellSize;
            float w = grid.Width * grid.CellSize, h = grid.Height * grid.CellSize, t = _edgeThickness;
            Wall(go, new Vector2(low.x + w * 0.5f, low.y - t * 0.5f), new Vector2(w + 2f * t, t));
            Wall(go, new Vector2(low.x + w * 0.5f, low.y + h + t * 0.5f), new Vector2(w + 2f * t, t));
            Wall(go, new Vector2(low.x - t * 0.5f, low.y + h * 0.5f), new Vector2(t, h + 2f * t));
            Wall(go, new Vector2(low.x + w + t * 0.5f, low.y + h * 0.5f), new Vector2(t, h + 2f * t));
        }

        private static void Wall(GameObject go, Vector2 centre, Vector2 size)
        {
            var box = go.AddComponent<BoxCollider2D>();
            box.offset = centre;
            box.size = size;
        }

        // Jack and the Main Base start in their corner; the camera starts there too instead of sweeping over from the old spot.
        private void MoveStart(RegionGrid grid)
        {
            Vector3 at = grid.CentreOf(Layout.Character);
            var control = FindAnyObjectByType<CharacterControl>();
            if (control != null) control.transform.position = at;
            foreach (Building scene in FindObjectsByType<Building>(FindObjectsInactive.Exclude))
            {
                if (!scene.IsMainBase) continue;
                Building b = AnchorPlacement.ChosenFor(scene);
                Vector2Int size = b.Asset != null ? b.Asset.Size : new Vector2Int(RegionLayout.AnchorSize, RegionLayout.AnchorSize);
                b.transform.position = grid.CentreOf(Layout.Anchor, size);
            }
            Camera cam = Camera.main;
            if (cam != null) cam.transform.position = new Vector3(at.x, at.y, cam.transform.position.z);
        }
    }
}
