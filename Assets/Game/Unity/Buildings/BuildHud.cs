using System.Collections.Generic;
using System.Text;
using Blondie.Core;
using Blondie.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Isekai
{
    /// <summary>
    /// Placeholder building UI (phase 9 replaces it): the build list with hotkeys and prices bottom-left, why a
    /// placement is refused, and for a selected Building its state and, for a Portal, its panel (<see cref="PortalPanel"/>).
    /// </summary>
    [RequireComponent(typeof(BuildMode))]
    public sealed class BuildHud : MonoBehaviour
    {
        private const string PanelName = "Build HUD";

        [SerializeField] private PanelSettings _panelSettings;

        private BuildMode _mode;
        private readonly List<Button> _buttons = new();
        private readonly List<TooltipManipulator> _buttonTooltips = new();
        private Label _help;
        private VisualElement _selection;
        private Label _selectionText;
        private PortalPanel _portalPanel;
        private float _nextRefresh;

        private void Awake() => _mode = GetComponent<BuildMode>();

        private void Start()
        {
            VisualElement root = GamePanels.Mount(this, PanelName, _panelSettings, out PanelRenderer panel);
            BuildList(root);

            _selection = new VisualElement
            {
                style =
                {
                    position = Position.Absolute, bottom = 4, left = new Length(35, LengthUnit.Percent), width = new Length(30, LengthUnit.Percent),
                    backgroundColor = new Color(0f, 0f, 0f, 0.6f), paddingLeft = 6, paddingRight = 6, paddingTop = 4, paddingBottom = 4,
                },
            };
            _selectionText = new Label { style = { color = Color.white } };
            _portalPanel = new PortalPanel { name = "portal-panel" };
            _selection.Add(_selectionText);
            _selection.Add(_portalPanel);
            root.Add(_selection);
            UIPanels.Created(panel);

            _mode.Changed += Redraw;
            Redraw();
        }

        private void BuildList(VisualElement root)
        {
            var box = new VisualElement
            {
                style =
                {
                    position = Position.Absolute,
                    left = 6,
                    bottom = 4,
                    minWidth = 280,
                    backgroundColor = new Color(0f, 0f, 0f, 0.6f),
                    paddingBottom = 4,
                    paddingTop = 4,
                    paddingLeft = 6,
                    paddingRight = 6,
                },
            };
            for (int i = 0; i < _mode.Catalogue.Count; i++)
            {
                BuildingAsset asset = _mode.Catalogue[i];
                var button = new Button(() => _mode.Pick(asset)) { name = $"build-{asset.DisplayName}", text = asset.DisplayName };
                button.style.unityTextAlign = TextAnchor.MiddleLeft;
                button.style.marginTop = 1;
                _buttons.Add(button);
                _buttonTooltips.Add(button.AddTooltip(string.Empty, asset.DisplayName));
                box.Add(button);
            }

            _help = GameStyle.Label(string.Empty, TextRole.Muted);
            _help.style.marginTop = 4;
            _help.AddTooltip(TooltipText.Get(TooltipKey.HudBuildHelp));
            box.Add(_help);
            root.Add(box);
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

        private void Redraw()
        {
            if (_buttons.Count == 0 || _help == null) return;
            Stockpile pile = RegionEconomy.Current != null ? RegionEconomy.Current.Stockpile : null;
            for (int i = 0; i < _mode.Catalogue.Count; i++)
            {
                BuildingAsset a = _mode.Catalogue[i];
                bool picked = a == _mode.Picked;
                bool affordable = pile != null && pile.CanAfford(a.Costs);
                Button button = _buttons[i];
                button.text = $"{(picked ? "> " : "  ")}{i + 1} {a.DisplayName}  {a.PriceText}" + (affordable ? string.Empty : "  (can't afford)");
                string reason = picked && !_mode.Placement.IsOk ? _mode.Placement.Reason : null;
                TooltipManipulator tip = _buttonTooltips[i];
                tip.Header = a.DisplayName;
                tip.Content = TooltipProvider.BuildButtonBody(a, affordable, reason);
            }
            _help.text = _mode.Picked != null
                ? (_mode.Placement.IsOk ? "Click to place, Shift to keep placing, right click to cancel" : _mode.Placement.Reason)
                : "Number keys: build. Click a Building to select it. Walk into Stone or rocks to mine them. M: map.";

            Building selected = _mode.Selected;
            _selection.style.display = selected != null ? DisplayStyle.Flex : DisplayStyle.None;
            if (selected == null) return;
            string name = selected.Asset != null ? selected.Asset.DisplayName : selected.name;
            var info = new StringBuilder(name);
            info.Append("  ").Append(Mathf.CeilToInt(selected.Damageable.Unit.Health)).Append(" hp");
            if (!selected.IsBuilt) info.Append("  building ").Append(Mathf.FloorToInt(selected.Progress * 100f)).Append('%');
            _selectionText.text = info.ToString();
            _portalPanel.Show(selected.IsBuilt && selected.TryGetComponent(out UnitProducer portal) ? portal : null);
        }
    }
}
