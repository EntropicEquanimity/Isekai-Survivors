using System.Collections.Generic;
using System.Linq;
using Blondie.Core;
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
        private const string PanelName = "Ruler Capture Summary";

        private VisualElement _box;
        private Label _title;
        private Label _detail;

        public static RulerCaptureSummaryPanel Current { get; private set; }
        public string TitleText => _title != null ? _title.text : string.Empty;
        public string DetailText => _detail != null ? _detail.text : string.Empty;
        public bool IsVisible => _box != null && _box.style.display == DisplayStyle.Flex;

        private void OnEnable() => Current = this;

        private void OnDisable()
        {
            if (Current == this) Current = null;
        }

        private void Start()
        {
            VisualElement root = GamePanels.Mount(this, PanelName, null, out PanelRenderer panel);
            _box = new VisualElement
            {
                style =
                {
                    position = Position.Absolute,
                    left = 6,
                    bottom = 6,
                    width = 360,
                    backgroundColor = new Color(0f, 0f, 0f, 0.62f),
                    borderBottomColor = GameStyle.Border,
                    borderBottomWidth = 1,
                    borderTopColor = GameStyle.Border,
                    borderTopWidth = 1,
                    borderLeftColor = GameStyle.Border,
                    borderLeftWidth = 1,
                    borderRightColor = GameStyle.Border,
                    borderRightWidth = 1,
                    paddingBottom = 6,
                    paddingTop = 6,
                    paddingLeft = 8,
                    paddingRight = 8,
                    display = DisplayStyle.None,
                },
            };
            _title = GameStyle.Label(string.Empty, TextRole.Warning);
            _detail = GameStyle.Label(string.Empty, TextRole.Muted);
            _detail.style.marginTop = 2;
            _box.Add(_title);
            _box.Add(_detail);
            root.Add(_box);
            UIPanels.Created(panel);
        }

        public void ShowSlain(string rulerName)
        {
            if (_box == null) return;
            _box.style.display = DisplayStyle.Flex;
            _title.text = string.IsNullOrEmpty(rulerName) ? "Ruler slain" : $"Ruler slain: {rulerName}";
            _detail.text = "The Region's ruler fell with the Seat.";
        }

        public void ShowEscaped(string rulerName, string destinationName, IReadOnlyList<TraitId> traits)
        {
            if (_box == null) return;
            _box.style.display = DisplayStyle.Flex;
            string who = string.IsNullOrEmpty(rulerName) ? "The ruler" : rulerName;
            _title.text = $"{who} escaped to {destinationName}";
            _detail.text = TraitsText(traits);
        }

        private static string TraitsText(IReadOnlyList<TraitId> traits)
        {
            if (traits == null || traits.Count == 0) return "Traits unknown.";
            return "Traits: " + string.Join(", ", traits.Select(t => TraitTable.Get(t).Name));
        }
    }
}
