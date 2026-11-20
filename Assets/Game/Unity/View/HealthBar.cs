using Blondie.Combat;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// A small pixel health bar over a <see cref="Damageable"/>: a dark back and a coloured fill, whole art pixels wide,
    /// placed just above the art. Hidden at full health (unless <see cref="_alwaysShow"/>) and when dead. Driven by the
    /// Damageable's events, so thousands of unhurt enemies cost nothing per frame; the bar is only built when first needed.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HealthBar : MonoBehaviour
    {
        private const int SortingOrder = 20;
        private static Sprite s_pixel;

        [Tooltip("Bar width in art pixels.")]
        [SerializeField, Min(2)] private int _width = 12;
        [SerializeField, Min(1)] private int _height = 2;
        [SerializeField] private Color _fill = new(0.35f, 0.9f, 0.35f);
        [SerializeField] private Color _back = new(0.08f, 0.08f, 0.1f, 0.9f);
        [Tooltip("Shown at full health too (e.g. the Character).")]
        [SerializeField] private bool _alwaysShow;
        [Tooltip("Art pixels between the top of the art and the bar.")]
        [SerializeField, Min(0)] private int _gap = 2;

        private Damageable _damageable;
        private Transform _bar;
        private Transform _fillPart;
        private bool _subscribed;
        private float _ppu = 32f;   // the art's pixels per unit, read when the bar is built

        public bool IsShown => _bar != null && _bar.gameObject.activeSelf;

        /// <summary>The fill's width in art pixels.</summary>
        public int FilledPixels { get; private set; }

        // Built hidden right away: pooled enemies are made once (at load) and reused, so a first hit on many at once
        // (an area attack into a crowd) doesn't create all their bars in one frame.
        private void Awake()
        {
            _damageable = GetComponentInParent<Damageable>();
            if (_damageable == null) return;
            Build();
            _bar.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            if (_damageable == null) return;
            if (!_subscribed)
            {
                _damageable.Damaged += OnDamaged;
                _damageable.Healed += OnHealed;
                _damageable.Died += OnDamaged;
                _damageable.Revived += Refresh;
                _subscribed = true;
            }
            Refresh();
        }

        private void OnDestroy()
        {
            if (!_subscribed || _damageable == null) return;
            _damageable.Damaged -= OnDamaged;
            _damageable.Healed -= OnHealed;
            _damageable.Died -= OnDamaged;
            _damageable.Revived -= Refresh;
        }

        private void OnDamaged(DamageResult _) => Refresh();

        private void OnHealed(float _, CombatUnit __) => Refresh();

        private void Refresh()
        {
            if (_damageable == null) return;
            float fraction = _damageable.IsDead ? 0f : Mathf.Clamp01(_damageable.Normalized);
            bool show = !_damageable.IsDead && (_alwaysShow || fraction < 0.999f);
            if (!show)
            {
                if (_bar != null) _bar.gameObject.SetActive(false);
                return;
            }
            if (_bar == null) Build();
            _bar.gameObject.SetActive(true);
            FilledPixels = Mathf.Clamp(Mathf.CeilToInt(fraction * _width), fraction > 0f ? 1 : 0, _width);
            _fillPart.localScale = new Vector3(FilledPixels / _ppu, _height / _ppu, 1f);
            _fillPart.localPosition = new Vector3((FilledPixels - _width) * 0.5f / _ppu, 0f, 0f);
        }

        private void Build()
        {
            _ppu = ArtPixelsPerUnit();
            float top = ArtTop();
            _bar = new GameObject("Health Bar").transform;
            _bar.SetParent(transform, false);
            _bar.localPosition = new Vector3(_width % 2 == 0 ? 0f : 0.5f / _ppu, top + (_gap + _height * 0.5f) / _ppu, 0f);
            Part("Back", _back, 0).localScale = new Vector3(_width / _ppu, _height / _ppu, 1f);
            _fillPart = Part("Fill", _fill, 1);
        }

        private Transform Part(string partName, Color color, int order)
        {
            var go = new GameObject(partName);
            go.transform.SetParent(_bar, false);
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = Pixel;
            r.color = color;
            r.sortingOrder = SortingOrder + order;
            return go.transform;
        }

        // Bars are measured in the art's own pixels, whatever grid it was drawn for (32 px today, 16 px after the swap).
        private float ArtPixelsPerUnit()
        {
            foreach (SpriteRenderer r in GetComponentsInChildren<SpriteRenderer>(true))
                if (r.sprite != null) return r.sprite.pixelsPerUnit;
            return 32f;
        }

        // The highest point of this object's art, in local units, at the moment the bar is built.
        private float ArtTop()
        {
            float top = 0.5f;
            bool any = false;
            foreach (SpriteRenderer r in GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (r.sprite == null) continue;
                // The sprite mesh's vertices hug its visible pixels; its rect would include the transparent padding.
                foreach (Vector2 v in r.sprite.vertices)
                {
                    float y = transform.InverseTransformPoint(r.transform.TransformPoint(new Vector3(v.x, r.flipY ? -v.y : v.y, 0f))).y;
                    top = any ? Mathf.Max(top, y) : y;
                    any = true;
                }
            }
            return top;
        }

        private static Sprite Pixel
        {
            get
            {
                if (s_pixel != null) return s_pixel;
                var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, name = "Health Bar Pixel" };
                tex.SetPixel(0, 0, Color.white);
                tex.Apply();
                s_pixel = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);   // one unit: parts scale to whole art pixels
                return s_pixel;
            }
        }
    }
}
