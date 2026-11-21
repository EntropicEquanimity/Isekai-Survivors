using System;
using System.Collections.Generic;
using System.Text;
using Blondie.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Isekai
{
    /// <summary>
    /// A button the docs/ui-rules.md way: an icon first, then numbers (a hotkey, costs as "20 [stone icon]"), and the
    /// words in its tooltip. With no icon it shows a short name instead, so nothing is ever blank.
    /// </summary>
    public sealed class IconButton : Button
    {
        private readonly VisualElement _icon;
        private readonly Label _key;
        private readonly Label _fallback;
        private readonly VisualElement _costs;
        private readonly TooltipManipulator _tooltip;
        private readonly List<(ResourceType type, Label amount)> _costLabels = new();
        private bool _picked;

        public IconButton(Sprite icon, string name, Action clicked, string iconSize = "large", string key = null, bool column = false) : base(clicked)
        {
            AddToClassList("isekai-icon-button");
            if (column) AddToClassList("isekai-icon-button--column");
            Name = name;
            this.name = name;
            if (key != null)
            {
                _key = new Label(key) { pickingMode = PickingMode.Ignore };
                _key.AddToClassList("isekai-key");
                Add(_key);
            }
            _icon = GameStyle.Icon(icon, iconSize);
            Add(_icon);
            _fallback = new Label(name) { pickingMode = PickingMode.Ignore };
            _fallback.style.display = icon != null ? DisplayStyle.None : DisplayStyle.Flex;
            Add(_fallback);
            _costs = new VisualElement { pickingMode = PickingMode.Ignore, style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginLeft = 2 } };
            Add(_costs);
            _tooltip = this.AddTooltip(string.Empty, name);
        }

        /// <summary>What the button is (the tooltip's header and the fallback text).</summary>
        public string Name { get; }

        /// <summary>The costs as "22 Souls, 20 Stone" (tests and logs; the screen shows icons).</summary>
        public string CostText
        {
            get
            {
                var text = new StringBuilder();
                foreach ((ResourceType type, Label amount) in _costLabels)
                {
                    if (text.Length > 0) text.Append(", ");
                    text.Append(amount.text).Append(' ').Append(type);
                }
                return text.ToString();
            }
        }

        /// <summary>The tooltip's body (explanation, placement reason).</summary>
        public string Tooltip
        {
            get => _tooltip.Content;
            set => _tooltip.Content = value;
        }

        /// <summary>Selected: framed in ember.</summary>
        public bool Picked
        {
            get => _picked;
            set
            {
                _picked = value;
                EnableInClassList("isekai-icon-button--picked", value);
            }
        }

        public void SetIcon(Sprite icon)
        {
            _icon.style.backgroundImage = icon != null ? new StyleBackground(icon) : new StyleBackground(StyleKeyword.None);
            _fallback.style.display = icon != null ? DisplayStyle.None : DisplayStyle.Flex;
        }

        /// <summary>Shows the costs as numbers with resource icons; a cost the stockpile can't pay turns red.</summary>
        public void SetCosts(IReadOnlyList<ResourceCost> costs, Stockpile pile)
        {
            if (_costLabels.Count != costs.Count) BuildCosts(costs);
            for (int i = 0; i < costs.Count; i++)
            {
                Label amount = _costLabels[i].amount;
                string text = costs[i].Amount.ToString();
                if (amount.text != text) amount.text = text;
                bool short_ = pile != null && pile.Get(costs[i].Type) < costs[i].Amount;
                amount.EnableInClassList("isekai-number--short", short_);
            }
        }

        private void BuildCosts(IReadOnlyList<ResourceCost> costs)
        {
            _costs.Clear();
            _costLabels.Clear();
            foreach (ResourceCost cost in costs)
            {
                var amount = new Label(cost.Amount.ToString()) { pickingMode = PickingMode.Ignore };
                amount.AddToClassList("isekai-number");
                _costs.Add(amount);
                Sprite icon = GameIcons.Of(cost.Type);
                if (icon != null) _costs.Add(GameStyle.Icon(icon));
                else _costs.Add(new Label(cost.Type.ToString()) { pickingMode = PickingMode.Ignore });
                _costLabels.Add((cost.Type, amount));
            }
        }
    }
}
