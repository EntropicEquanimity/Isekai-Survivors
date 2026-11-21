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
        private bool _announced;

        // The loadout's controls, built once and updated in place (a rebuild on every click flashed the screen).
        private readonly Dictionary<AnchorKind, IconButton> _anchorTiles = new();
        private readonly Dictionary<SoulPath, IconButton> _pathTiles = new();
        private Label _anchorAbout;
        private Label _pathCount;
        private VisualElement _pathAbout;
        private Button _confirm;

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

        // On the map: what the loadout chose (fixed for the Run).
        private static VisualElement LoadoutSummary()
        {
            string anchor = RunSession.Anchor is { } kept ? AnchorTuning.Name(kept) : "none";
            string paths = RunSession.Paths.Count > 0 ? string.Join(", ", RunSession.Paths.Select(SoulPaths.Name)) : "none";
            Label line = GameStyle.Label($"Anchor: {anchor}   Soul paths: {paths}", TextRole.Muted);
            line.style.marginBottom = 6;
            return line;
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
            var panel = new VisualElement { style = { alignItems = Align.Center, maxWidth = new Length(90, LengthUnit.Percent) } };
            panel.AddToClassList("isekai-menu");
            panel.Add(GameStyle.Label("Prepare the invasion", TextRole.Title));

            panel.Add(Heading("Anchor", "Where Jack's soul is bound: he revives there, and Demon Kings and workers are summoned there."));
            var anchors = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            foreach (AnchorKind kind in new[] { AnchorKind.Shrine, AnchorKind.WarThrone })
            {
                AnchorKind k = kind;
                IconButton tile = Tile(GameIcons.Of(AnchorCatalog.Asset(kind)), AnchorTuning.Name(kind), () => ChooseAnchor(k));
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
            _pathCount.style.marginLeft = 6;
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
            _pathAbout = new VisualElement { style = { alignItems = Align.Center, minHeight = 30 } };
            panel.Add(_pathAbout);

            _confirm = new Button(() => ConfirmLoadout()) { text = "Go", name = "Confirm loadout", style = { marginTop = 8, minWidth = 120 } };
            _confirm.AddTooltip("To the Planet: choose where to land.");
            panel.Add(_confirm);
            RefreshLoadout();
            return panel;
        }

        // A square tile: the icon large, its short name under it (the rules allow a name under an icon).
        private static IconButton Tile(Sprite icon, string name, System.Action clicked)
        {
            var tile = new IconButton(icon, name, clicked, iconSize: "huge", column: true) { style = { width = 96, marginLeft = 3, marginRight = 3, marginTop = 3, marginBottom = 3 } };
            if (icon != null) tile.Add(GameStyle.Label(name, TextRole.Muted));
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
            _pathAbout.Clear();
            foreach (SoulPath path in picks.Picked)
            {
                Label about = GameStyle.Label($"{SoulPaths.Name(path)}: {SoulPaths.About(path)}", TextRole.Body);
                about.style.whiteSpace = WhiteSpace.Normal;
                about.style.unityTextAlign = TextAnchor.MiddleCenter;
                _pathAbout.Add(about);
            }
            _confirm.SetEnabled(picks.IsFull);
        }

        private void Build()
        {
            VisualElement root = GamePanels.Mount(this, PanelName, _panelSettings, out PanelRenderer panel);
            root.Clear();
            PlanetRun run = RunSession.Run;
            RunSave.Save();   // the Planet screen is where a Run is saved: between Regions, never inside one (on the loadout too, so Continue returns to it)
            var column = new VisualElement
            {
                pickingMode = PickingMode.Ignore,
                style = { position = Position.Absolute, left = 0, right = 0, top = 0, bottom = 0, alignItems = Align.Center, justifyContent = Justify.Center },
            };
            _map = new PlanetMapView(run, new Vector2(880f, 336f), region => RunSession.GoToRegion(region));
            _confirm = null;
            if (ShowingLoadout)
            {
                column.Add(Loadout());
                root.Add(column);
                Announce(panel);
                return;
            }
            string title = run.State switch
            {
                PlanetRunState.Won => "The Planet is yours!",
                PlanetRunState.Lost => "The Run is lost",
                _ => run.CapturedInOrder.Count == 0 ? "Choose where to land" : "Choose your next Region",
            };
            Label heading = GameStyle.Label(title, TextRole.Title);
            heading.style.marginBottom = 4;
            column.Add(heading);
            int secs = Mathf.FloorToInt(run.RunSeconds);
            string buffs = string.Join(", ", run.Buffs.Select(b => RegionBuffs.Get(b)?.Name).Where(n => n != null));
            Label stats = GameStyle.Label($"Run time {secs / 60}:{secs % 60:00}   Captured {run.CapturedInOrder.Count}   Bonus Stone {run.BonusStone}" +
                                          (buffs.Length > 0 ? $"   Buffs: {buffs}" : ""), TextRole.Muted);
            stats.style.marginBottom = 6;
            column.Add(stats);
            if (PickingOpen(run)) RunSession.Anchor ??= AnchorKind.Shrine;   // a Run steered past the loadout still has an Anchor
            column.Add(LoadoutSummary());
            column.Add(_map);
            var buttons = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 8 } };
            if (run.IsOver)
            {
                buttons.Add(new Button(() => { RunSession.StartNew(); RunSession.LoadoutPending = true; _picks = null; Build(); }) { text = "New Run" });
                if (GameSetup.IsRunning) buttons.Add(new Button(GameSetup.GoToMainMenu) { text = "Main Menu" });
            }
            column.Add(buttons);
            root.Add(column);
            Announce(panel);
        }

        // Announced once: announcing again makes Pixel Perfect swap the panel's settings, which rebuilds it (a flash).
        private void Announce(PanelRenderer panel)
        {
            if (_announced) return;
            _announced = true;
            UIPanels.Created(panel);
        }
    }
}
