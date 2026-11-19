using System;
using Blondie.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Isekai
{
    /// <summary>
    /// Placeholder building UI (phase 9 replaces it): the build list with hotkeys and prices bottom-left, why a
    /// placement is refused, and for a selected Building its state, Cloning Pods and its Units' stance buttons.
    /// </summary>
    [RequireComponent(typeof(BuildMode))]
    public sealed class BuildHud : MonoBehaviour
    {
        private const string PanelName = "Build HUD";

        [SerializeField] private PanelSettings _panelSettings;

        private BuildMode _mode;
        private Label _list;
        private VisualElement _selection;
        private Label _selectionText;
        private VisualElement _stanceRow;
        private float _nextRefresh;

        private void Awake() => _mode = GetComponent<BuildMode>();

        private void Start()
        {
            VisualElement root = GamePanels.Mount(this, PanelName, _panelSettings, out PanelRenderer panel);

            _list = new Label { pickingMode = PickingMode.Ignore, style = { position = Position.Absolute, left = 6, bottom = 4, color = Color.white } };
            root.Add(_list);

            _selection = new VisualElement
            {
                style =
                {
                    position = Position.Absolute, bottom = 4, left = new Length(35, LengthUnit.Percent), width = new Length(30, LengthUnit.Percent),
                    backgroundColor = new Color(0f, 0f, 0f, 0.6f), paddingLeft = 6, paddingRight = 6, paddingTop = 4, paddingBottom = 4,
                },
            };
            _selectionText = new Label { style = { color = Color.white } };
            _stanceRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            foreach (Stance stance in Enum.GetValues(typeof(Stance)))
            {
                Stance s = stance;
                _stanceRow.Add(new Button(() => SetStance(s)) { text = s.ToString(), name = "stance-" + s });
            }
            _selection.Add(_selectionText);
            _selection.Add(_stanceRow);
            root.Add(_selection);
            UIPanels.Created(panel);

            _mode.Changed += Redraw;
            Redraw();
        }

        private void OnDestroy()
        {
            if (_mode != null) _mode.Changed -= Redraw;
        }

        // Construction progress and pod counts change without events: refresh a few times a second.
        private void Update()
        {
            using (PerfMarkers.Hud.Auto()) Tick();
        }

        private void Tick()
        {
            if (Time.unscaledTime < _nextRefresh) return;
            _nextRefresh = Time.unscaledTime + 0.25f;
            Redraw();
        }

        private void SetStance(Stance stance)
        {
            if (_mode.Selected != null && _mode.Selected.TryGetComponent(out UnitProducer producer)) producer.Stance = stance;
            Redraw();
        }

        private void Redraw()
        {
            if (_list == null) return;
            Stockpile pile = RegionEconomy.Current != null ? RegionEconomy.Current.Stockpile : null;
            var text = new System.Text.StringBuilder();
            for (int i = 0; i < _mode.Catalogue.Count; i++)
            {
                BuildingAsset a = _mode.Catalogue[i];
                bool picked = a == _mode.Picked;
                bool affordable = pile != null && pile.CanAfford(a.Costs);
                text.Append(picked ? "> " : "  ").Append(i + 1).Append(' ').Append(a.DisplayName).Append("  ").Append(a.PriceText);
                if (!affordable) text.Append("  (can't afford)");
                text.Append('\n');
            }
            if (_mode.Picked != null)
                text.Append(_mode.Placement.IsOk ? "Click to place, Shift to keep placing, right click to cancel" : _mode.Placement.Reason);
            else text.Append("Number keys: build. Click a Building to select it. Hold E by Stone to mine. M: map.");
            _list.text = text.ToString();

            Building selected = _mode.Selected;
            _selection.style.display = selected != null ? DisplayStyle.Flex : DisplayStyle.None;
            if (selected == null) return;
            string name = selected.Asset != null ? selected.Asset.DisplayName : selected.name;
            var info = new System.Text.StringBuilder(name);
            info.Append("  ").Append(Mathf.CeilToInt(selected.Damageable.Unit.Health)).Append(" hp");
            if (!selected.IsBuilt) info.Append("  building ").Append(Mathf.FloorToInt(selected.Progress * 100f)).Append('%');
            if (selected.TryGetComponent(out CloningPods pods)) info.Append("  Cloning Pods ").Append(pods.Bay.Pods);
            bool producer = selected.TryGetComponent(out UnitProducer p);
            if (producer) info.Append("  Units ").Append(p.Units.Count).Append("  Stance ").Append(p.Stance);
            _selectionText.text = info.ToString();
            _stanceRow.style.display = producer ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
