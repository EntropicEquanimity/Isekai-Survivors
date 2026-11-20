using Blondie.Core;
using Blondie.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Isekai
{
    /// <summary>
    /// A plain readout of the Region's stockpile in the top-left corner, and a prompt at the bottom centre while the
    /// Character is mining ("Mining Stone: 42 left"). Phase 9 replaces it with the real HUD.
    /// </summary>
    public sealed class StockpileHud : MonoBehaviour
    {
        private const string PanelName = "Stockpile HUD";

        [SerializeField] private PanelSettings _panelSettings;

        private Label _wood;
        private Label _stone;
        private Label _souls;
        private Label _prompt;
        private Stockpile _stockpile;
        private Miner _miner;

        private void Start()
        {
            VisualElement root = GamePanels.Mount(this, PanelName, _panelSettings, out PanelRenderer panel);
            var row = new VisualElement
            {
                style = { position = Position.Absolute, left = 6, top = 4, flexDirection = FlexDirection.Row, alignItems = Align.Center },
            };
            _wood = AddResource(row, ResourceType.Wood);
            _stone = AddResource(row, ResourceType.Stone);
            _souls = AddResource(row, ResourceType.Souls);
            root.Add(row);
            _prompt = new Label
            {
                pickingMode = PickingMode.Ignore,
                style = { position = Position.Absolute, left = 0, right = 0, bottom = 120, unityTextAlign = TextAnchor.MiddleCenter, color = Color.white },
            };
            root.Add(_prompt);
            UIPanels.Created(panel);
        }

        /// <summary>The mining prompt shown now (empty when there's nothing to mine).</summary>
        public string Prompt => _prompt != null ? _prompt.text : string.Empty;

        private static Label AddResource(VisualElement row, ResourceType type)
        {
            var label = new Label { style = { color = Color.white, marginRight = 12 } };
            label.AddTooltip(TooltipText.Get(TooltipProvider.KeyForResource(type)), type.ToString());
            row.Add(label);
            return label;
        }

        private void Update()
        {
            using (PerfMarkers.Hud.Auto()) Tick();
        }

        private void Tick()
        {
            UpdatePrompt();
            Stockpile current = RegionEconomy.Current != null ? RegionEconomy.Current.Stockpile : null;
            if (current == _stockpile && _wood != null && _wood.text.Length > 0) return;
            if (_stockpile != null) _stockpile.Changed -= OnChanged;
            _stockpile = current;
            if (_stockpile != null) _stockpile.Changed += OnChanged;
            Redraw();
        }

        private void UpdatePrompt()
        {
            if (_prompt == null) return;
            _miner = Player.Character != null ? Player.Character.GetComponent<Miner>() : null;
            ResourceNode node = _miner != null ? _miner.Node ?? _miner.InReach : null;
            string text = node == null ? string.Empty : $"Mining {node.Type}: {node.Remaining} left";
            if (_prompt.text != text) _prompt.text = text;
        }

        private void OnDestroy()
        {
            if (_stockpile != null) _stockpile.Changed -= OnChanged;
        }

        private void OnChanged(ResourceType type, int amount) => Redraw();

        private void Redraw()
        {
            if (_wood == null || _stone == null || _souls == null) return;
            if (_stockpile == null)
            {
                _wood.text = "Wood 0";
                _stone.text = "Stone 0";
                _souls.text = "Souls 0";
                return;
            }

            _wood.text = $"Wood {_stockpile.Get(ResourceType.Wood)}";
            _stone.text = $"Stone {_stockpile.Get(ResourceType.Stone)}";
            _souls.text = $"Souls {_stockpile.Get(ResourceType.Souls)}";
        }
    }
}
