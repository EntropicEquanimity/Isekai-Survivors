using System;
using Blondie.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Isekai
{
    /// <summary>
    /// The Vigilance readout at the top right: its eye, one pip per stage (lit up to the stage now), a bar of progress to the
    /// next stage and the points as numbers. The stage's name is in the tooltip.
    /// </summary>
    [RegionSystem]
    public sealed class VigilanceHud : MonoBehaviour
    {
        private const string IconName = "Icon_Vigilance";

        private VisualElement[] _pips;
        private Label _next;
        private VisualElement _fill;
        private TooltipManipulator _tip;
        private string _key;

        /// <summary>The stage's name (the tooltip's header).</summary>
        public string StageText { get; private set; } = string.Empty;

        /// <summary>The progress line shown ("12 / 25", or empty at Crusade).</summary>
        public string NextText => _next != null ? _next.text : string.Empty;

        /// <summary>How full the bar is, 0..1.</summary>
        public float Fill => _fill != null && _fill.style.width.value.unit == LengthUnit.Percent ? _fill.style.width.value.value / 100f : 0f;

        private void Start()
        {
            var box = new VisualElement { style = { minWidth = 120 } };
            box.AddToClassList("isekai-panel");

            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            var icon = new VisualElement { style = { width = 16, height = 16, marginRight = 5 } };
            Texture2D texture = Resources.Load<Texture2D>(IconName);
            if (texture != null) icon.style.backgroundImage = Background.FromTexture2D(texture);
            row.Add(icon);
            _pips = new VisualElement[Enum.GetValues(typeof(VigilanceStage)).Length];
            for (int i = 0; i < _pips.Length; i++)
            {
                _pips[i] = new VisualElement { pickingMode = PickingMode.Ignore };
                _pips[i].AddToClassList("isekai-pip");
                row.Add(_pips[i]);
            }
            box.Add(row);

            var bar = new VisualElement { style = { height = 5, marginTop = 3, backgroundColor = new Color(0.12f, 0.12f, 0.14f, 1f) } };
            _fill = new VisualElement { style = { height = 5, width = Length.Percent(0), backgroundColor = GameStyle.Warning } };
            bar.Add(_fill);
            box.Add(bar);

            _next = GameStyle.Number(string.Empty);
            box.Add(_next);
            _tip = box.AddTooltip(TooltipText.Get(TooltipKey.HudVigilance), "Vigilance");
            HudStack.Add(HudCorner.TopRight, box, 0);
        }

        private void Update()
        {
            using (PerfMarkers.Hud.Auto()) Tick();
        }

        private void Tick()
        {
            VigilanceDirector director = VigilanceDirector.Current;
            if (director == null || _pips == null) return;
            VigilanceStage stage = director.Stage;
            int points = Mathf.FloorToInt(director.Points);
            string key = stage + "/" + points;
            if (key == _key) return;
            _key = key;

            StageText = VigilanceStageNames.Name(stage);
            _tip.Header = "Vigilance: " + StageText;
            for (int i = 0; i < _pips.Length; i++) _pips[i].EnableInClassList("isekai-pip--on", i <= (int)stage);
            _fill.style.width = Length.Percent(director.ProgressToNext * 100f);
            _next.text = stage == VigilanceStage.Crusade ? string.Empty : $"{points} / {VigilanceTuning.Threshold(stage + 1):0}";
        }
    }
}
