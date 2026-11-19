using Blondie.Core;
using Blondie.GameSetup;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Isekai
{
    /// <summary>
    /// M shows the Planet map over the Region (read only); M again or Escape returns to the Region. The game keeps
    /// running underneath, and the wheel doesn't zoom the Region while the map is up.
    /// </summary>
    public sealed class PlanetOverlay : MonoBehaviour
    {
        private const string PanelName = "Planet Overlay";
        private const string MenuName = "PlanetMap";

        [SerializeField] private PanelSettings _panelSettings;
        [SerializeField] private Key _toggleKey = Key.M;

        private ScrollZoom _zoom;
        private VisualElement _shade;
        private PlanetMapView _map;

        public bool IsOpen { get; private set; }

        private void Start()
        {
            _zoom = FindAnyObjectByType<ScrollZoom>();
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

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || _toggleKey == Key.None || !keyboard[_toggleKey].wasPressedThisFrame) return;
            if (IsOpen) Close();
            else Open();
        }

        public void Open()
        {
            if (IsOpen || RunSession.Run == null || _shade == null) return;
            IsOpen = true;
            _shade.Clear();
            _shade.Add(new Label(RunSession.Run.Current != null ? $"{RunSession.Run.Current.Name} on the Planet" : "The Planet") { style = { fontSize = PlanetMapView.PixelFontSize * 2, color = Color.white } });
            _map = new PlanetMapView(RunSession.Run, new Vector2(880f, 400f), null);
            _shade.Add(_map);
            _shade.Add(new Label("M or Escape to return") { style = { fontSize = PlanetMapView.PixelFontSize, color = new Color(0.8f, 0.8f, 0.85f), marginTop = 4 } });
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
