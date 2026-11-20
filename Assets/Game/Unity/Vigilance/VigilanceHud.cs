using Blondie.Core;
using Blondie.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Isekai
{
    /// <summary>
    /// The Vigilance readout at the top right: its icon, the stage's name and a bar of progress to the next stage.
    /// Phase 9 replaces it with the real HUD.
    /// </summary>
    [RegionSystem]
    public sealed class VigilanceHud : MonoBehaviour
    {
        private const string PanelName = "Vigilance HUD";
        private const string IconName = "Icon_Vigilance";

        private Label _stage;
        private Label _next;
        private VisualElement _fill;
        private string _key;

        /// <summary>The stage name shown.</summary>
        public string StageText => _stage != null ? _stage.text : string.Empty;

        /// <summary>The progress line shown ("12 / 25", or empty at Crusade).</summary>
        public string NextText => _next != null ? _next.text : string.Empty;

        /// <summary>How full the bar is, 0..1.</summary>
        public float Fill => _fill != null && _fill.style.width.value.unit == LengthUnit.Percent ? _fill.style.width.value.value / 100f : 0f;

        private void Start()
        {
            VisualElement root = GamePanels.Mount(this, PanelName, null, out PanelRenderer panel);
            var box = new VisualElement
            {
                style =
                {
                    position = Position.Absolute,
                    right = 6,
                    top = 6,
                    minWidth = 150,
                    backgroundColor = new Color(0f, 0f, 0f, 0.62f),
                    paddingBottom = 4,
                    paddingLeft = 5,
                    paddingRight = 5,
                    paddingTop = 4,
                },
            };
            SetBorder(box.style);

            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            var icon = new VisualElement { style = { width = 18, height = 18, marginRight = 5 } };
            Texture2D texture = Resources.Load<Texture2D>(IconName);
            if (texture != null) icon.style.backgroundImage = Background.FromTexture2D(texture);
            row.Add(icon);
            _stage = GameStyle.Label("-", TextRole.Warning);
            row.Add(_stage);
            box.Add(row);

            var bar = new VisualElement { style = { height = 5, marginTop = 3, backgroundColor = new Color(0.12f, 0.12f, 0.14f, 1f) } };
            _fill = new VisualElement { style = { height = 5, width = Length.Percent(0), backgroundColor = GameStyle.Warning } };
            bar.Add(_fill);
            box.Add(bar);

            _next = GameStyle.Label(string.Empty, TextRole.Muted);
            box.Add(_next);
            box.AddTooltip(TooltipText.Get(TooltipKey.HudVigilance));

            root.Add(box);
            UIPanels.Created(panel);
        }

        private static void SetBorder(IStyle style)
        {
            style.borderTopWidth = style.borderBottomWidth = style.borderLeftWidth = style.borderRightWidth = 1;
            style.borderTopColor = style.borderBottomColor = style.borderLeftColor = style.borderRightColor = GameStyle.Border;
        }

        private void Update()
        {
            using (PerfMarkers.Hud.Auto()) Tick();
        }

        private void Tick()
        {
            VigilanceDirector director = VigilanceDirector.Current;
            if (director == null || _stage == null) return;
            VigilanceStage stage = director.Stage;
            int points = Mathf.FloorToInt(director.Points);
            string key = stage + "/" + points;
            if (key == _key) return;
            _key = key;

            _stage.text = VigilanceStageNames.Name(stage);
            _fill.style.width = Length.Percent(director.ProgressToNext * 100f);
            _next.text = stage == VigilanceStage.Crusade ? string.Empty : $"{points} / {VigilanceTuning.Threshold(stage + 1):0}";
        }
    }
}
