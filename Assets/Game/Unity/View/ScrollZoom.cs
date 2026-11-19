using Blondie.Core;
using Blondie.PixelPerfect;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Isekai
{
    /// <summary>
    /// Mouse-wheel zoom for a <see cref="PixelCamera"/> in whole-number steps, so the art stays pixel perfect: the
    /// camera's reference resolution becomes the screen size divided by the zoom scale. Past the widest view, the far
    /// levels keep that low-res picture but lower the camera's pixels per unit (32, 16, 8), so the whole Region can be
    /// seen while every sprite stays point-sampled. UI panels keep their own grid, so only the world zooms.
    /// </summary>
    [RequireComponent(typeof(PixelCamera))]
    public sealed class ScrollZoom : MonoBehaviour
    {
        [Tooltip("Most zoomed out (before the far levels): each art pixel drawn this many screen pixels wide.")]
        [SerializeField, Min(1)] private int _minScale = 1;
        [Tooltip("Most zoomed in.")]
        [SerializeField, Min(1)] private int _maxScale = 6;
        [SerializeField, Min(1)] private int _startScale = 3;
        [Tooltip("The widest view ever rendered, in art pixels; zooming out stops here on big screens.")]
        [SerializeField] private Vector2Int _maxView = new(1280, 720);
        [Tooltip("Levels past the widest view, each halving the pixels per unit (32 → 16 → 8).")]
        [SerializeField, Range(0, 2)] private int _farLevels = 2;

        private PixelCamera _camera;
        private PixelGrid _baseGrid;
        private ZoomLevels _levels;
        private Int2 _appliedFor;

        public ZoomLevels Levels => _levels ??= new ZoomLevels(_minScale, Mathf.Max(_minScale, _maxScale), _startScale,
            new Int2(_maxView.x, _maxView.y), Mathf.Min(_farLevels, FarLevelsFor(_baseGrid)));

        /// <summary>While true the wheel doesn't zoom (a window over the Region wants it).</summary>
        public bool Blocked { get; set; }

        private void Awake()
        {
            _camera = GetComponent<PixelCamera>();
            _baseGrid = _camera.Grid;
        }

        private void OnEnable() => Apply(force: true);

        private void OnDisable()
        {
            if (_camera != null) _camera.Grid = _baseGrid;
        }

        private void Update()
        {
            Mouse mouse = Mouse.current;
            float scroll = mouse != null ? mouse.scroll.ReadValue().y : 0f;
            if (scroll != 0f && !Blocked && !UIPanels.IsPointerOverUI(mouse.position.ReadValue())) Zoom(scroll > 0f ? 1 : -1);
            else Apply(force: false);
        }

        /// <summary>Steps the zoom (positive = in) and applies it at once.</summary>
        public void Zoom(int steps) => Apply(Levels.Zoom(steps, _camera.OutputSize));

        private void Apply(bool force)
        {
            Int2 screen = _camera.OutputSize;
            if (!force && screen == _appliedFor) return;
            _appliedFor = screen;
            _camera.ReferenceResolution = Levels.ViewFor(screen);
            _camera.Grid = (PixelGrid)((int)_baseGrid / Levels.PixelDivisor);
        }

        // PixelGrid stops at 8 pixels per unit.
        private static int FarLevelsFor(PixelGrid grid)
        {
            int levels = 0;
            for (int ppu = (int)grid; ppu > (int)PixelGrid.Px8; ppu /= 2) levels++;
            return levels;
        }
    }
}
