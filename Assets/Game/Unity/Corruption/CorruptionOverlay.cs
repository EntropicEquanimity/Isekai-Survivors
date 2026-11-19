using Blondie.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Isekai
{
    /// <summary>
    /// Draws corruption over the ground: a Tilemap above the ground tiles with the placeholder Grass_Corrupted tiles for
    /// levels 1 to 4 (level 0 shows the ground as it is). It redraws only the cells whose level changed, from the
    /// layer's <see cref="CellLayer.Changed"/> event; nothing runs per frame.
    /// </summary>
    [RegionSystem]
    public sealed class CorruptionOverlay : MonoBehaviour
    {
        private const int SortingOrder = -50;

        private readonly Tile[] _tiles = new Tile[(CorruptionTuning.MaxLevel + 1) * CorruptionLook.Variants];
        private CellLayer _layer;
        private Tilemap _tilemap;
        private GameObject _root;

        /// <summary>How many tiles were redrawn so far (a test checks that only changed cells are).</summary>
        public int Redraws { get; private set; }

        public Tilemap Tilemap => _tilemap;

        private void Start()
        {
            RegionCorruption corruption = RegionCorruption.Current;
            var look = Resources.Load<CorruptionLook>("CorruptionLook");
            if (corruption == null || look == null)
            {
                Debug.LogWarning("Corruption overlay: no corruption layer or no CorruptionLook; nothing will be drawn.");
                enabled = false;
                return;
            }
            MakeTiles(look);
            MakeTilemap();
            _layer = corruption.Field.Layer;
            _layer.Changed += OnChanged;
            DrawAll();
        }

        private void OnDestroy()
        {
            if (_layer != null) _layer.Changed -= OnChanged;
            if (_root != null) Destroy(_root);
            foreach (Tile t in _tiles)
                if (t != null) Destroy(t);
        }

        private void MakeTiles(CorruptionLook look)
        {
            for (int level = 1; level <= CorruptionTuning.MaxLevel; level++)
            for (int v = 0; v < CorruptionLook.Variants; v++)
            {
                var tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = look.Sprite(level, v);
                tile.colliderType = Tile.ColliderType.None;
                _tiles[level * CorruptionLook.Variants + v] = tile;
            }
        }

        // The grid's lower-left corner sits at the lower-left of cell (0, 0), so a tile cell is the flow-field cell.
        private void MakeTilemap()
        {
            RegionGrid ground = RegionGrid.Current;
            _root = new GameObject("Corruption Overlay");
            _root.transform.position = ground.CentreOf(new Int2(0, 0)) - new Vector3(ground.CellSize, ground.CellSize, 0f) * 0.5f;
            var grid = _root.AddComponent<Grid>();
            grid.cellSize = new Vector3(ground.CellSize, ground.CellSize, 0f);
            _tilemap = _root.AddComponent<Tilemap>();
            var renderer = _root.AddComponent<TilemapRenderer>();
            renderer.sortingOrder = SortingOrder;
        }

        private void DrawAll()
        {
            for (int y = 0; y < _layer.Height; y++)
            for (int x = 0; x < _layer.Width; x++)
            {
                var cell = new Int2(x, y);
                byte level = _layer.Get(cell);
                if (level > 0) Draw(cell, level);
            }
        }

        private void OnChanged(Int2 cell, byte before, byte after) => Draw(cell, after);

        private void Draw(Int2 cell, byte level)
        {
            Redraws++;
            _tilemap.SetTile(new Vector3Int(cell.x, cell.y, 0), level == 0 ? null : _tiles[level * CorruptionLook.Variants + Variant(cell)]);
        }

        // A stable variant per cell, so ground doesn't tile in rows and doesn't flicker when redrawn.
        private static int Variant(Int2 cell) => (int)(((uint)(cell.x * 73856093) ^ (uint)(cell.y * 19349663)) % CorruptionLook.Variants);

        /// <summary>The sprite drawn at a cell now (null for level 0).</summary>
        public Sprite SpriteAt(Int2 cell) => _tilemap != null ? _tilemap.GetSprite(new Vector3Int(cell.x, cell.y, 0)) : null;
    }
}
