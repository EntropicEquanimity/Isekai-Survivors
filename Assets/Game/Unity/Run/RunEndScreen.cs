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
        private static readonly Color ButtonColour = new(78f / 255f, 18f / 255f, 20f / 255f);
        private static readonly Color ButtonHoverColour = new(122f / 255f, 28f / 255f, 26f / 255f);

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
            return string.Join(" ", ((Label)row[0]).text, ((Label)row[1]).text);
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
                        if (b is UnityEngine.UIElements.Button button) texts.Add(button.text);
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
                    backgroundColor = new Color(0.03f, 0.01f, 0.015f, 0.86f),
                    display = DisplayStyle.None,
                },
            };
            var box = new VisualElement
            {
                style =
                {
                    minWidth = 420, paddingTop = 16, paddingBottom = 16, paddingLeft = 24, paddingRight = 24,
                    backgroundColor = new Color(18f / 255f, 7f / 255f, 9f / 255f, 1f),
                    alignItems = Align.Stretch,
                },
            };
            SetBorder(box, GameStyle.Border, 2);
            _title = GameStyle.Label(string.Empty, TextRole.Title);
            _title.style.unityTextAlign = TextAnchor.MiddleCenter;
            _subtitle = GameStyle.Label(string.Empty, TextRole.Muted);
            _subtitle.style.unityTextAlign = TextAnchor.MiddleCenter;
            _subtitle.style.marginBottom = 12;
            _rows = new VisualElement();
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

        /// <summary>New Run: a new Run on a new Planet starts straight away, on its map.</summary>
        public void NewRun()
        {
            RunSession.StartNew();
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
                var line = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, paddingTop = 2, paddingBottom = 2 } };
                Label label = GameStyle.Label(row.Label, TextRole.Muted);
                Label value = GameStyle.Label(row.Value, TextRole.Body);
                value.style.marginLeft = 24;
                line.Add(label);
                line.Add(value);
                line.userData = line.AddTooltip(RunStatTips.Text(row.Stat), RunStatTips.Header(row.Stat));
                _rows.Add(line);
                _rowOf[row.Stat] = line;
            }
            _buttons.Clear();
            _buttons.Add(MenuStyled("New Run", NewRun));
            _buttons.Add(MenuStyled("Main menu", ToMainMenu));
            _overlay.style.display = DisplayStyle.Flex;
        }

        // A button in the menus' colours (IsekaiMenus.uss: dark red, brighter on hover).
        private static UnityEngine.UIElements.Button MenuStyled(string text, System.Action onClick)
        {
            var button = new UnityEngine.UIElements.Button(onClick) { text = text, name = text };
            button.style.backgroundColor = ButtonColour;
            button.style.color = new Color(238f / 255f, 224f / 255f, 214f / 255f);
            button.style.fontSize = GameStyle.FontSize;
            button.style.marginLeft = 6;
            button.style.marginRight = 6;
            button.style.paddingLeft = 14;
            button.style.paddingRight = 14;
            button.style.paddingTop = 6;
            button.style.paddingBottom = 6;
            SetBorder(button, GameStyle.Border, 1);
            button.RegisterCallback<PointerEnterEvent>(_ => button.style.backgroundColor = ButtonHoverColour);
            button.RegisterCallback<PointerLeaveEvent>(_ => button.style.backgroundColor = ButtonColour);
            return button;
        }

        private static void SetBorder(VisualElement e, Color colour, float width)
        {
            e.style.borderTopColor = e.style.borderBottomColor = e.style.borderLeftColor = e.style.borderRightColor = colour;
            e.style.borderTopWidth = e.style.borderBottomWidth = e.style.borderLeftWidth = e.style.borderRightWidth = width;
        }
    }
}
