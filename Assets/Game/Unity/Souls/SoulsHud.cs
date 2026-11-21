using Blondie.Core;
using Blondie.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Isekai
{
    /// <summary>
    /// The soul readout at the top centre: soul level with a button (and the L key) to raise it for Souls, and while Jack
    /// is bodiless, in red, how many Souls drain a second and what stops him reviving. Phase 9 replaces it with the real HUD.
    /// </summary>
    [RegionSystem]
    public sealed class SoulsHud : MonoBehaviour
    {
        private const string PanelName = "Souls HUD";

        private Label _level;
        private Label _drain;
        private Label _status;
        private Button _raise;
        private string _key;

        /// <summary>The line about the drain: empty while Jack has a body.</summary>
        public string DrainText => _drain != null ? _drain.text : string.Empty;

        /// <summary>What stops or allows the revive: empty while Jack has a body.</summary>
        public string StatusText => _status != null ? _status.text : string.Empty;

        /// <summary>The soul level line.</summary>
        public string LevelText => _level != null ? _level.text : string.Empty;

        private void Start()
        {
            VisualElement root = GamePanels.Mount(this, PanelName, null, out PanelRenderer panel);
            var column = new VisualElement
            {
                style = { position = Position.Absolute, left = 0, right = 0, top = 4, alignItems = Align.Center },
            };
            root.Add(column);
            _drain = Line(column, new Color(1f, 0.35f, 0.35f));
            _status = Line(column, new Color(1f, 0.8f, 0.5f));
            _level = Line(column, Color.white);
            _raise = new Button(RaiseLevel);
            _drain.AddTooltip(TooltipText.Get(TooltipKey.HudSoulDrain));
            _status.AddTooltip(TooltipText.Get(TooltipKey.HudSouls));
            _level.AddTooltip(TooltipText.Get(TooltipKey.HudSoulLevel));
            _raise.AddTooltip(TooltipText.Get(TooltipKey.HudSoulRaise));
            column.Add(_raise);
            UIPanels.Created(panel);
        }

        private static Label Line(VisualElement parent, Color color)
        {
            var label = new Label { style = { color = color, unityFontStyleAndWeight = FontStyle.Bold } };
            label.style.display = DisplayStyle.None;
            parent.Add(label);
            return label;
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

            int level = souls.Ledger.Level, cost = souls.Ledger.LevelUpCost, have = souls.Souls;
            float drain = souls.DrainPerSecond;
            string blocker = souls.ReviveBlocker();
            string key = $"{level}/{cost}/{have}/{drain:0.0}/{blocker}/{souls.IsBodiless}/{souls.IsLost}";
            if (key == _key) return;
            _key = key;

            _level.text = $"Soul level {level}";
            _level.style.display = DisplayStyle.Flex;
            _raise.text = $"Raise soul level: {cost} Souls (L)";
            _raise.SetEnabled(have >= cost);

            bool bodiless = souls.IsBodiless;
            SetLine(_drain, bodiless ? $"BODILESS: -{drain:0.0} Souls/s ({have} left)" : null);
            // Possessing a demon is always a way back, so its hint stays even when the revive is blocked (no Anchor, too few Souls).
            string possess = $"{BodyKey.Name} over a demon: possess it ({PossessCost()} Souls)";
            SetLine(_status, !bodiless ? null
                : blocker != null ? $"{blocker}. {possess}" : $"{BodyKey.Name} over the Anchor: revive ({souls.Ledger.ReviveCost} Souls). {possess}");
        }

        private static void SetLine(Label label, string text)
        {
            label.text = text ?? string.Empty;
            label.style.display = string.IsNullOrEmpty(text) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private static int PossessCost() => RegionPossession.Current != null ? RegionPossession.Current.Cost() : 0;

        private static void RaiseLevel() => RegionSouls.Current?.TryRaiseSoulLevel();
    }
}
