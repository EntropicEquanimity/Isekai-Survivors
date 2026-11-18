using System;
using System.Text;
using Blondie.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Isekai
{
    /// <summary>A plain readout of the Region's stockpile in the top-left corner. Phase 9 replaces it with the real HUD.</summary>
    public sealed class StockpileHud : MonoBehaviour
    {
        private const string PanelName = "Stockpile HUD";

        [SerializeField] private PanelSettings _panelSettings;

        private Label _label;
        private Stockpile _stockpile;

        private void Start()
        {
            VisualElement root = GamePanels.Mount(this, PanelName, _panelSettings, out PanelRenderer panel);
            _label = new Label { style = { position = Position.Absolute, left = 6, top = 4, color = Color.white } };
            root.Add(_label);
            UIPanels.Created(panel);
        }

        private void Update()
        {
            Stockpile current = RegionEconomy.Current != null ? RegionEconomy.Current.Stockpile : null;
            if (current == _stockpile && _label != null && _label.text.Length > 0) return;
            if (_stockpile != null) _stockpile.Changed -= OnChanged;
            _stockpile = current;
            if (_stockpile != null) _stockpile.Changed += OnChanged;
            Redraw();
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
