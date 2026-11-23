using System.Collections.Generic;
using System.Linq;
using Blondie.Core;
using Blondie.GameSetup;
using Blondie.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Isekai
{
    /// <summary>
    /// The Planet scene. A New Run opens on the loadout (<see cref="RunSession.LoadoutPending"/>): the Anchor and the Soul
    /// paths, chosen before anything else. Then the map of the Run, where the player picks the next Region (the edge to
    /// land on at first, then any Region touching a captured one). Starts a Run when there is none; after a won or lost
    /// Run it offers a new one.
    /// </summary>
    public sealed class PlanetScreen : MonoBehaviour
    {
        private const string PanelName = "Planet Screen";

        [SerializeField] private PanelSettings _panelSettings;

        private PlanetMapView _map;
        private SoulPathPicks _picks;

        // The loadout's controls, built once and updated in place (a rebuild on every click flashed the screen).
        private readonly Dictionary<AnchorKind, IconButton> _anchorTiles = new();
        private readonly Dictionary<SoulPath, IconButton> _pathTiles = new();
        private Label _anchorAbout;
        private Label _pathCount;
        private Label[] _pathAbout;
        private IconButton _confirm;
        private VisualElement _loadoutPage, _mapPage, _mapHead, _mapSlot;
        private PlanetRun _shownRun;

        /// <summary>The Planet map (built even while the loadout shows, so the Run can be steered from code).</summary>
        public PlanetMapView Map => _map;

        /// <summary>The loadout (Anchor, Soul paths) is showing instead of the map.</summary>
        public bool ShowingLoadout => RunSession.LoadoutPending && RunSession.Run != null && !RunSession.Run.IsOver;

        /// <summary>The Soul paths picked so far on the loadout.</summary>
        public IReadOnlyList<SoulPath> PickedPaths => Picks.Picked;

        private SoulPathPicks Picks
        {
            get
            {
                if (_picks != null) return _picks;
                _picks = new SoulPathPicks();
                foreach (SoulPath path in RunSession.Paths) _picks.Toggle(path);
                return _picks;
            }
        }

        /// <summary>Picks or unpicks a Soul path on the loadout.</summary>
        public void TogglePath(SoulPath path)
        {
            Picks.Toggle(path);
            RefreshLoadout();
        }

        /// <summary>Ends the loadout and shows the Planet map; false until every Soul path slot is filled.</summary>
        public bool ConfirmLoadout()
        {
            if (!Picks.IsFull) return false;
            RunSession.Anchor ??= AnchorKind.Shrine;
            RunSession.SetPaths(Picks.Picked);
            RunSession.LoadoutPending = false;
            Build();
            return true;
        }

        private void Start()
        {
            RunSave.RestorePending();   // Continue: the saved Run comes back here, behind the fade
            if (RunSession.Run == null) RunSession.StartNew();
            Time.timeScale = 1f;
            Build();
        }

        // The Anchor is picked before landing on the first Region and fixed for the rest of the Run.
        private static bool PickingOpen(PlanetRun run) => !run.IsOver && run.CapturedInOrder.Count == 0;

        /// <summary>Picks the Run's Anchor; false once the first Region is captured (or the Run is over).</summary>
        public bool ChooseAnchor(AnchorKind kind)
        {
            if (!PickingOpen(RunSession.Run)) return false;
            RunSession.Anchor = kind;
            if (ShowingLoadout) RefreshLoadout();
            else Build();
            return true;
        }

        // On the map: what the loadout chose (fixed for the Run), as icons.
        private static VisualElement LoadoutSummary()
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            if (RunSession.Anchor is { } anchor) row.Add(Chosen(GameIcons.Of(anchor), AnchorTuning.Name(anchor), AnchorAbout(anchor)));
            foreach (SoulPath path in RunSession.Paths) row.Add(Chosen(GameIcons.Of(path), SoulPaths.Name(path), SoulPaths.About(path)));
            return row;
        }

        private static VisualElement Chosen(Sprite icon, string name, string about)
        {
            var slot = new VisualElement { style = { marginLeft = 4, marginRight = 4 } };
            if (icon != null) slot.Add(GameStyle.Icon(icon, 32));
            else slot.Add(GameStyle.Label(name));
            slot.AddTooltip(about, name);
            return slot;
        }

        private static VisualElement Stat(Sprite icon, string name, string value, string about)
        {
            VisualElement pair = GameStyle.IconNumber(icon, value, out _, name);
            pair.style.marginLeft = pair.style.marginRight = 10;
            pair.AddTooltip(about, name);
            return pair;
        }

        private static string AnchorAbout(AnchorKind kind) => kind == AnchorKind.WarThrone
            ? "In plain sight, Vigilance starts at Uneasy, a strong aura for demons, faster corruption."
            : "Small and hidden until a patrol finds it; being found raises Vigilance a stage.";

        // The loadout (docs/ui-rules.md): icon tiles with the name under each, the picked one framed, and the words for
        // what is picked shown under the tiles. Built once; clicks update it in place.
        private VisualElement Loadout()
        {
            _anchorTiles.Clear();
            _pathTiles.Clear();
            RunSession.Anchor ??= AnchorKind.Shrine;
            var panel = new VisualElement { style = { flexGrow = 1, alignSelf = Align.Stretch, alignItems = Align.Center, justifyContent = Justify.SpaceAround, paddingLeft = 24, paddingRight = 24, paddingTop = 24, paddingBottom = 24 } };
            panel.Add(GameStyle.Label("Prepare the invasion", TextRole.Title));

            panel.Add(Heading("Anchor", "Where Jack's soul is bound: he revives there, and Demon Kings and workers are summoned there."));
            var anchors = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            foreach (AnchorKind kind in new[] { AnchorKind.Shrine, AnchorKind.WarThrone })
            {
                AnchorKind k = kind;
                IconButton tile = Tile(GameIcons.Of(kind), AnchorTuning.Name(kind), () => ChooseAnchor(k));
                tile.name = "Anchor " + AnchorTuning.Name(kind);
                tile.Tooltip = AnchorAbout(kind);
                _anchorTiles[kind] = tile;
                anchors.Add(tile);
            }
            panel.Add(anchors);
            _anchorAbout = GameStyle.Label(string.Empty, TextRole.Muted);
            _anchorAbout.style.whiteSpace = WhiteSpace.Normal;
            _anchorAbout.style.unityTextAlign = TextAnchor.MiddleCenter;
            panel.Add(_anchorAbout);

            var pathsHead = Heading("Soul paths", "Each soul level gives a point in one of the two paths you pick.");
            _pathCount = GameStyle.Label(string.Empty, TextRole.Accent);
            _pathCount.style.marginLeft = 16;
            pathsHead.Add(_pathCount);
            panel.Add(pathsHead);
            var grid = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, justifyContent = Justify.Center } };
            foreach (SoulPath path in SoulPaths.Starting)
            {
                SoulPath p = path;
                IconButton tile = Tile(GameIcons.Of(path), SoulPaths.Name(path), () => TogglePath(p));
                tile.name = "Path " + SoulPaths.Name(path);
                tile.Tooltip = SoulPaths.About(path);
                _pathTiles[path] = tile;
                grid.Add(tile);
            }
            panel.Add(grid);
            _pathAbout = new Label[Picks.Slots];
            for (int i = 0; i < _pathAbout.Length; i++)
            {
                _pathAbout[i] = GameStyle.Label(" ", TextRole.Body);
                _pathAbout[i].style.whiteSpace = WhiteSpace.Normal;
                _pathAbout[i].style.unityTextAlign = TextAnchor.MiddleCenter;
                panel.Add(_pathAbout[i]);
            }

            _confirm = new IconButton(GameIcons.Named("Go"), "Go", () => ConfirmLoadout(), box: 48) { name = "Confirm loadout", Tooltip = "To the Planet: choose where to land." };
            panel.Add(_confirm);
            RefreshLoadout();
            return panel;
        }

        // A square tile: the icon large, its short name under it (the rules allow a name under an icon).
        private static IconButton Tile(Sprite icon, string name, System.Action clicked)
        {
            var tile = new IconButton(icon, name, clicked, box: 96, column: true);
            tile.AddToClassList("isekai-tile");
            if (tile.HasIcon) tile.Add(GameStyle.Label(name, TextRole.Muted));
            return tile;
        }

        private static VisualElement Heading(string title, string tooltip)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginTop = 10, marginBottom = 2 } };
            row.Add(GameStyle.Label(title, TextRole.Accent));
            row.AddTooltip(tooltip, title);
            return row;
        }

        // Puts the picks on the tiles, the words for what is picked under them, and the count; no rebuild.
        private void RefreshLoadout()
        {
            if (_confirm == null) return;
            foreach (KeyValuePair<AnchorKind, IconButton> tile in _anchorTiles) tile.Value.Picked = RunSession.Anchor == tile.Key;
            _anchorAbout.text = RunSession.Anchor is { } anchor ? AnchorAbout(anchor) : string.Empty;
            SoulPathPicks picks = Picks;
            foreach (KeyValuePair<SoulPath, IconButton> tile in _pathTiles) tile.Value.Picked = picks.Has(tile.Key);
            _pathCount.text = $"{picks.Picked.Count}/{picks.Slots}";
            for (int i = 0; i < _pathAbout.Length; i++)
                _pathAbout[i].text = i < picks.Picked.Count ? $"{SoulPaths.Name(picks.Picked[i])}: {SoulPaths.About(picks.Picked[i])}" : " ";
            _confirm.SetEnabled(picks.IsFull);
        }

        // Two pages, built once: the loadout and the map. Build shows the one that is due and refreshes it; only a new Run's
        // map is made anew.
        private void Build()
        {
            PlanetRun run = RunSession.Run;
            RunSave.Save();   // the Planet screen is where a Run is saved: between Regions, never inside one (on the loadout too, so Continue returns to it)
            if (_mapPage == null) BuildPages();
            if (_shownRun != run)
            {
                _shownRun = run;
                _map?.RemoveFromHierarchy();
                _map = new PlanetMapView(run, region => RunSession.GoToRegion(region));
                _mapSlot.Add(_map);
            }
            bool loadout = ShowingLoadout;
            if (loadout && _loadoutPage.childCount == 0) _loadoutPage.Add(Loadout());
            _loadoutPage.style.display = loadout ? DisplayStyle.Flex : DisplayStyle.None;
            _mapPage.style.display = loadout ? DisplayStyle.None : DisplayStyle.Flex;
            if (loadout)
            {
                RefreshLoadout();
                return;
            }
            if (PickingOpen(run)) RunSession.Anchor ??= AnchorKind.Shrine;   // a Run steered past the loadout still has an Anchor
            _map.Refresh();
            FillHead(run);
        }

        private void BuildPages()
        {
            VisualElement root = GamePanels.Mount(this, PanelName, _panelSettings, out PanelRenderer panel);
            _loadoutPage = Page(root);
            _mapPage = Page(root);
            _mapHead = new VisualElement { style = { alignItems = Align.Center, flexShrink = 0 } };
            _mapPage.Add(_mapHead);
            _mapSlot = new VisualElement { style = { flexGrow = 1, alignSelf = Align.Stretch } };
            _mapPage.Add(_mapSlot);
            UIPanels.Created(panel);
        }

        private static VisualElement Page(VisualElement root)
        {
            var page = new VisualElement { style = { position = Position.Absolute, left = 0, right = 0, top = 0, bottom = 0, paddingTop = 12, paddingBottom = 12, backgroundColor = GameStyle.Backdrop } };
            page.AddToClassList("isekai-menu");
            root.Add(page);
            return page;
        }

        // The few lines over the map: what to do, the Run in icons and numbers, what the loadout chose, and the ways on once
        // the Run is over.
        private void FillHead(PlanetRun run)
        {
            _mapHead.Clear();
            _mapHead.Add(GameStyle.Label(run.State switch
            {
                PlanetRunState.Won => "The Planet is yours!",
                PlanetRunState.Lost => "The Run is lost",
                _ => run.CapturedInOrder.Count == 0 ? "Choose where to land" : "Choose your next Region",
            }, TextRole.Title));
            var stats = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, flexWrap = Wrap.Wrap, justifyContent = Justify.Center } };
            stats.Add(Stat(GameIcons.Named("Time"), "Run time", RunSummary.FormatTime(run.RunSeconds), RunStatTips.Text(RunStat.Time)));
            stats.Add(Stat(GameIcons.Named("Captured"), "Regions captured", $"{run.CapturedInOrder.Count}", RunStatTips.Text(RunStat.Regions)));
            stats.Add(Stat(GameIcons.Of(ResourceType.Stone), "Bonus Stone", "+" + run.BonusStone, "Stone every Region starts with, from the Regions captured."));
            string buffs = string.Join("\n", run.Buffs.Select(RegionBuffs.Describe));
            if (buffs.Length > 0) stats.Add(Stat(GameIcons.Named("Level"), "Buffs", $"{run.Buffs.Count()}", buffs));
            stats.Add(LoadoutSummary());
            _mapHead.Add(stats);
            if (!run.IsOver) return;
            var buttons = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 8 } };
            buttons.Add(new IconButton(GameIcons.Named("NewRun"), "New Run", StartAnotherRun, box: 48));
            if (GameSetup.IsRunning) buttons.Add(new IconButton(GameIcons.Named("Menu"), "Main menu", GameSetup.GoToMainMenu, box: 48));
            _mapHead.Add(buttons);
        }

        private void StartAnotherRun()
        {
            RunSession.StartNew();
            RunSession.LoadoutPending = true;
            _picks = null;
            Build();
        }
    }
}
