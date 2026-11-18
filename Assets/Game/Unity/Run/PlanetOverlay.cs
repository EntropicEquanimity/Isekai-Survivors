using Blondie.Core;
using Blondie.GameSetup;
using UnityEngine;
using UnityEngine.UIElements;

namespace Isekai
{
    /// <summary>
    /// Zooming out past the widest Region view shows the Planet map over the Region (read only); scrolling back in or
    /// Escape returns to the Region. The game keeps running underneath.
    /// </summary>
    public sealed class PlanetOverlay : MonoBehaviour
    {
        private const string PanelName = "Planet Overlay";
        private const string MenuName = "PlanetMap";

        [SerializeField] private PanelSettings _panelSettings;

        private ScrollZoom _zoom;
        private VisualElement _shade;
        private PlanetMapView _map;

        public bool IsOpen { get; private set; }

        private void Start()
        {
            _zoom = FindAnyObjectByType<ScrollZoom>();
            if (_zoom != null)
            {
                _zoom.OutPastLimit += Open;
                _zoom.InWhileBlocked += Close;
            }
            VisualElement root = GamePanels.Mount(this, PanelName, _panelSettings, out PanelRenderer panel, 90f);
            _shade = new VisualElement
            {
                style =
                {
                    position = Position.Absolute, left = 0, right = 0, top = 0, bottom = 0, display = DisplayStyle.None,
                    backgroundColor = new Color(0.05f, 0.05f, 0.1f, 0.85f), alignItems = Align.Center, justifyContent = Justify.Center,
                },
            };
            root.Add(_shade);
            UIPanels.Created(panel);
        }

        private void OnDestroy()
        {
            if (_zoom == null) return;
            _zoom.OutPastLimit -= Open;
            _zoom.InWhileBlocked -= Close;
        }

        public void Open()
        {
            if (IsOpen || RunSession.Run == null || _shade == null) return;
            IsOpen = true;
            _shade.Clear();
            _shade.Add(new Label(RunSession.Run.Current != null ? $"{RunSession.Run.Current.Name} on the Planet" : "The Planet") { style = { fontSize = PlanetMapView.PixelFontSize * 2, color = Color.white } });
            _map = new PlanetMapView(RunSession.Run, new Vector2(880f, 400f), null);
            _shade.Add(_map);
            _shade.Add(new Label("Scroll in to return") { style = { fontSize = PlanetMapView.PixelFontSize, color = new Color(0.8f, 0.8f, 0.85f), marginTop = 22 } });
            _shade.style.display = DisplayStyle.Flex;
            if (_zoom != null) _zoom.Blocked = true;
            MenuStack.Push(MenuName, Close);
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            _shade.style.display = DisplayStyle.None;
            if (_zoom != null) _zoom.Blocked = false;
            MenuStack.Pop(MenuName);
        }
    }
}
