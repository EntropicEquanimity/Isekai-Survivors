using Blondie.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Isekai
{
    /// <summary>
    /// Shown only while no Anchor stands: one icon button that raises a new one of the Run's kind beside Jack, with its price.
    /// What it does and why it can't be raised are in its tooltip; a refused click also says why over the world.
    /// </summary>
    [RegionSystem]
    public sealed class AnchorRaisePanel : MonoBehaviour
    {
        private VisualElement _box;
        private IconButton _raise;
        private float _nextRefresh;

        private void Start()
        {
            _box = new VisualElement { style = { display = DisplayStyle.None } };
            _box.AddToClassList("isekai-panel");
            _box.AddToClassList("isekai-panel--alarm");
            _raise = new IconButton(GameIcons.Of(AnchorCatalog.Chosen), "Raise a " + AnchorTuning.Name(AnchorCatalog.Chosen), Raise)
            {
                Tooltip = TooltipText.Get(TooltipKey.HudAnchorRaise),
            };
            _box.Add(_raise);
            HudStack.Add(HudCorner.TopRight, _box, 3);
        }

        private void Update()
        {
            if (_box == null || Time.unscaledTime < _nextRefresh) return;
            _nextRefresh = Time.unscaledTime + 0.2f;
            Refresh();
        }

        private void Raise()
        {
            if (RegionAnchor.Current != null && RegionAnchor.Current.TryRaise(out string reason) == null && !string.IsNullOrEmpty(reason) && Camera.main != null)
                FloatingText.Spawn(Camera.main.transform.position + new Vector3(0f, 1f, 10f), reason, Color.red, 3f);
            Refresh();
        }

        private void Refresh()
        {
            bool none = RegionAnchor.Standing() == null;
            _box.style.display = none ? DisplayStyle.Flex : DisplayStyle.None;
            if (!none) return;
            BuildingAsset asset = AnchorCatalog.Asset(AnchorCatalog.Chosen);
            if (asset != null) _raise.SetCosts(asset.Costs, RegionEconomy.Current != null ? RegionEconomy.Current.Stockpile : null);
        }
    }
}
