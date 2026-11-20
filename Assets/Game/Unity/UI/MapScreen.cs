using System;
using Blondie.Core;
using Blondie.GameSetup;
using Blondie.UI;
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
    public sealed class MapScreen : MonoBehaviour, IRegionEndListener
    {
        public enum Tab { Region, Planet }

        private const string PanelName = "Map Screen";
        private const string MenuName = "MapScreen";
        private const float RedrawSeconds = 0.12f;

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

        private static readonly string[] SpawnCircleIcon = { "..###..", ".#...#.", "#..#..#", ".#...#.", "..###.." };
        private static readonly string[] RaidIcon = { "#...#", ".#.#.", "..#..", ".#.#.", "#...#" };
        private static readonly string[] RallyIcon = { ".#.", "###", ".#." };

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
                    backgroundColor = GameStyle.Backdrop, alignItems = Align.Center, justifyContent = Justify.Center,
                },
            };
            var header = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 6 } };
            _title = GameStyle.Label("", TextRole.Title);
            _title.style.marginRight = 16;
            _regionTab = new Button(() => Show(Tab.Region)) { text = "Region" };
            _planetTab = new Button(() => Show(Tab.Planet)) { text = "Planet" };
            header.Add(_title);
            header.Add(_regionTab);
            header.Add(_planetTab);
            _content = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.FlexStart } };
            _shade.Add(header);
            _shade.Add(_content);
            Label hint = GameStyle.Label("Scroll in, M or Escape to return", TextRole.Muted);
            hint.style.marginTop = 6;
            _shade.Add(hint);
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
            using (PerfMarkers.MapRedraw.Auto()) _regionMap.Redraw();
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
            var legend = new VisualElement { style = { marginLeft = 12, width = 220 } };
            Row(legend, Swatch(RegionMapView.You, 6, true), "You", TooltipKey.LegendYou);
            Row(legend, Swatch(RegionMapView.Demon, 4, false), "Demons", TooltipKey.LegendDemons);
            Row(legend, Swatch(RegionMapView.Attacker, 4, false), "Mortals, attacking", TooltipKey.LegendMortalsAttacking);
            Row(legend, Swatch(RegionMapView.Roamer, 4, false), "Mortals, roaming", TooltipKey.LegendMortalsRoaming);
            Row(legend, Swatch(RegionMapView.Attacker, 8, true), "Elite Mortals", TooltipKey.LegendEliteMortals);
            Row(legend, IconImage("Main Base", RegionMapView.Home), "Anchor", TooltipKey.LegendMainBase);
            foreach (string building in new[] { "Portal", "Spire", "Outpost" })
                Row(legend, IconImage(building, RegionMapView.Ally), building, building switch
                {
                    "Portal" => TooltipKey.LegendPortal,
                    "Spire" => TooltipKey.LegendSpire,
                    _ => TooltipKey.LegendOutpost,
                });
            Row(legend, IconImage("Building", RegionMapView.Hostile), "Mortal building", TooltipKey.LegendMortalBuilding);
            Row(legend, IconImage(SpawnCircleIcon, new Color32(214, 96, 255, 255)), "Spawn circle", TooltipKey.LegendSpawnCircle);
            Row(legend, IconImage(RaidIcon, new Color32(255, 120, 60, 255)), "Raid mark", TooltipKey.LegendRaidMark);
            Row(legend, IconImage(RallyIcon, new Color32(120, 200, 255, 255)), "Rally mark", TooltipKey.LegendRallyMark);
            Row(legend, Swatch(new Color32(196, 196, 186, 255), 4, false), "Stone", TooltipKey.LegendStone);
            Row(legend, Swatch(new Color32(40, 70, 46, 255), 8, false), "Building footprint", TooltipKey.LegendBuildingFootprint);
            Row(legend, Swatch(new Color32(56, 82, 38, 255), 8, false), "Ground", TooltipKey.LegendGround);
            Row(legend, Swatch(new Color32(118, 116, 110, 255), 8, false), "Rock", TooltipKey.LegendRock);
            Row(legend, Swatch(new Color32(8, 9, 12, 255), 8, false), "Unseen fog", TooltipKey.LegendUnseen);
            Row(legend, Swatch(new Color32(235, 235, 240, 255), 8, false, hollow: true), "Camera view", TooltipKey.LegendCamera);
            return legend;
        }

        private static void Row(VisualElement parent, VisualElement mark, string text, TooltipKey tooltip)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, height = 22 } };
            var slot = new VisualElement { style = { width = 22, alignItems = Align.Center, justifyContent = Justify.Center } };
            slot.Add(mark);
            row.Add(slot);
            Label label = GameStyle.Label(text);
            label.style.marginLeft = 6;
            row.Add(label);
            string tip = TooltipText.Get(tooltip);
            label.AddTooltip(tip, text);
            mark.AddTooltip(tip, text);
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

        private static VisualElement IconImage(string[] icon, Color32 colour)
        {
            Texture2D tex = RegionMapView.IconTexture(icon, colour);
            return new VisualElement { style = { width = tex.width, height = tex.height, backgroundImage = Background.FromTexture2D(tex) } };
        }

        void IRegionEndListener.OnRegionEnded(RegionEnd end)
        {
            if (end != RegionEnd.Defeated) return;
            Close();
            enabled = false;
        }
    }
}
