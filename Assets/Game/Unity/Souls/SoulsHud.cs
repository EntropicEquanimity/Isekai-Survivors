using Blondie.Core;
using Blondie.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Isekai
{
    /// <summary>
    /// The soul readout at the top centre, icons and numbers (words in the tooltips): the soul level with the button (and L)
    /// that raises it, and while Jack is bodiless, in red, the Souls that drain each second and his two ways back: the key
    /// with the Anchor and its price, and the key with a demon and its price. A way back that is blocked is dimmed.
    /// </summary>
    [RegionSystem]
    public sealed class SoulsHud : MonoBehaviour
    {
        private Label _level;
        private IconButton _raise;
        private VisualElement _bodiless;
        private Label _drain, _reviveKey, _reviveCost, _possessKey, _possessCost;
        private VisualElement _revive;
        private TooltipManipulator _reviveTip;
        private string _key;

        /// <summary>What the drain readout says, in words: empty while Jack has a body.</summary>
        public string DrainText { get; private set; } = string.Empty;

        /// <summary>What the ways back say, in words: empty while Jack has a body.</summary>
        public string StatusText { get; private set; } = string.Empty;

        /// <summary>The soul level, in words.</summary>
        public string LevelText { get; private set; } = string.Empty;

        private void Start()
        {
            var column = new VisualElement { style = { alignItems = Align.Center } };
            column.AddToClassList("isekai-panel");

            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            VisualElement level = GameStyle.IconNumber(GameIcons.Named("SoulLevel"), "1", out _level, "Soul level");
            level.AddTooltip(TooltipText.Get(TooltipKey.HudSoulLevel), "Soul level");
            level.style.marginRight = 6;
            row.Add(level);
            _raise = new IconButton(GameIcons.Named("Level"), "Raise soul level", RaiseLevel, box: 16, key: "L") { Tooltip = TooltipText.Get(TooltipKey.HudSoulRaise) };
            row.Add(_raise);
            column.Add(row);

            _bodiless = new VisualElement { style = { alignItems = Align.Center, display = DisplayStyle.None } };
            VisualElement drain = GameStyle.IconNumber(GameIcons.Named("Drain"), "0", out _drain, "Drain");
            _drain.AddToClassList("isekai-number--short");
            drain.AddTooltip(TooltipText.Get(TooltipKey.HudSoulDrain), "Bodiless");
            _bodiless.Add(drain);
            var ways = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
            _revive = Way(GameIcons.Of(AnchorCatalog.Chosen), "Anchor", out _reviveKey, out _reviveCost);
            _reviveTip = _revive.AddTooltip(string.Empty, "Revive at the Anchor");
            ways.Add(_revive);
            VisualElement possess = Way(GameIcons.Of(Resources.Load<GameObject>(RegionPossession.KingPrefab)), "Demon", out _possessKey, out _possessCost, unit: true);
            possess.AddTooltip("Fly onto one of your demons and press the key to take its body.", "Possess a demon");
            ways.Add(possess);
            _bodiless.Add(ways);
            column.Add(_bodiless);
            HudStack.Add(HudCorner.TopCentre, column, 0);
        }

        // "R [where] 22 [souls]".
        private static VisualElement Way(Sprite where, string name, out Label key, out Label cost, bool unit = false)
        {
            var way = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginLeft = 6, marginRight = 6 } };
            key = new Label(BodyKey.Name) { pickingMode = PickingMode.Ignore };
            key.AddToClassList("isekai-key");
            way.Add(key);
            if (where != null) way.Add(unit ? GameStyle.UnitIcon(where, 32, out _) : GameStyle.Icon(where, 32));
            else way.Add(new Label(name) { pickingMode = PickingMode.Ignore });
            way.Add(GameStyle.IconNumber(GameIcons.Of(ResourceType.Souls), "0", out cost));
            return way;
        }

        private void Update()
        {
            using (PerfMarkers.Hud.Auto()) Tick();
        }

        private void Tick()
        {
            RegionSouls souls = RegionSouls.Current;
            if (souls == null || _level == null) return;
            if (Keyboard.current != null && Keyboard.current[Key.L].wasPressedThisFrame) RaiseLevel();

            int level = souls.Ledger.Level, cost = souls.Ledger.LevelUpCost, have = souls.Souls, possess = PossessCost();
            float drain = souls.DrainPerSecond;
            string blocker = souls.ReviveBlocker();
            string key = $"{level}/{cost}/{have}/{drain:0.0}/{blocker}/{souls.IsBodiless}/{souls.IsLost}/{possess}/{BodyKey.Name}";
            if (key == _key) return;
            _key = key;

            LevelText = $"Soul level {level}";
            _level.text = level.ToString();
            _raise.SetCosts(new[] { new ResourceCost(ResourceType.Souls, cost) }, RegionEconomy.Current != null ? RegionEconomy.Current.Stockpile : null);
            _raise.SetEnabled(have >= cost);

            bool bodiless = souls.IsBodiless;
            _bodiless.style.display = bodiless ? DisplayStyle.Flex : DisplayStyle.None;
            DrainText = bodiless ? $"BODILESS: -{drain:0.0} Souls/s ({have} left)" : string.Empty;
            // Possessing a demon is always a way back, so it stays shown even when the revive is blocked (no Anchor, too few Souls).
            string possessText = $"{BodyKey.Name} over a demon: possess it ({possess} Souls)";
            StatusText = !bodiless ? string.Empty
                : blocker != null ? $"{blocker}. {possessText}" : $"{BodyKey.Name} over the Anchor: revive ({souls.Ledger.ReviveCost} Souls). {possessText}";
            if (!bodiless) return;
            _drain.text = $"-{drain:0.0}/s";
            _reviveKey.text = _possessKey.text = BodyKey.Name;
            _reviveCost.text = souls.Ledger.ReviveCost.ToString();
            _possessCost.text = possess.ToString();
            _revive.style.opacity = blocker != null ? 0.45f : 1f;
            _reviveTip.Content = blocker ?? "Fly onto the Anchor and press the key to come back there.";
            _possessCost.EnableInClassList("isekai-number--short", have < possess);
            _reviveCost.EnableInClassList("isekai-number--short", have < souls.Ledger.ReviveCost);
        }

        private static int PossessCost() => RegionPossession.Current != null ? RegionPossession.Current.Cost() : 0;

        private static void RaiseLevel() => RegionSouls.Current?.TryRaiseSoulLevel();
    }
}
