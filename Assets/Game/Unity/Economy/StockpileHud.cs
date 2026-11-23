using System.Collections.Generic;
using Blondie.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Isekai
{
    /// <summary>
    /// The Region's stockpile in the top-left corner, an icon and a number per Resource, and at the bottom centre what the
    /// Character is mining (its icon and how much is left).
    /// </summary>
    public sealed class StockpileHud : MonoBehaviour
    {
        private static readonly ResourceType[] Shown = { ResourceType.Wood, ResourceType.Stone, ResourceType.Souls };

        private readonly Dictionary<ResourceType, Label> _amounts = new();
        private VisualElement _prompt;
        private VisualElement _promptIcon;
        private Label _promptLeft;
        private Stockpile _stockpile;
        private Miner _miner;
        private ResourceNode _promptFor;

        private void Start()
        {
            var row = new VisualElement();
            row.AddToClassList("isekai-panel");
            row.style.flexDirection = FlexDirection.Row;
            foreach (ResourceType type in Shown)
            {
                VisualElement pair = GameStyle.IconNumber(GameIcons.Of(type), "0", out Label amount);
                pair.style.marginRight = 8;
                pair.AddTooltip(TooltipText.Get(TooltipProvider.KeyForResource(type)), type.ToString());
                _amounts[type] = amount;
                row.Add(pair);
            }
            HudStack.Add(HudCorner.TopLeft, row, 0);

            _prompt = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, display = DisplayStyle.None } };
            _prompt.AddToClassList("isekai-panel");
            _promptIcon = GameStyle.Icon(null);
            _promptLeft = GameStyle.Number("0");
            _prompt.Add(_promptIcon);
            _prompt.Add(_promptLeft);
            HudStack.Add(HudCorner.BottomCentre, _prompt, 0);
        }

        /// <summary>What the mining prompt says, in words (empty when there's nothing to mine).</summary>
        public string Prompt => _promptFor != null ? $"Mining {_promptFor.Type}: {_promptFor.Remaining} left" : string.Empty;

        private void Update()
        {
            using (PerfMarkers.Hud.Auto()) Tick();
        }

        private void Tick()
        {
            UpdatePrompt();
            Stockpile current = RegionEconomy.Current != null ? RegionEconomy.Current.Stockpile : null;
            if (current == _stockpile) return;
            if (_stockpile != null) _stockpile.Changed -= OnChanged;
            _stockpile = current;
            if (_stockpile != null) _stockpile.Changed += OnChanged;
            foreach (ResourceType type in Shown) Show(type);
        }

        private void UpdatePrompt()
        {
            if (_prompt == null) return;
            _miner = Player.Character != null ? Player.Character.GetComponent<Miner>() : null;
            ResourceNode node = _miner != null ? _miner.Node ?? _miner.InReach : null;
            if (node != _promptFor)
            {
                _promptFor = node;
                _prompt.style.display = node != null ? DisplayStyle.Flex : DisplayStyle.None;
                if (node != null) GameStyle.SetIcon(_promptIcon, GameIcons.Of(node.Type));
            }
            if (node == null) return;
            string left = node.Remaining.ToString();
            if (_promptLeft.text != left) _promptLeft.text = left;
        }

        private void OnDestroy()
        {
            if (_stockpile != null) _stockpile.Changed -= OnChanged;
        }

        private void OnChanged(ResourceType type, int amount) => Show(type);

        private void Show(ResourceType type)
        {
            if (_amounts.TryGetValue(type, out Label label)) label.text = (_stockpile != null ? _stockpile.Get(type) : 0).ToString();
        }
    }
}
