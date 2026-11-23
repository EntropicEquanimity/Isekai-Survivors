using System.Collections.Generic;
using System.Linq;
using Blondie.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Isekai
{
    /// <summary>
    /// A small capture panel for the Seat's ruler: slain, or escaped to another Region with the Traits the player has
    /// now learned.
    /// </summary>
    [RegionSystem]
    public sealed class RulerCaptureSummaryPanel : MonoBehaviour
    {
        private VisualElement _box;
        private Label _title;
        private VisualElement _icon;
        private TooltipManipulator _tip;

        public static RulerCaptureSummaryPanel Current { get; private set; }
        public string TitleText => _title != null ? _title.text : string.Empty;
        /// <summary>The rest of the story (the tooltip).</summary>
        public string DetailText => _tip != null ? _tip.Content : string.Empty;
        public bool IsVisible => _box != null && _box.style.display == DisplayStyle.Flex;

        private void OnEnable() => Current = this;

        private void OnDisable()
        {
            if (Current == this) Current = null;
        }

        private void Start()
        {
            _box = new VisualElement { style = { display = DisplayStyle.None, flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            _box.AddToClassList("isekai-panel");
            _icon = GameStyle.Icon(null, 32);
            _box.Add(_icon);
            _title = GameStyle.Label(string.Empty, TextRole.Warning);
            _title.style.marginLeft = 6;
            _box.Add(_title);
            _tip = _box.AddTooltip(string.Empty);
            HudStack.Add(HudCorner.TopLeft, _box, 2);
        }

        public void ShowSlain(string rulerName)
        {
            if (_box == null) return;
            _box.style.display = DisplayStyle.Flex;
            _title.text = string.IsNullOrEmpty(rulerName) ? "Ruler slain" : $"Ruler slain: {rulerName}";
            _tip.Content = "The Region's ruler fell with the Seat.";
            GameStyle.SetIcon(_icon, GameIcons.Named("RulerSlain"), 32);
        }

        public void ShowEscaped(string rulerName, string destinationName, IReadOnlyList<TraitId> traits)
        {
            if (_box == null) return;
            _box.style.display = DisplayStyle.Flex;
            string who = string.IsNullOrEmpty(rulerName) ? "The ruler" : rulerName;
            _title.text = $"{who} escaped to {destinationName}";
            _tip.Content = TraitsText(traits);
            GameStyle.SetIcon(_icon, GameIcons.Named("RulerEscaped"), 32);
        }

        private static string TraitsText(IReadOnlyList<TraitId> traits)
        {
            if (traits == null || traits.Count == 0) return "Traits unknown.";
            return "Traits: " + string.Join(", ", traits.Select(t => TraitTable.Get(t).Name));
        }
    }
}
