using System;
using System.Text;
using Blondie.Core;
using Blondie.Input;
using UnityEngine;
using UnityEngine.UIElements;

namespace Isekai
{
    /// <summary>
    /// A plain readout of the Region's stockpile in the top-left corner, and a prompt at the bottom centre while the
    /// Character stands by a Resource node ("Hold E to mine Stone"). Phase 9 replaces it with the real HUD.
    /// </summary>
    public sealed class StockpileHud : MonoBehaviour
    {
        private const string PanelName = "Stockpile HUD";

        [SerializeField] private PanelSettings _panelSettings;

        private Label _label;
        private Label _prompt;
        private Stockpile _stockpile;
        private Miner _miner;

        private void Start()
        {
            VisualElement root = GamePanels.Mount(this, PanelName, _panelSettings, out PanelRenderer panel);
            _label = new Label { style = { position = Position.Absolute, left = 6, top = 4, color = Color.white } };
            root.Add(_label);
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

        private void Update()
        {
            UpdatePrompt();
            Stockpile current = RegionEconomy.Current != null ? RegionEconomy.Current.Stockpile : null;
            if (current == _stockpile && _label != null && _label.text.Length > 0) return;
            if (_stockpile != null) _stockpile.Changed -= OnChanged;
            _stockpile = current;
            if (_stockpile != null) _stockpile.Changed += OnChanged;
            Redraw();
        }

        private void UpdatePrompt()
        {
            if (_prompt == null) return;
            if (_miner == null || !_miner.isActiveAndEnabled) _miner = FindAnyObjectByType<Miner>();
            ResourceNode node = _miner != null ? _miner.Node ?? _miner.InReach : null;
            string text = node == null ? string.Empty
                : _miner.Node != null ? $"Mining {node.Type}: {node.Remaining} left"
                : $"Hold {InteractKey()} to mine {node.Type} ({node.Remaining} left)";
            if (_prompt.text != text) _prompt.text = text;
        }

        private static string InteractKey()
        {
            string key = GameInput.GetBindingDisplay(GameAction.Interact);
            return string.IsNullOrEmpty(key) ? "E" : key;
        }

        private void OnDestroy()
        {
            if (_stockpile != null) _stockpile.Changed -= OnChanged;
        }

        private void OnChanged(ResourceType type, int amount) => Redraw();

        private void Redraw()
        {
            if (_label == null) return;
            if (_stockpile == null) { _label.text = " "; return; }
            var text = new StringBuilder();
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
                text.Append(type).Append(' ').Append(_stockpile.Get(type)).Append("   ");
            _label.text = text.ToString();
        }
    }
}
