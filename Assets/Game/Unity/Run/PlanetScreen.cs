using System.Linq;
using Blondie.Core;
using Blondie.GameSetup;
using UnityEngine;
using UnityEngine.UIElements;

namespace Isekai
{
    /// <summary>
    /// The Planet scene: the map of the Run, where the player picks the next Region (the edge to land on at first, then
    /// any Region touching a captured one). Starts a Run when there is none; after a won or lost Run it offers a new one.
    /// </summary>
    public sealed class PlanetScreen : MonoBehaviour
    {
        private const string PanelName = "Planet Screen";

        [SerializeField] private PanelSettings _panelSettings;

        private PlanetMapView _map;

        public PlanetMapView Map => _map;

        private void Start()
        {
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
            Build();
            return true;
        }

        private VisualElement AnchorPicker(PlanetRun run)
        {
            bool open = PickingOpen(run);
            if (open) RunSession.Anchor ??= AnchorKind.Shrine;
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 6 } };
            row.Add(GameStyle.Label(open ? "Anchor:" : "Anchor: " + (RunSession.Anchor is { } kept ? AnchorTuning.Name(kept) : "none"), TextRole.Muted));
            if (!open) return row;
            foreach (AnchorKind kind in new[] { AnchorKind.Shrine, AnchorKind.WarThrone })
            {
                AnchorKind picked = kind;
                var button = new Button(() => ChooseAnchor(picked))
                {
                    text = (RunSession.Anchor == kind ? "[x] " : "[ ] ") + AnchorTuning.Name(kind),
                    name = "Anchor " + AnchorTuning.Name(kind),
                };
                button.style.marginLeft = 6;
                row.Add(button);
            }
            string detail = RunSession.Anchor == AnchorKind.WarThrone
                ? "In plain sight, Vigilance starts at Uneasy, a strong aura for demons, faster corruption."
                : "Small and hidden until a patrol finds it; being found raises Vigilance a stage.";
            Label about = GameStyle.Label(detail, TextRole.Muted);
            about.style.marginLeft = 8;
            row.Add(about);
            return row;
        }

        private void Build()
        {
            VisualElement root = GamePanels.Mount(this, PanelName, _panelSettings, out PanelRenderer panel);
            root.Clear();
            PlanetRun run = RunSession.Run;
            RunSave.Save();   // the Planet screen is where a Run is saved: between Regions, never inside one
            var column = new VisualElement
            {
                pickingMode = PickingMode.Ignore,
                style = { position = Position.Absolute, left = 0, right = 0, top = 0, bottom = 0, alignItems = Align.Center, justifyContent = Justify.Center },
            };
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
            _map = new PlanetMapView(run, new Vector2(880f, 336f), region => RunSession.GoToRegion(region));
            column.Add(AnchorPicker(run));
            column.Add(_map);
            var buttons = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 8 } };
            if (run.IsOver)
            {
                buttons.Add(new Button(() => { RunSession.StartNew(); Build(); }) { text = "New Run" });
                if (GameSetup.IsRunning) buttons.Add(new Button(GameSetup.GoToMainMenu) { text = "Main Menu" });
            }
            column.Add(buttons);
            root.Add(column);
            UIPanels.Created(panel);
        }
    }
}
