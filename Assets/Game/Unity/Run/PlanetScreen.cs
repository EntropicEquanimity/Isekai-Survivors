using System.Linq;
using Blondie.Core;
using Blondie.GameSetup;
using Blondie.SkillTrees;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Isekai
{
    /// <summary>
    /// The Planet scene: the map of the Run, where the player picks the next Region (the edge to land on at first, then
    /// any Region touching a captured one). Starts a Run when there is none; after a won or lost Run it offers a new one.
    /// Before landing and after a Run, the Legacy button opens the meta upgrade tree (<see cref="MetaProgress"/>).
    /// </summary>
    public sealed class PlanetScreen : MonoBehaviour
    {
        private const string PanelName = "Planet Screen";
        private const string LegacyStyle = "Isekai/LegacyTree";
        private const float LegacyStep = 152f;   // UI pixels between Legacy nodes: room for their names

        [SerializeField] private PanelSettings _panelSettings;

        private PlanetMapView _map;
        private SkillTreeWindow _legacy;

        public PlanetMapView Map => _map;

        /// <summary>The Legacy tree window (made on first open).</summary>
        public SkillTreeWindow LegacyWindow => _legacy;

        /// <summary>Shards may be spent before landing and once the Run is over, never mid-Run.</summary>
        public static bool CanSpendShards(PlanetRun run) => run == null || run.IsOver || (run.CapturedInOrder.Count == 0 && run.Current == null);

        private void Start()
        {
            if (RunSession.Run == null) RunSession.StartNew();
            Time.timeScale = 1f;
            Build();
        }

        private void Build()
        {
            VisualElement root = GamePanels.Mount(this, PanelName, _panelSettings, out PanelRenderer panel);
            root.Clear();
            PlanetRun run = RunSession.Run;
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
            column.Add(new Label(title) { style = { fontSize = PlanetMapView.PixelFontSize * 2, color = Color.white, marginBottom = 4 } });
            int secs = Mathf.FloorToInt(run.RunSeconds);
            string buffs = string.Join(", ", run.Buffs.Select(b => RegionBuffs.Get(b)?.Name).Where(n => n != null));
            column.Add(new Label($"Run time {secs / 60}:{secs % 60:00}   Captured {run.CapturedInOrder.Count}   Bonus Stone {run.BonusStone}" +
                                 (buffs.Length > 0 ? $"   Buffs: {buffs}" : ""))
                { style = { fontSize = PlanetMapView.PixelFontSize, color = new Color(0.85f, 0.85f, 0.9f), marginBottom = 6 } });
            string earned = run.IsOver && MetaProgress.LastEarned > 0 ? $"   (+{MetaProgress.LastEarned} from this Run)" : "";
            column.Add(new Label($"Shards {MetaProgress.Shards}{earned}")
                { style = { fontSize = PlanetMapView.PixelFontSize, color = new Color(0.7f, 0.85f, 1f), marginBottom = 6 } });
            _map = new PlanetMapView(run, new Vector2(880f, 360f), region => RunSession.GoToRegion(region));
            column.Add(_map);
            var buttons = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 8 } };
            if (CanSpendShards(run)) buttons.Add(new Button(OpenLegacy) { text = "Legacy upgrades" });
            if (run.IsOver)
            {
                buttons.Add(new Button(() => { RunSession.StartNew(); Build(); }) { text = "New Run" });
                if (GameSetup.IsRunning) buttons.Add(new Button(GameSetup.GoToMainMenu) { text = "Main Menu" });
            }
            column.Add(buttons);
            root.Add(column);
            UIPanels.Created(panel);
        }

        /// <summary>Opens the Legacy tree, where Shards buy upgrades for every later Run.</summary>
        public void OpenLegacy()
        {
            if (_legacy == null)
            {
                // Made inactive so the window's Awake finds its panel (with the game's settings) and holder already set.
                var go = new GameObject("Legacy Window");
                go.SetActive(false);
                go.transform.SetParent(transform, false);
                PanelRenderer panel = UIPanels.FindOrCreateRenderer(go.transform, SkillTreeWindow.PanelObjectName);
                if (_panelSettings != null) UIPanels.SetPanelSettings(panel, _panelSettings);
                _legacy = go.AddComponent<SkillTreeWindow>();
                _legacy.Holder = MetaProgress.Holder;
                _legacy.ToggleKey = Key.None;
                _legacy.Closed += Build;   // show the Shards left
                GameText.Install();
                go.SetActive(true);
                var style = Resources.Load<StyleSheet>(LegacyStyle);
                if (style != null) _legacy.Root.styleSheets.Add(style);
                _legacy.Presenter.LayoutScale = LegacyStep;
                _legacy.ShowTree(MetaProgress.Tree);
            }
            _legacy.Open();
        }
    }
}
