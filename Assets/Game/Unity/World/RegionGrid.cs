using System;
using System.Collections.Generic;
using Blondie.Core;
using Blondie.FlowFields;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = System.Random;

namespace Isekai
{
    /// <summary>
    /// The Region's ground as a grid of cells: its size, cell size and origin, and every conversion between world
    /// positions and cells. Walkability comes from the horde's <see cref="FlowFieldGridComponent"/> (baked from the
    /// obstacle colliders); Buildings, spawners, the map and later the corruption layer all go through here instead of
    /// converting positions themselves, so the cell size (16 px after the art swap) is set in one place.
    /// <para>Tilemap colliders only get their shapes a frame after a Region loads at runtime, after the flow grid baked
    /// in Start. So this bakes once more after a frame and raises <see cref="Baked"/>: listeners re-block Building
    /// ground and recompute paths then, and spawning that needs walls should wait for <see cref="IsBaked"/>.</para>
    /// </summary>
    [DefaultExecutionOrder(-500)]
    [RequireComponent(typeof(FlowFieldGridComponent))]
    public sealed class RegionGrid : MonoBehaviour
    {
        private FlowFieldGridComponent _component;
        private readonly Dictionary<string, CellLayer> _layers = new();

        public static RegionGrid Current { get; private set; }

        /// <summary>The walkability and movement-cost grid (blocked cells are walls or Buildings).</summary>
        public FlowFieldGrid Field => Component.Grid;

        public int Width => Field.Width;
        public int Height => Field.Height;
        public float CellSize => Field.CellSize;

        /// <summary>The walls are in (see the class remarks).</summary>
        public bool IsBaked { get; private set; }

        /// <summary>The walls were baked again; anything that wrote costs into <see cref="Field"/> should write them again.</summary>
        public event Action Baked;

        private FlowFieldGridComponent Component => _component != null ? _component : _component = GetComponent<FlowFieldGridComponent>();

        private void OnEnable() => Current = this;

        private void OnDisable()
        {
            if (Current == this) Current = null;
        }

        private System.Collections.IEnumerator Start()
        {
            foreach (TilemapCollider2D t in FindObjectsByType<TilemapCollider2D>(FindObjectsInactive.Exclude)) t.ProcessTilemapChanges();
            Physics2D.SyncTransforms();
            yield return null;
            Component.Bake();
            IsBaked = true;
            Baked?.Invoke();
        }

        public bool Contains(Int2 cell) => Field.Contains(cell);

        /// <summary>
        /// A named byte per cell (vision, explored ground, corruption), made on first use at the map's size. Layers
        /// notify their own listeners and never touch <see cref="Field"/>'s costs, so writing one never invalidates a path.
        /// </summary>
        public CellLayer Layer(string name)
        {
            if (!_layers.TryGetValue(name, out CellLayer layer)) _layers[name] = layer = new CellLayer(name, Width, Height);
            return layer;
        }

        /// <summary>
        /// Gives the map a new size. Only while the Region is starting (before the grid is baked or any path asked
        /// for): the walls are lost and layers start again, so it throws once the grid is baked or a
        /// <see cref="CellLayer"/> has been handed out (it would be orphaned).
        /// </summary>
        public void Resize(int width, int height)
        {
            if (IsBaked || _layers.Count > 0) throw new InvalidOperationException("RegionGrid.Resize is only for Region start: the grid is already baked or has layers.");
            if (Width == width && Height == height) return;
            Component.Width = width;
            Component.Height = height;
            Component.Rebuild();
            _layers.Clear();
        }

        /// <summary>Makes a cell a wall for paths and building (an <see cref="Obstacle"/> placed after the bake).</summary>
        public void Block(Int2 cell)
        {
            if (Contains(cell)) Field.SetCostRect(cell, new Int2(1, 1), FlowFieldGrid.Blocked);
        }

        /// <summary>Opens a cell again (an obstacle destroyed).</summary>
        public void Unblock(Int2 cell)
        {
            if (Contains(cell)) Field.SetCostRect(cell, new Int2(1, 1), FlowFieldGrid.DefaultCost);
        }

        public bool IsWalkable(Int2 cell) => Field.IsWalkable(cell);

        public bool IsWalkable(Vector3 world) => Field.IsWalkable(CellAt(world));

        public Int2 CellAt(Vector3 world) => Field.WorldToCell(new Float3(world.x, world.y, 0f));

        /// <summary>The world centre of a cell.</summary>
        public Vector3 CentreOf(Int2 cell)
        {
            Float3 c = Field.CellToWorld(cell);
            return new Vector3(c.x, c.y, 0f);
        }

        /// <summary>The world centre of a footprint of <paramref name="size"/> cells whose lower-left cell is <paramref name="origin"/>.</summary>
        public Vector3 CentreOf(Int2 origin, Vector2Int size)
        {
            Vector3 a = CentreOf(origin), b = CentreOf(new Int2(origin.x + size.x - 1, origin.y + size.y - 1));
            return (a + b) * 0.5f;
        }

        /// <summary>The lower-left cell of a footprint of <paramref name="size"/> cells centred on <paramref name="centre"/>.</summary>
        public Int2 FootprintOrigin(Vector3 centre, Vector2Int size) =>
            CellAt(centre - new Vector3((size.x - 1) * 0.5f * CellSize, (size.y - 1) * 0.5f * CellSize, 0f));

        /// <summary>
        /// A walkable spot from a <see cref="SpawnPlacement"/> shape that <paramref name="accept"/> agrees to, jittered
        /// within its cell by <paramref name="jitter"/> (a fraction of a cell).
        /// </summary>
        public bool TryPickSpot(Random random, Func<Random, Int2> shape, Func<Vector3, bool> accept, out Vector3 spot, float jitter = 0f, int attempts = 30)
        {
            spot = default;
            if (!SpawnPlacement.TryPick(random, shape, c => Contains(c) && IsWalkable(c), accept == null ? null : c => accept(CentreOf(c)), out Int2 cell, attempts))
                return false;
            spot = CentreOf(cell);
            if (jitter > 0f)
                spot += new Vector3((float)(random.NextDouble() * 2 - 1) * jitter * CellSize * 0.5f, (float)(random.NextDouble() * 2 - 1) * jitter * CellSize * 0.5f, 0f);
            return true;
        }

        /// <summary>Anywhere on the map, away from the edge by <paramref name="margin"/> cells.</summary>
        public Func<Random, Int2> Anywhere(int margin = 0) => SpawnPlacement.Anywhere(Width, Height, margin);

        /// <summary>Between <paramref name="minDistance"/> and <paramref name="maxDistance"/> world units from <paramref name="centre"/>.</summary>
        public Func<Random, Int2> Ring(Vector3 centre, float minDistance, float maxDistance) =>
            SpawnPlacement.Ring(CellAt(centre), minDistance / CellSize, maxDistance / CellSize);

        /// <summary>Within <paramref name="band"/> cells of the map's edge.</summary>
        public Func<Random, Int2> EdgeBand(int band) => SpawnPlacement.EdgeBand(Width, Height, band);
    }
}
