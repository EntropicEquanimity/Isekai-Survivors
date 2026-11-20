using Blondie.Core;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// A softer edge for fog of war: dark puffs of the fog cloud texture (Art/Vfx/Resources/Vfx_Fog_Cloud, from
    /// D:\GameDev\Assets\VFX\fogcloud.png) along the line where what Jack's side sees meets what it does not, so the edge
    /// is not a hard staircase of cells. It is its own overlay, drawn just above <see cref="FogOverlay"/> and leaving it
    /// alone. Every <see cref="ScanEvery"/> seconds it looks at the cells in view, keeps the puffs that still sit on an
    /// edge, fades out those that no longer do and places new ones, from a fixed pool of <see cref="PoolSize"/> sprites.
    /// Puffs sit on whole pixels (a slow drift of a pixel or two) and keep their place while the edge moves away.
    /// </summary>
    [RegionSystem]
    public sealed class FogEdgeOverlay : MonoBehaviour
    {
        public const string CloudResource = "Vfx_Fog_Cloud";
        public const int PoolSize = 96;
        public const float ScanEvery = 0.25f;

        private const int SortingOrder = FogOverlay.SortingOrder + 1;
        private const int ViewMargin = 4;       // cells beyond the screen that still get puffs
        private const int Spacing = 3;          // cells between two puffs, at least
        private const float PeakAlpha = 0.35f;
        private const float OutwardCells = 1.5f;   // a puff sits mostly over the fog, so it only feathers the seen ground by a cell or two
        private const float FadePerSecond = 3f;
        private const float PixelSize = 1f / 16f;

        private static readonly Color Tint = new(0.1f, 0.08f, 0.16f, 1f);

        private readonly SpriteRenderer[] _puffs = new SpriteRenderer[PoolSize];
        private readonly Int2[] _cells = new Int2[PoolSize];
        private readonly Vector3[] _offset = new Vector3[PoolSize];
        private readonly float[] _alpha = new float[PoolSize];
        private readonly bool[] _fadingOut = new bool[PoolSize];
        private readonly bool[] _used = new bool[PoolSize];
        private Sprite _cloud;
        private CellLayer _visible;
        private RegionGrid _grid;
        private float _scanAt = -1f;
        private bool _built;

        public static FogEdgeOverlay Current { get; private set; }

        /// <summary>Puffs showing (fading out ones too), for tests.</summary>
        public int PuffCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < PoolSize; i++)
                    if (_used[i]) n++;
                return n;
            }
        }

        /// <summary>The puffs' sprite renderers, for tests (null where unused).</summary>
        public SpriteRenderer PuffAt(int index) => _used[index] ? _puffs[index] : null;

        /// <summary>Looks at the edge now (it normally does every <see cref="ScanEvery"/> seconds).</summary>
        public void ScanNow()
        {
            if (!_built && !TryBuild()) return;
            Scan();
        }

        private void OnEnable() => Current = this;

        private void OnDisable()
        {
            if (Current == this) Current = null;
        }

        private void OnDestroy()
        {
            for (int i = 0; i < PoolSize; i++)
                if (_puffs[i] != null) Destroy(_puffs[i].gameObject);
        }

        private void LateUpdate()
        {
            if (!_built && !TryBuild()) return;
            long start = VfxCost.Start();
            bool on = RegionVision.IsOn;
            if (on && Time.unscaledTime >= _scanAt)
            {
                _scanAt = Time.unscaledTime + ScanEvery;
                Scan();
            }
            Animate(on);
            VfxCost.Stop(start);
        }

        private bool TryBuild()
        {
            _grid = RegionGrid.Current;
            RegionVision vision = RegionVision.Current;
            if (_grid == null || !_grid.IsBaked || vision == null || !vision.IsReady) return false;
            var texture = Resources.Load<Texture2D>(CloudResource);
            if (texture == null) { enabled = false; return false; }
            _visible = _grid.Layer(RegionVision.VisibleLayer(Side.Yours));
            // The pivot on a whole pixel, so a puff placed on a pixel stays on the grid.
            _cloud = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 16f);
            _cloud.name = CloudResource;
            for (int i = 0; i < PoolSize; i++)
            {
                var go = new GameObject("Fog Puff " + i);
                go.transform.SetParent(transform, false);
                SpriteRenderer r = go.AddComponent<SpriteRenderer>();
                r.sprite = _cloud;
                r.sortingOrder = SortingOrder;
                r.color = new Color(Tint.r, Tint.g, Tint.b, 0f);
                go.SetActive(false);
                _puffs[i] = r;
            }
            _built = true;
            return true;
        }

        // A cell Jack's side sees that touches one it does not: the edge of what is seen.
        private bool IsEdge(Int2 cell)
        {
            if (_visible.Get(cell) == 0) return false;
            return !Sees(cell.x + 1, cell.y) || !Sees(cell.x - 1, cell.y) || !Sees(cell.x, cell.y + 1) || !Sees(cell.x, cell.y - 1);
        }

        // Which way the unseen ground lies from an edge cell.
        private Vector3 Outward(Int2 cell)
        {
            float x = (Sees(cell.x + 1, cell.y) ? 0f : 1f) - (Sees(cell.x - 1, cell.y) ? 0f : 1f);
            float y = (Sees(cell.x, cell.y + 1) ? 0f : 1f) - (Sees(cell.x, cell.y - 1) ? 0f : 1f);
            var d = new Vector3(x, y, 0f);
            return d.sqrMagnitude > 0f ? d.normalized : Vector3.zero;
        }

        private bool Sees(int x, int y)
        {
            var c = new Int2(x, y);
            return _visible.Contains(c) && _visible.Get(c) != 0;
        }

        private void Scan()
        {
            // Puffs whose cell is no longer an edge fade away; the rest stay.
            for (int i = 0; i < PoolSize; i++)
                if (_used[i] && !_fadingOut[i] && !IsEdge(_cells[i])) _fadingOut[i] = true;

            Camera cam = Camera.main;
            if (cam == null) return;
            Vector3 c = cam.transform.position;
            float halfH = cam.orthographicSize, halfW = halfH * cam.aspect;
            Int2 lo = _grid.CellAt(new Vector3(c.x - halfW, c.y - halfH, 0f));
            Int2 hi = _grid.CellAt(new Vector3(c.x + halfW, c.y + halfH, 0f));
            int x0 = Mathf.Max(0, lo.x - ViewMargin), x1 = Mathf.Min(_grid.Width - 1, hi.x + ViewMargin);
            int y0 = Mathf.Max(0, lo.y - ViewMargin), y1 = Mathf.Min(_grid.Height - 1, hi.y + ViewMargin);

            // Every edge cell is a candidate, but a puff is only put where there is none within Spacing cells already.
            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                if ((x * 7 + y * 13) % 3 != 0) continue;   // a fixed scatter, so the same cells win every scan
                var cell = new Int2(x, y);
                if (!IsEdge(cell) || NearAPuff(cell)) continue;
                if (!Place(cell)) return;
            }
        }

        private bool NearAPuff(Int2 cell)
        {
            for (int i = 0; i < PoolSize; i++)
            {
                if (!_used[i] || _fadingOut[i]) continue;
                if (Mathf.Abs(_cells[i].x - cell.x) < Spacing && Mathf.Abs(_cells[i].y - cell.y) < Spacing) return true;
            }
            return false;
        }

        private bool Place(Int2 cell)
        {
            for (int i = 0; i < PoolSize; i++)
            {
                if (_used[i]) continue;
                _used[i] = true;
                _fadingOut[i] = false;
                _cells[i] = cell;
                _offset[i] = Outward(cell) * (OutwardCells * _grid.CellSize);
                _alpha[i] = 0f;
                _puffs[i].gameObject.SetActive(true);
                return true;
            }
            return false;   // the pool is full
        }

        private void Animate(bool on)
        {
            float dt = Time.unscaledDeltaTime;
            float t = Time.unscaledTime;
            for (int i = 0; i < PoolSize; i++)
            {
                if (!_used[i]) continue;
                float target = on && !_fadingOut[i] ? PeakAlpha : 0f;
                _alpha[i] = Mathf.MoveTowards(_alpha[i], target, FadePerSecond * dt * PeakAlpha);
                if (_alpha[i] <= 0f && target == 0f)
                {
                    _used[i] = false;
                    _puffs[i].gameObject.SetActive(false);
                    continue;
                }
                SpriteRenderer r = _puffs[i];
                r.color = new Color(Tint.r, Tint.g, Tint.b, _alpha[i]);
                // A slow drift of a pixel or two, in whole pixels.
                Int2 cell = _cells[i];
                Vector3 at = _grid.CentreOf(cell) + _offset[i];
                float phase = cell.x * 0.7f + cell.y * 1.3f;
                at.x += Mathf.Round(Mathf.Sin(t * 0.5f + phase) * 2f) * PixelSize;
                at.y += Mathf.Round(Mathf.Cos(t * 0.4f + phase) * 2f) * PixelSize;
                at.x = Mathf.Round(at.x / PixelSize) * PixelSize;
                at.y = Mathf.Round(at.y / PixelSize) * PixelSize;
                at.z = 0f;
                r.transform.position = at;
            }
        }
    }
}
