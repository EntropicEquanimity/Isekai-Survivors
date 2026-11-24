using System.Text;
using Blondie.Controllers;
using Blondie.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Isekai
{
    /// <summary>
    /// The controls card, bottom right: every key the Region uses, with today's Soul prices for bodies. H shows or hides
    /// it; it starts closed (docs/ui-rules.md: minimal text), and the choice is remembered between sessions.
    /// </summary>
    [RegionSystem]
    public sealed class ControlsHud : MonoBehaviour
    {
        private const string OpenKey = "Isekai.Controls.Open";
        public const Key ToggleKey = Key.H;

        private VisualElement _card;
        private Label _body;
        private Label _hint;
        private string _shown;
        private float _nextRefresh;

        /// <summary>The card is open.</summary>
        public bool IsOpen => _card != null && _card.style.display != DisplayStyle.None;

        /// <summary>The card's text now.</summary>
        public string Text => _body != null ? _body.text : string.Empty;

        private void Start()
        {
            var column = new VisualElement { style = { alignItems = Align.FlexEnd } };
            _card = new VisualElement { style = { maxWidth = 460 } };
            _card.AddToClassList("isekai-panel");
            _body = GameStyle.Label(string.Empty, TextRole.Body);
            _body.style.whiteSpace = WhiteSpace.Normal;
            _card.Add(_body);
            _hint = GameStyle.Label("H: controls", TextRole.Muted);
            column.Add(_card);
            column.Add(_hint);
            HudStack.Add(HudCorner.BottomRight, column, 0);
            SetOpen(LoadOpen());
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && !PlayerBrain.BlockInput && keyboard[ToggleKey].wasPressedThisFrame) Toggle();
            if (!IsOpen || Time.unscaledTime < _nextRefresh) return;
            _nextRefresh = Time.unscaledTime + 0.5f;
            string text = Compose();
            if (text == _shown) return;
            _shown = text;
            _body.text = text;
        }

        /// <summary>Shows or hides the card (the H key) and remembers the choice.</summary>
        public void Toggle()
        {
            SetOpen(!IsOpen);
            SaveOpen(IsOpen);
        }

        private void SetOpen(bool open)
        {
            if (_card == null) return;
            _card.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
            _nextRefresh = 0f;
        }

        // The keys, with the Soul prices that apply now (they rise with soul level).
        internal static string Compose()
        {
            RegionSouls souls = RegionSouls.Current;
            int level = souls != null ? souls.Ledger.Level : SoulTuning.StartingLevel;
            int revive = SoulRules.ReviveCost(level);
            int possess = RegionPossession.Current != null ? RegionPossession.Current.Cost() : SoulRules.PossessCost(level, bodyAlive: true);
            int king = SoulRules.SummonKingCost(level);
            string r = BodyKey.Name;
            var text = new StringBuilder();
            text.AppendLine("WASD  move. Stand still and Jack fights.");
            text.AppendLine("Walk into rocks (Stone) or dead trees (Wood) to mine.");
            text.AppendLine("1-3  build on corrupted ground. Right click cancels.");
            text.AppendLine("Click a Building  orders, rally, summons.");
            text.AppendLine($"Anchor panel  Demon King ({king} Souls), workers.");
            text.AppendLine($"{r}  leave your body (you become a wisp).");
            text.AppendLine($"Wisp + {r}  over the Anchor: revive ({revive}). Over a demon: possess ({possess}).");
            text.AppendLine("L  raise soul level.");
            text.Append("Wheel  zoom. M  map. Esc  pause.");
            return text.ToString();
        }

        private static bool LoadOpen()
        {
            return KeyValueStorage.Current.GetInt(OpenKey, 0) == 1;
        }

        private static void SaveOpen(bool open)
        {
            KeyValueStorage.Current.SetInt(OpenKey, open ? 1 : 0);
            KeyValueStorage.Current.Save();
        }
    }
}
