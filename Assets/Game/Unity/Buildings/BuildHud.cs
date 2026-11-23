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
        private BuildMode _mode;
        private readonly List<IconButton> _buttons = new();
        private Label _help;
        private VisualElement _selection;
        private Label _selectionText;
        private VisualElement _selectionIcon;
        private TooltipManipulator _selectionTip;
        private Building _shownSelection;
        private PortalPanel _portalPanel;
        private float _nextRefresh;

        private void Awake() => _mode = GetComponent<BuildMode>();

        private void Start()
        {
            BuildList();

            _selection = new VisualElement { style = { display = DisplayStyle.None } };
            _selection.AddToClassList("isekai-panel");
            var head = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            _selectionIcon = GameStyle.Icon(null, 32);
            head.Add(_selectionIcon);
            VisualElement health = GameStyle.IconNumber(GameIcons.Named("Health"), "0", out _selectionText, "hp");
            health.style.marginLeft = 6;
            head.Add(health);
            _selectionTip = head.AddTooltip(string.Empty);
            _portalPanel = new PortalPanel { name = "portal-panel" };
            _selection.Add(head);
            _selection.Add(_portalPanel);
            HudStack.Add(HudCorner.BottomCentre, _selection, 1);

            _mode.Changed += Redraw;
            Redraw();
        }

        // A row of icon buttons bottom-left (docs/ui-rules.md): hotkey, the Building's icon, its costs as numbers with
        // resource icons; the name and role are in the tooltip. One short line under it: why a placement is refused.
        private void BuildList()
        {
            var box = new VisualElement { style = { alignItems = Align.FlexStart } };
            box.AddToClassList("isekai-panel");
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            for (int i = 0; i < _mode.Catalogue.Count; i++)
            {
                BuildingAsset asset = _mode.Catalogue[i];
                var button = new IconButton(GameIcons.Of(asset), asset.DisplayName, () => _mode.Pick(asset), key: (i + 1).ToString())
                {
                    name = $"build-{asset.DisplayName}",
                };
                if (i > 0) button.style.marginLeft = 2;
                _buttons.Add(button);
                row.Add(button);
            }
            box.Add(row);
            _help = GameStyle.Label(string.Empty, TextRole.Muted);
            _help.style.marginTop = 2;
            _help.AddTooltip(TooltipText.Get(TooltipKey.HudBuildHelp));
            box.Add(_help);
            HudStack.Add(HudCorner.BottomLeft, box, 0);
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
                IconButton button = _buttons[i];
                button.Picked = picked;
                button.SetCosts(a.Costs, pile);
                string reason = picked && !_mode.Placement.IsOk ? _mode.Placement.Reason : null;
                button.Tooltip = TooltipProvider.BuildButtonBody(a, affordable, reason);
            }
            string help = _mode.Picked != null
                ? (_mode.Placement.IsOk ? "Click: place. Shift: keep placing. Right click: cancel." : _mode.Placement.Reason)
                : string.Empty;
            if (_help.text != help) _help.text = help;
            _help.style.display = help.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;

            Building selected = _mode.Selected;
            _selection.style.display = selected != null ? DisplayStyle.Flex : DisplayStyle.None;
            if (selected == null) return;
            string name = selected.Asset != null ? selected.Asset.DisplayName : selected.name;
            if (_shownSelection != selected)
            {
                _shownSelection = selected;
                Sprite icon = GameIcons.Of(selected.Asset);
                GameStyle.SetIcon(_selectionIcon, icon, 32);
                _selectionTip.Header = name;
            }
            // Numbers beside the icon: health, and how far it is built while it goes up.
            var info = new StringBuilder();
            info.Append(Mathf.CeilToInt(selected.Damageable.Unit.Health));
            if (!selected.IsBuilt) info.Append("  ").Append(Mathf.FloorToInt(selected.Progress * 100f)).Append('%');
            _selectionText.text = info.ToString();
            _selectionTip.Content = selected.IsBuilt ? null : "Going up: it gains health as it is built.";
            _portalPanel.Show(selected.IsBuilt && selected.TryGetComponent(out UnitProducer portal) ? portal : null);
        }
    }
}
