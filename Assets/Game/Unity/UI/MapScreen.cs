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
    /// The map screen over the Region, like a strategy game's map view. M opens it on the Region tab (zooming out never
    /// does: the wheel only zooms) (<see cref="RegionMapView"/>, redrawn a few times a second); the Planet tab shows the
    /// Run's Planet (read only). Scrolling in, M or Escape returns to the Region. The game keeps running underneath.
    /// </summary>
    public sealed class MapScreen : MonoBehaviour, IRegionEndListener
    {
        public enum Tab { Region, Planet }

        private const string PanelName = "Map Screen";
        private const string MenuName = "MapScreen";
        private const float RedrawSeconds = 0.12f;

        [SerializeField] private Key _toggleKey = Key.M;

        private ScrollZoom _zoom;
        private VisualElement _shade;
        private VisualElement _content;
        private Label _title;
        private IconButton _regionTab, _planetTab;
        private VisualElement _regionPage, _planetPage;
        private PlanetMapView _planetMap;
        private PlanetRun _planetMapRun;
        private RegionMapView _regionMap;
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
                _zoom.InWhileBlocked += Close;
            }
            VisualElement root = GamePanels.Mount(this, PanelName, null, out PanelRenderer panel, 90f);
            _shade = new VisualElement
            {
                style =
                {
                    position = Position.Absolute, left = 0, right = 0, top = 0, bottom = 0, display = DisplayStyle.None,
                    backgroundColor = GameStyle.Backdrop, alignItems = Align.Center, paddingTop = 12, paddingBottom = 12, paddingLeft = 16, paddingRight = 16,
                },
            };
            var header = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 6 } };
            _title = GameStyle.Label("", TextRole.Title);
            _title.style.marginRight = 16;
            _regionTab = new IconButton(GameIcons.Named("Region"), "Region", () => Show(Tab.Region));
            _planetTab = new IconButton(GameIcons.Named("Planet"), "Planet", () => Show(Tab.Planet));
            _planetTab.style.marginLeft = 4;
            header.Add(_title);
            header.Add(_regionTab);
            header.Add(_planetTab);
            _content = new VisualElement { style = { flexGrow = 1, alignSelf = Align.Stretch } };
            _regionPage = new VisualElement { style = { flexGrow = 1, alignItems = Align.Center, justifyContent = Justify.Center } };
            _planetPage = new VisualElement { style = { flexGrow = 1 } };
            _content.Add(_regionPage);
            _content.Add(_planetPage);
            _regionPage.RegisterCallback<GeometryChangedEvent>(_ => FitRegion());
            _shade.AddToClassList("isekai-menu");
            _shade.Add(header);
            _shade.Add(_content);
            root.Add(_shade);
            UIPanels.Created(panel);
        }

        private void OnDestroy()
        {
            _regionMap?.Dispose();
            if (_zoom == null) return;
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
            // Both pages stay built; a tab only shows one of them (a rebuild on a click flashes).
            _regionPage.style.display = tab == Tab.Region ? DisplayStyle.Flex : DisplayStyle.None;
            _planetPage.style.display = tab == Tab.Planet ? DisplayStyle.Flex : DisplayStyle.None;
            _regionTab.Picked = tab == Tab.Region;
            _planetTab.Picked = tab == Tab.Planet;
            _planetTab.SetEnabled(RunSession.Run != null);
            if (tab == Tab.Planet)
            {
                _title.text = "The Planet";
                if (RunSession.Run == null) return;
                if (_planetMap == null || _planetMapRun != RunSession.Run)
                {
                    _planetMap?.RemoveFromHierarchy();
                    _planetMapRun = RunSession.Run;
                    _planetMap = new PlanetMapView(RunSession.Run, null);
                    _planetPage.Add(_planetMap);
                }
                else _planetMap.Refresh();
                return;
            }
            _title.text = RunSession.Run?.Current?.Name ?? "This Region";
            if (_regionMap == null && HordeDirector.Current != null && HordeDirector.Current.Grid.IsCreated)
            {
                _regionMap = new RegionMapView(HordeDirector.Current.Grid);
                _regionPage.Add(_regionMap);
                _regionPage.Add(Legend());
                FitRegion();
            }
            if (_regionMap == null) return;
            _regionMap.Redraw();
            _redrawAt = Time.unscaledTime + RedrawSeconds;
        }

        // The legend strip under the map keeps 48 px of the page.
        private void FitRegion()
        {
            if (_regionMap == null || float.IsNaN(_regionPage.contentRect.width) || _regionPage.contentRect.width < 1f) return;
            _regionMap.Fit(new Vector2(_regionPage.contentRect.width, _regionPage.contentRect.height - 48f));
        }

        private static VisualElement Legend()
        {
            // Marks only, in one strip: what each is comes up on hover.
            var legend = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, justifyContent = Justify.Center, height = 48, alignItems = Align.Center } };
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
            var slot = new VisualElement { style = { width = 32, height = 32, alignItems = Align.Center, justifyContent = Justify.Center } };
            slot.Add(mark);
            slot.AddTooltip(TooltipText.Get(tooltip), text);
            parent.Add(slot);
        }

        private static VisualElement Swatch(Color32 colour, int size, bool outlined, bool hollow = false)
        {
            size *= 2;
            var box = new VisualElement { pickingMode = PickingMode.Ignore, style = { width = size, height = size } };
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
            return new VisualElement { pickingMode = PickingMode.Ignore, style = { width = tex.width, height = tex.height, backgroundImage = Background.FromTexture2D(tex) } };
        }

        private static VisualElement IconImage(string[] icon, Color32 colour)
        {
            Texture2D tex = RegionMapView.IconTexture(icon, colour);
            return new VisualElement { pickingMode = PickingMode.Ignore, style = { width = tex.width, height = tex.height, backgroundImage = Background.FromTexture2D(tex) } };
        }

        void IRegionEndListener.OnRegionEnded(RegionEnd end)
        {
            if (end != RegionEnd.Defeated) return;
            Close();
            enabled = false;
        }
    }
}
