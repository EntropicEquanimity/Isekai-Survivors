using Blondie.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Isekai
{
    /// <summary>
    /// Shown only while no Anchor stands: a button that raises a new one of the Run's kind beside Jack, with its price and,
    /// when it can't be raised, why (not corrupted enough, not enough Resources).
    /// </summary>
    [RegionSystem]
    public sealed class AnchorRaisePanel : MonoBehaviour
    {
        private const string PanelName = "Anchor Raise";

        private VisualElement _box;
        private Label _status;
        private Button _raise;
        private float _nextRefresh;

        private void Start()
        {
            VisualElement root = GamePanels.Mount(this, PanelName, null, out PanelRenderer panel);
            _box = new VisualElement
            {
                style =
                {
                    position = Position.Absolute,
                    right = 6,
                    top = 130,
                    width = 280,
                    backgroundColor = new Color(0f, 0f, 0f, 0.6f),
                    paddingBottom = 6,
                    paddingTop = 6,
                    paddingLeft = 8,
                    paddingRight = 8,
                    display = DisplayStyle.None,
                },
            };
            _box.Add(GameStyle.Label("No Anchor", TextRole.Title));
            _status = GameStyle.Label(string.Empty, TextRole.Muted);
            _status.style.marginTop = 2;
            _box.Add(_status);
            _raise = new Button(Raise) { text = "Raise a new Anchor" };
            _raise.style.marginTop = 6;
            _box.Add(_raise);
            root.Add(_box);
            UIPanels.Created(panel);
        }

        private void Update()
        {
            if (_box == null || Time.unscaledTime < _nextRefresh) return;
            _nextRefresh = Time.unscaledTime + 0.2f;
            Refresh();
        }

        private void Raise()
        {
            RegionAnchor.Current?.TryRaise(out _);
            Refresh();
        }

        private void Refresh()
        {
            bool none = RegionAnchor.Standing() == null;
            _box.style.display = none ? DisplayStyle.Flex : DisplayStyle.None;
            if (!none) return;
            BuildingAsset asset = AnchorCatalog.Asset(AnchorCatalog.Chosen);
            string name = AnchorTuning.Name(AnchorCatalog.Chosen);
            _raise.text = asset != null ? $"Raise a {name} ({asset.PriceText})" : $"Raise a {name}";
            _status.text = "No revive until a new Anchor goes up, on corrupted ground beside Jack.";
        }
    }
}
