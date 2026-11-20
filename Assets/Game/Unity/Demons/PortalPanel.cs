using System;
using Blondie.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Isekai
{
    /// <summary>
    /// The Portal panel inside <see cref="BuildHud"/>'s selection box: the standing order (Guard, Raid, Follow), the
    /// bones policy (Harvest, Destroy, Ignore), the demon it summons (Imp, Bone demon), and buttons that mark the raid
    /// target and the rally point (the next click in the world, or a click on the map screen for the target). The option
    /// in force is the one button that is switched off.
    /// </summary>
    public sealed class PortalPanel : VisualElement
    {
        private readonly Label _summary = GameStyle.Label(string.Empty, TextRole.Muted);
        private readonly Label _demons = GameStyle.Label(string.Empty, TextRole.Muted);
        private readonly Button[] _orders = new Button[3];
        private readonly Button[] _policies = new Button[3];
        private readonly Button[] _roster = new Button[2];
        private readonly Button _markRaid, _markRally;
        private const int MaxDemonLines = 8;
        private UnitProducer _portal;

        public PortalPanel()
        {
            Add(_summary);
            _demons.name = "portal-demons";
            Add(_demons);
            AddRow("Order", _orders, Enum.GetValues(typeof(PortalOrder)), o => { if (_portal != null) _portal.Order = (PortalOrder)o; Refresh(); }, "order-");
            AddRow("Bones", _policies, Enum.GetValues(typeof(BonesPolicy)), o => { if (_portal != null) _portal.Policy = (BonesPolicy)o; Refresh(); }, "bones-");
            AddRow("Summons", _roster, new object[] { DemonType.Imp, DemonType.BoneDemon }, o => { if (_portal != null) _portal.Roster = (DemonType)o; Refresh(); }, "roster-");
            var marks = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, marginTop = 2 } };
            _markRaid = new Button(() => PortalMarking.Arm(PortalMark.Raid)) { text = "Mark raid target", name = "mark-raid" };
            _markRally = new Button(() => PortalMarking.Arm(PortalMark.Rally)) { text = "Set rally point", name = "mark-rally" };
            _markRaid.AddTooltip(TooltipText.Get(TooltipKey.PortalMarkRaid));
            _markRally.AddTooltip(TooltipText.Get(TooltipKey.PortalMarkRally));
            marks.Add(_markRaid);
            marks.Add(_markRally);
            Add(marks);
        }

        /// <summary>Shows <paramref name="portal"/> (null hides the panel).</summary>
        public void Show(UnitProducer portal)
        {
            _portal = portal;
            style.display = portal != null ? DisplayStyle.Flex : DisplayStyle.None;
            if (portal != null) Refresh();
        }

        /// <summary>Redraws the state: the option in force, the number of demons, and what the next click sets.</summary>
        public void Refresh()
        {
            if (_portal == null) return;
            for (int i = 0; i < _orders.Length; i++) _orders[i].SetEnabled(_portal.Order != (PortalOrder)i);
            for (int i = 0; i < _policies.Length; i++) _policies[i].SetEnabled(_portal.Policy != (BonesPolicy)i);
            _roster[0].SetEnabled(_portal.Roster != DemonType.Imp);
            _roster[1].SetEnabled(_portal.Roster != DemonType.BoneDemon);
            _markRaid.text = PortalMarking.Armed == PortalMark.Raid ? "Click the target (map: M)" : "Mark raid target";
            _markRally.text = PortalMarking.Armed == PortalMark.Rally ? "Click the rally point" : "Set rally point";
            string raid = _portal.HasRaidTarget ? $"raid {_portal.RaidTarget.x:0},{_portal.RaidTarget.y:0}" : "no raid target";
            _summary.text = $"Demons {_portal.Units.Count}  {raid}";
            var lines = new System.Text.StringBuilder();
            for (int i = 0; i < _portal.Units.Count && i < MaxDemonLines; i++)
            {
                DemonLoyalty d = DemonLoyalty.Of(_portal.Units[i].gameObject);
                if (d != null) lines.Append(i > 0 ? "\n" : string.Empty).Append(_portal.Units[i].Type == DemonType.BoneDemon ? "Bone demon" : "Imp").Append("  ").Append(d.Describe());
            }
            _demons.text = lines.ToString();
            _demons.style.display = lines.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void AddRow(string title, Button[] buttons, Array values, Action<object> pick, string prefix)
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, alignItems = Align.Center, marginTop = 2 } };
            row.Add(GameStyle.Label(title + " ", TextRole.Muted));
            int i = 0;
            foreach (object value in values)
            {
                object v = value;
                string text = v is DemonType.BoneDemon ? "Bone demon" : v.ToString();
                buttons[i] = new Button(() => pick(v)) { text = text, name = prefix + v };
                if (TryTooltip(prefix, v, out TooltipKey key)) buttons[i].AddTooltip(TooltipText.Get(key), text);
                row.Add(buttons[i++]);
            }
            Add(row);
        }

        private static bool TryTooltip(string prefix, object value, out TooltipKey key)
        {
            key = default;
            if (prefix == "order-" && value is PortalOrder order)
            {
                key = order switch
                {
                    PortalOrder.Guard => TooltipKey.PortalOrderGuard,
                    PortalOrder.Raid => TooltipKey.PortalOrderRaid,
                    _ => TooltipKey.PortalOrderFollow,
                };
                return true;
            }

            if (prefix == "bones-" && value is BonesPolicy policy)
            {
                key = policy switch
                {
                    BonesPolicy.Harvest => TooltipKey.PortalBonesHarvest,
                    BonesPolicy.Destroy => TooltipKey.PortalBonesDestroy,
                    _ => TooltipKey.PortalBonesIgnore,
                };
                return true;
            }

            if (prefix == "roster-" && value is DemonType type)
            {
                key = type == DemonType.BoneDemon ? TooltipKey.PortalSummonBoneDemon : TooltipKey.PortalSummonImp;
                return true;
            }

            return false;
        }
    }
}
