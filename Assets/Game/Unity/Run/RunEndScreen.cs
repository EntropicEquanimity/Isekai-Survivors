using System.Collections.Generic;
using Blondie.Core;
using Blondie.GameSetup;
using Blondie.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Isekai
{
    /// <summary>
    /// The victory and defeat screens: a Run summary (Regions captured, time, deaths, Souls, rulers slain or escaped,
    /// each explained on hover) over a dimmed Region, with New Run (a new Run starts straight away) and Main menu. The
    /// game stops while it shows. <see cref="RegionRun"/> opens it when the Capital falls or the Run is lost.
    /// </summary>
    [RegionSystem]
    public sealed class RunEndScreen : MonoBehaviour
    {
        private const string PanelName = "Run End Screen";
        private const float SortingOrder = 120f;

        private static readonly Color WonColour = GameStyle.Warning;
        private static readonly Color LostColour = new(0.92f, 0.32f, 0.26f);

        private VisualElement _overlay;
        private Label _title;
        private Label _subtitle;
        private VisualElement _rows;
        private VisualElement _buttons;
        private readonly Dictionary<RunStat, VisualElement> _rowOf = new();
        private bool _paused;

        public static RunEndScreen Current { get; private set; }

        public bool IsVisible => _overlay != null && _overlay.style.display == DisplayStyle.Flex;

        /// <summary>The summary on show (null until the Run ends).</summary>
        public RunSummary Summary { get; private set; }

        public string TitleText => _title != null ? _title.text : string.Empty;
        public string SubtitleText => _subtitle != null ? _subtitle.text : string.Empty;

        /// <summary>The text of a figure's row ("Deaths 2"), or null when it is not shown.</summary>
        public string RowText(RunStat stat)
        {
            if (!_rowOf.TryGetValue(stat, out VisualElement row)) return null;
            return row.viewDataKey;
        }

        /// <summary>Whether a figure's row explains itself on hover.</summary>
        public bool HasTooltip(RunStat stat) => _rowOf.TryGetValue(stat, out VisualElement row) && row.userData is TooltipManipulator;

        /// <summary>The buttons' labels, in order.</summary>
        public IReadOnlyList<string> ButtonTexts
        {
            get
            {
                var texts = new List<string>();
                if (_buttons != null)
                    foreach (VisualElement b in _buttons.Children())
                        if (b is IconButton button) texts.Add(button.Name);
                return texts;
            }
        }

        private void OnEnable() => Current = this;

        private void OnDisable()
        {
            if (Current == this) Current = null;
        }

        private void OnDestroy()
        {
            if (_paused) GamePause.Resume(this);
        }

        private void Start()
        {
            VisualElement root = GamePanels.Mount(this, PanelName, null, out PanelRenderer panel, SortingOrder);
            _overlay = new VisualElement
            {
                pickingMode = PickingMode.Position,   // nothing behind it takes a click
                style =
                {
                    position = Position.Absolute, left = 0, right = 0, top = 0, bottom = 0,
                    alignItems = Align.Center, justifyContent = Justify.Center,
                    backgroundColor = new Color(0.03f, 0.01f, 0.015f, 0.94f),
                    display = DisplayStyle.None,
                },
            };
            _overlay.AddToClassList("isekai-menu");   // it fills the screen
            var box = new VisualElement { style = { alignItems = Align.Center } };
            _title = GameStyle.Label(string.Empty, TextRole.Title);
            _title.style.unityTextAlign = TextAnchor.MiddleCenter;
            _subtitle = GameStyle.Label(string.Empty, TextRole.Muted);
            _subtitle.style.unityTextAlign = TextAnchor.MiddleCenter;
            _subtitle.style.marginBottom = 12;
            _rows = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, justifyContent = Justify.Center, maxWidth = 1100 } };
            _buttons = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.Center, marginTop = 16 } };
            box.Add(_title);
            box.Add(_subtitle);
            box.Add(_rows);
            box.Add(_buttons);
            _overlay.Add(box);
            root.Add(_overlay);
            UIPanels.Created(panel);
            if (Summary != null) Draw();
        }

        /// <summary>Shows the screen for a Run that has ended and stops the game behind it.</summary>
        public void Show(RunSummary summary)
        {
            Summary = summary;
            if (!_paused)
            {
                _paused = true;
                GamePause.Pause(this);
            }
            if (_overlay != null) Draw();   // otherwise Start draws it
        }

        /// <summary>New Run: a new Run on a new Planet starts straight away, on its loadout.</summary>
        public void NewRun()
        {
            RunSession.StartNew();
            RunSession.LoadoutPending = true;   // every New Run chooses its Anchor and Soul paths first
            RunSession.GoToPlanet();
        }

        /// <summary>Back to the main menu (the Run is over and its save is gone).</summary>
        public void ToMainMenu()
        {
            if (GameSetup.IsRunning) GameSetup.GoToMainMenu();
            else
            {
                RunSession.Clear();
                RunSession.GoToPlanet();   // no main menu outside the full game: the Planet scene starts a Run of its own
            }
        }

        private void Draw()
        {
            RunSummary s = Summary;
            _title.text = s.Title;
            _title.style.color = s.Won ? WonColour : LostColour;
            _subtitle.text = s.Subtitle;
            _rows.Clear();
            _rowOf.Clear();
            foreach (RunSummaryRow row in s.Rows)
            {
                // An icon and a number; what it is, and what it means, on hover.
                VisualElement line = GameStyle.IconNumber(IconOf(row.Stat), row.Value, out Label value, row.Label);
                line.style.marginLeft = line.style.marginRight = 16;
                line.style.marginTop = line.style.marginBottom = 8;
                if (line[0] != value && IconOf(row.Stat) != null) GameStyle.SetIcon(line[0], IconOf(row.Stat), 32);
                line.viewDataKey = row.Label + " " + row.Value;
                line.userData = line.AddTooltip(RunStatTips.Text(row.Stat), RunStatTips.Header(row.Stat));
                _rows.Add(line);
                _rowOf[row.Stat] = line;
            }
            _buttons.Clear();
            _buttons.Add(new IconButton(GameIcons.Named("NewRun"), "New Run", NewRun, box: 48));
            _buttons.Add(new IconButton(GameIcons.Named("Menu"), "Main menu", ToMainMenu, box: 48));
            _overlay.style.display = DisplayStyle.Flex;
        }

        private static Sprite IconOf(RunStat stat) => stat switch
        {
            RunStat.Regions => GameIcons.Named("Captured"),
            RunStat.Time => GameIcons.Named("Time"),
            RunStat.Deaths => GameIcons.Named("Death"),
            RunStat.Souls => GameIcons.Of(ResourceType.Souls),
            RunStat.SoulLevel => GameIcons.Named("SoulLevel"),
            RunStat.RulersSlain => GameIcons.Named("RulerSlain"),
            _ => GameIcons.Named("RulerEscaped"),
        };
    }
}
