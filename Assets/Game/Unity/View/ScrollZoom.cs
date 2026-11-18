using Blondie.Core;
using Blondie.PixelPerfect;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Isekai
{
    /// <summary>
    /// Mouse-wheel zoom for a <see cref="PixelCamera"/> in whole-number steps, so the art stays pixel perfect: the
    /// camera's reference resolution becomes the screen size divided by the zoom scale. UI panels keep their own grid,
    /// so only the world zooms.
    /// </summary>
    [RequireComponent(typeof(PixelCamera))]
    public sealed class ScrollZoom : MonoBehaviour
    {
        [Tooltip("Most zoomed out: each art pixel drawn this many screen pixels wide.")]
        [SerializeField, Min(1)] private int _minScale = 1;
        [Tooltip("Most zoomed in.")]
        [SerializeField, Min(1)] private int _maxScale = 6;
        [SerializeField, Min(1)] private int _startScale = 3;
        [Tooltip("The widest view ever rendered, in art pixels; zooming out stops here on big screens.")]
        [SerializeField] private Vector2Int _maxView = new(1280, 720);

        private PixelCamera _camera;
        private ZoomLevels _levels;
        private Int2 _appliedFor;

        public ZoomLevels Levels => _levels ??= new ZoomLevels(_minScale, Mathf.Max(_minScale, _maxScale), _startScale, new Int2(_maxView.x, _maxView.y));

        private void Awake() => _camera = GetComponent<PixelCamera>();

        private void OnEnable() => Apply(force: true);

        private void Update()
        {
            Mouse mouse = Mouse.current;
            float scroll = mouse != null ? mouse.scroll.ReadValue().y : 0f;
            if (scroll != 0f) Zoom(scroll > 0f ? 1 : -1);
            else Apply(force: false);
        }

        /// <summary>Steps the zoom (positive = in) and applies it at once.</summary>
        public void Zoom(int steps) => Apply(Levels.Step(steps));

        private void Apply(bool force)
        {
            Int2 screen = _camera.OutputSize;
            if (!force && screen == _appliedFor) return;
            _appliedFor = screen;
            _camera.ReferenceResolution = Levels.ViewFor(screen);
        }
    }
}
