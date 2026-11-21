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
    /// it; it starts shown until the player hides it once (remembered between sessions).
    /// </summary>
    [RegionSystem]
    public sealed class ControlsHud : MonoBehaviour
    {
        private const string PanelName = "Controls HUD";
        private const string HiddenKey = "Isekai.Controls.Hidden";
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
            VisualElement root = GamePanels.Mount(this, PanelName, null, out PanelRenderer panel);
            var column = new VisualElement
            {
                style = { position = Position.Absolute, right = 4, bottom = 4, alignItems = Align.FlexEnd },
            };
            _card = new VisualElement
            {
                style =
                {
                    width = 230, backgroundColor = new Color(0f, 0f, 0f, 0.7f),
                    paddingLeft = 5, paddingRight = 5, paddingTop = 4, paddingBottom = 4,
                    borderTopWidth = 1, borderBottomWidth = 1, borderLeftWidth = 1, borderRightWidth = 1,
                    borderTopColor = GameStyle.Border, borderBottomColor = GameStyle.Border,
                    borderLeftColor = GameStyle.Border, borderRightColor = GameStyle.Border,
                },
            };
            _card.Add(GameStyle.Label("Controls", TextRole.Accent));
            _body = GameStyle.Label(string.Empty, TextRole.Body);
            _body.style.whiteSpace = WhiteSpace.Normal;
            _card.Add(_body);
            _hint = GameStyle.Label("H: controls", TextRole.Muted);
            column.Add(_card);
            column.Add(_hint);
            root.Add(column);
            UIPanels.Created(panel);
            SetOpen(!LoadHidden());
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
            SaveHidden(!IsOpen);
        }

        private void SetOpen(bool open)
        {
            if (_card == null) return;
            _card.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
            _hint.text = open ? "H: hide controls" : "H: controls";
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

        private static bool LoadHidden()
        {
            try { return PlayerPrefs.GetInt(HiddenKey, 0) == 1; }
            catch { return false; }
        }

        private static void SaveHidden(bool hidden)
        {
            try { PlayerPrefs.SetInt(HiddenKey, hidden ? 1 : 0); }
            catch { /* preferences unavailable: the choice lasts this session */ }
        }
    }
}
