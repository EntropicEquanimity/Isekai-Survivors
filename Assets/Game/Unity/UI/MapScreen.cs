using System;
using Blondie.Core;
using Blondie.GameSetup;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Isekai
{
    /// <summary>
    /// The map screen over the Region, like a strategy game's map view. Zooming out past the widest view or pressing M
    /// opens it on the Region tab (<see cref="RegionMapView"/>, redrawn a few times a second); the Planet tab shows the
    /// Run's Planet (read only). Scrolling in, M or Escape returns to the Region. The game keeps running underneath.
    /// </summary>
    public sealed class MapScreen : MonoBehaviour
    {
        public enum Tab { Region, Planet }

        private const string PanelName = "Map Screen";
        private const string MenuName = "MapScreen";
        private const float RedrawSeconds = 0.12f;
        private static readonly Color Backdrop = new(0.055f, 0.06f, 0.08f, 1f);
        private static readonly Color Dim = new(0.75f, 0.76f, 0.82f);

        [SerializeField] private PanelSettings _panelSettings;
        [SerializeField] private Key _toggleKey = Key.M;

        private ScrollZoom _zoom;
        private VisualElement _shade;
        private VisualElement _content;
        private Label _title;
        private Button _regionTab, _planetTab;
        private RegionMapView _regionMap;
        private PanelSettings _ownSettings;
        private VisualElement _legend;
        private float _redrawAt;

        public bool IsOpen { get; private set; }
        public Tab Current { get; private set; }
        public RegionMapView RegionMap => _regionMap;

        public event Action Opened;

        private void Start()
        {
            _zoom = FindAnyObjectByType<ScrollZoom>();
            if (_zoom != null)
            {
                _zoom.OutPastLimit += OpenRegion;
                _zoom.InWhileBlocked += Close;
            }
            // Its own panel, sorted above the HUDs that share the game's panel settings.
            if (_panelSettings != null)
            {
                _ownSettings = Instantiate(_panelSettings);
                _ownSettings.name = PanelName;
                _ownSettings.sortingOrder = _panelSettings.sortingOrder + 50;
            }
            VisualElement root = GamePanels.Mount(this, PanelName, _ownSettings, out PanelRenderer panel, 90f);
            _shade = new VisualElement
            {
                style =
                {
                    position = Position.Absolute, left = 0, right = 0, top = 0, bottom = 0, display = DisplayStyle.None,
                    backgroundColor = Backdrop, alignItems = Align.Center, justifyContent = Justify.Center,
                },
            };
            var header = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 6 } };
            _title = new Label { style = { fontSize = PlanetMapView.PixelFontSize * 2, color = Color.white, marginRight = 16 } };
            _regionTab = new Button(() => Show(Tab.Region)) { text = "Region" };
            _planetTab = new Button(() => Show(Tab.Planet)) { text = "Planet" };
            header.Add(_title);
            header.Add(_regionTab);
            header.Add(_planetTab);
            _content = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.FlexStart } };
            _shade.Add(header);
            _shade.Add(_content);
            _shade.Add(new Label("Scroll in, M or Escape to return") { style = { fontSize = PlanetMapView.PixelFontSize, color = Dim, marginTop = 6 } });
            root.Add(_shade);
            UIPanels.Created(panel);
        }

        private void OnDestroy()
        {
            _regionMap?.Dispose();
            if (_ownSettings != null) Destroy(_ownSettings);
            if (_zoom == null) return;
            _zoom.OutPastLimit -= OpenRegion;
            _zoom.InWhileBlocked -= Close;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && _toggleKey != Key.None && keyboard[_toggleKey].wasPressedThisFrame && !GameSetup.IsPaused)
            {
                if (IsOpen) Close();
                else OpenRegion();
            }
            if (IsOpen && GameSetup.IsPaused) Close();
            if (!IsOpen || Current != Tab.Region || _regionMap == null || Time.unscaledTime < _redrawAt) return;
            _redrawAt = Time.unscaledTime + RedrawSeconds;
            _regionMap.Redraw();
        }

        public void OpenRegion() => Open(Tab.Region);

        public void Open(Tab tab)
        {
            if (_shade == null) return;
            if (!IsOpen)
            {
                IsOpen = true;
                _shade.style.display = DisplayStyle.Flex;
                if (_zoom != null) _zoom.Blocked = true;
                MenuStack.Push(MenuName, Close);
                Opened?.Invoke();
            }
            Show(tab);
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            _shade.style.display = DisplayStyle.None;
            if (_zoom != null) _zoom.Blocked = false;
            MenuStack.Pop(MenuName);
        }

        public void Show(Tab tab)
        {
            if (tab == Tab.Planet && RunSession.Run == null) tab = Tab.Region;
            if (tab == Tab.Region && HordeDirector.Current == null) tab = Tab.Planet;
            Current = tab;
            _content.Clear();
            _regionTab.SetEnabled(tab != Tab.Region);
            _planetTab.SetEnabled(RunSession.Run != null && tab != Tab.Planet);
            string region = RunSession.Run?.Current?.Name ?? "This Region";
            if (tab == Tab.Planet)
            {
                _title.text = "The Planet";
                if (RunSession.Run != null) _content.Add(new PlanetMapView(RunSession.Run, new Vector2(880f, 380f), null));
                return;
            }
            _title.text = region;
            if (_regionMap == null && HordeDirector.Current != null && HordeDirector.Current.Grid.IsCreated)
                _regionMap = new RegionMapView(HordeDirector.Current.Grid);
            if (_regionMap == null) return;
            _regionMap.Redraw();
            _redrawAt = Time.unscaledTime + RedrawSeconds;
            _content.Add(_regionMap);
            _content.Add(_legend ??= Legend());
        }

        private static VisualElement Legend()
        {
            var legend = new VisualElement { style = { marginLeft = 12, width = 160 } };
            Row(legend, Swatch(RegionMapView.You, 6, true), "You");
            Row(legend, Swatch(RegionMapView.Militia, 4, false), "Militia");
            Row(legend, Swatch(RegionMapView.Attacker, 4, false), "Enemy, attacking");
            Row(legend, Swatch(RegionMapView.Roamer, 4, false), "Enemy, roaming");
            Row(legend, Swatch(RegionMapView.Attacker, 8, true), "Elite enemy");
            Row(legend, IconImage("Nest", RegionMapView.Nest), "Enemy nest");
            Row(legend, IconImage("Main Base", RegionMapView.Home), "Main Base");
            foreach (string building in new[] { "Barracks", "Tower", "Outpost" })
                Row(legend, IconImage(building, RegionMapView.Ally), building);
            Row(legend, Swatch(new Color32(196, 196, 186, 255), 4, false), "Stone");
            Row(legend, Swatch(new Color32(235, 235, 240, 255), 8, false, hollow: true), "Camera view");
            return legend;
        }

        private static void Row(VisualElement parent, VisualElement mark, string text)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, height = 22 } };
            var slot = new VisualElement { style = { width = 22, alignItems = Align.Center, justifyContent = Justify.Center } };
            slot.Add(mark);
            row.Add(slot);
            row.Add(new Label(text) { style = { fontSize = PlanetMapView.PixelFontSize, color = Color.white, marginLeft = 6 } });
            parent.Add(row);
        }

        private static VisualElement Swatch(Color32 colour, int size, bool outlined, bool hollow = false)
        {
            var box = new VisualElement { style = { width = size, height = size } };
            if (hollow)
            {
                box.style.borderTopWidth = box.style.borderBottomWidth = box.style.borderLeftWidth = box.style.borderRightWidth = 1;
                box.style.borderTopColor = box.style.borderBottomColor = box.style.borderLeftColor = box.style.borderRightColor = (Color)colour;
            }
            else box.style.backgroundColor = (Color)colour;
            if (outlined)
            {
                box.style.borderTopWidth = box.style.borderBottomWidth = box.style.borderLeftWidth = box.style.borderRightWidth = 1;
                box.style.borderTopColor = box.style.borderBottomColor = box.style.borderLeftColor = box.style.borderRightColor = Color.black;
            }
            return box;
        }

        private static VisualElement IconImage(string icon, Color32 colour)
        {
            Texture2D tex = RegionMapView.IconTexture(RegionMapView.Icons[icon], colour);
            return new VisualElement { style = { width = tex.width, height = tex.height, backgroundImage = Background.FromTexture2D(tex) } };
        }
    }
}
