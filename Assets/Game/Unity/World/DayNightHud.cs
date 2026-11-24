using Blondie.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Isekai
{
    /// <summary>The time of day at the top right: a sun or a moon and the time left until it changes.</summary>
    [RegionSystem]
    public sealed class DayNightHud : MonoBehaviour
    {
        private VisualElement _icon;
        private Label _name;
        private Label _left;
        private TooltipManipulator _tip;
        private string _key;

        /// <summary>"Day" or "Night" (the tooltip's header).</summary>
        public string PhaseText { get; private set; } = "Day";

        /// <summary>The countdown, in words.</summary>
        public string CountdownText { get; private set; } = string.Empty;

        private void Start()
        {
            var box = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            box.AddToClassList("isekai-panel");
            _icon = GameStyle.Icon(GameIcons.Named("Day"));
            _name = GameStyle.Label("Day", TextRole.Accent);
            _left = GameStyle.Number("00:00");
            box.Add(_icon);
            box.Add(_name);
            box.Add(_left);
            _tip = box.AddTooltip(TooltipText.Get(TooltipKey.HudDayNight), "Day");
            HudStack.Add(HudCorner.TopRight, box, 1);
        }

        private void Update()
        {
            using (PerfMarkers.Hud.Auto()) Tick();
        }

        private void Tick()
        {
            RegionDayNight dayNight = RegionDayNight.Current;
            if (dayNight == null || _left == null) return;

            bool night = dayNight.IsNight;
            int whole = Mathf.Max(0, Mathf.CeilToInt(dayNight.SecondsUntilChange));
            string key = (night ? "N" : "D") + whole;
            if (key == _key) return;
            _key = key;

            string time = $"{whole / 60:00}:{whole % 60:00}";
            PhaseText = night ? "Night" : "Day";
            CountdownText = $"Until {(night ? "day" : "night")}: {time}";
            Sprite icon = GameIcons.Named(PhaseText);
            GameStyle.SetIcon(_icon, icon);
            _name.text = PhaseText;
            _name.style.display = icon != null ? DisplayStyle.None : DisplayStyle.Flex;   // no art: the word
            _left.text = time;
            _tip.Header = PhaseText;
        }
    }
}
