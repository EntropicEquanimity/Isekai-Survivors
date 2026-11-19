using Blondie.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Isekai
{
    /// <summary>A small day-night clock: phase and time left until the next change.</summary>
    [RegionSystem]
    public sealed class DayNightHud : MonoBehaviour
    {
        private const string PanelName = "Day Night HUD";

        private Label _phase;
        private Label _countdown;
        private string _key;

        public string PhaseText => _phase != null ? _phase.text : string.Empty;
        public string CountdownText => _countdown != null ? _countdown.text : string.Empty;

        private void Start()
        {
            VisualElement root = GamePanels.Mount(this, PanelName, null, out PanelRenderer panel);
            var box = new VisualElement
            {
                pickingMode = PickingMode.Ignore,
                style =
                {
                    position = Position.Absolute,
                    right = 6,
                    top = 60,
                    minWidth = 138,
                    backgroundColor = new Color(0f, 0f, 0f, 0.62f),
                    paddingBottom = 4,
                    paddingLeft = 5,
                    paddingRight = 5,
                    paddingTop = 4,
                },
            };
            SetBorder(box.style);

            _phase = GameStyle.Label("Day", TextRole.Accent);
            _phase.pickingMode = PickingMode.Ignore;
            box.Add(_phase);

            _countdown = GameStyle.Label("Until night: 00:00", TextRole.Muted);
            _countdown.pickingMode = PickingMode.Ignore;
            box.Add(_countdown);

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
            RegionDayNight dayNight = RegionDayNight.Current;
            if (dayNight == null || _phase == null) return;

            bool night = dayNight.IsNight;
            int whole = Mathf.Max(0, Mathf.CeilToInt(dayNight.SecondsUntilChange));
            string key = (night ? "N" : "D") + whole;
            if (key == _key) return;
            _key = key;

            int minutes = whole / 60;
            int seconds = whole % 60;
            _phase.text = night ? "Night" : "Day";
            _countdown.text = $"Until {(night ? "day" : "night")}: {minutes:00}:{seconds:00}";
        }
    }
}
