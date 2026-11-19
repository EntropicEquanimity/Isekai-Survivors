using Blondie.Core;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// Fog of war drawn over the world: one sprite above the ground and everything on it, one texel per ground cell
    /// (point-filtered), black where your side has never seen, dimmed where it has but doesn't now, clear where it
    /// sees. It listens to the "Visible.Yours" and "Explored.Yours" layers and rewrites only the texels that changed,
    /// uploading the texture once a frame when something did.
    /// </summary>
    [RegionSystem]
    public sealed class FogOverlay : MonoBehaviour
    {
        /// <summary>Above every sprite in the world (health bars sit near 20) and below the UI panels.</summary>
        public const int SortingOrder = 5000;

        public static readonly Color32 UnseenColour = new(0, 0, 0, 255);
        public static readonly Color32 ExploredColour = new(0, 0, 0, 140);
        public static readonly Color32 VisibleColour = new(0, 0, 0, 0);

        private Texture2D _texture;
        private Color32[] _pixels;
        private SpriteRenderer _renderer;
        private CellLayer _visible, _explored;
        private int _width;
        private bool _dirty;

        public static FogOverlay Current { get; private set; }

        /// <summary>The fog texture (for tests).</summary>
        public Texture2D Texture => _texture;

        /// <summary>The fog's colour over one cell right now (for tests).</summary>
        public Color32 ColourAt(Int2 cell) => _pixels != null && _visible.Contains(cell) ? _pixels[cell.x + cell.y * _width] : UnseenColour;

        private void OnEnable() => Current = this;

        private void OnDisable()
        {
            if (Current == this) Current = null;
            Unhook();
        }

        private void OnDestroy()
        {
            if (_renderer != null) Destroy(_renderer.gameObject);
            if (_texture != null) Destroy(_texture);
        }

        private void LateUpdate()
        {
            if (_texture == null && !TryBuild()) return;
            if (_renderer.enabled != RegionVision.IsOn) _renderer.enabled = RegionVision.IsOn;
            if (!_dirty) return;
            _dirty = false;
            _texture.SetPixels32(_pixels);
            _texture.Apply(false);
        }

        private bool TryBuild()
        {
            RegionGrid grid = RegionGrid.Current;
            if (grid == null || !grid.IsBaked) return false;
            _width = grid.Width;
            int height = grid.Height;
            _pixels = new Color32[_width * height];
            for (int i = 0; i < _pixels.Length; i++) _pixels[i] = UnseenColour;
            _texture = new Texture2D(_width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "Fog of War",
            };
            _dirty = true;

            float cell = grid.CellSize;
            Sprite sprite = Sprite.Create(_texture, new Rect(0, 0, _width, height), Vector2.zero, 1f / cell, 0, SpriteMeshType.FullRect);
            var go = new GameObject("Fog of War");
            go.transform.SetParent(transform, false);
            go.transform.position = grid.CentreOf(new Int2(0, 0)) - new Vector3(cell * 0.5f, cell * 0.5f, 0f);
            _renderer = go.AddComponent<SpriteRenderer>();
            _renderer.sprite = sprite;
            _renderer.sortingOrder = SortingOrder;

            _visible = grid.Layer(RegionVision.VisibleLayer(Side.Yours));
            _explored = grid.Layer(RegionVision.ExploredLayer(Side.Yours));
            _visible.Changed += OnCellChanged;
            _explored.Changed += OnCellChanged;
            for (int y = 0; y < height; y++)   // whatever was seen before the overlay came up
            for (int x = 0; x < _width; x++)
                Draw(new Int2(x, y));
            return true;
        }

        private void Unhook()
        {
            if (_visible != null) _visible.Changed -= OnCellChanged;
            if (_explored != null) _explored.Changed -= OnCellChanged;
        }

        private void OnCellChanged(Int2 cell, byte before, byte after) => Draw(cell);

        private void Draw(Int2 cell)
        {
            Color32 colour = _visible.Get(cell) != 0 ? VisibleColour : _explored.Get(cell) != 0 ? ExploredColour : UnseenColour;
            int i = cell.x + cell.y * _width;
            if (_pixels[i].a == colour.a) return;
            _pixels[i] = colour;
            _dirty = true;
        }
    }
}
