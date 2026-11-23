using System;
using System.Collections.Generic;
using Blondie.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Isekai
{
    /// <summary>
    /// The Portal panel inside <see cref="BuildHud"/>'s selection box, all icons (names and explanations in the tooltips): the
    /// standing order (Guard, Raid, Follow), the bones policy (Harvest, Destroy, Ignore), the demon it summons (Imp, Bone
    /// demon), and the buttons that mark the raid target and the rally point (the next click in the world, or a click on the
    /// map screen for the target). What is in force is framed; a mark button is framed while the next click sets it. Under
    /// them, each demon with its level and loyalty. Nothing is rebuilt on a click.
    /// </summary>
    public sealed class PortalPanel : VisualElement
    {
        private const int MaxDemonLines = 8;

        private readonly Dictionary<PortalOrder, IconButton> _orders = new();
        private readonly Dictionary<BonesPolicy, IconButton> _policies = new();
        private readonly Dictionary<DemonType, IconButton> _roster = new();
        private readonly IconButton _markRaid, _markRally;
        private readonly VisualElement _rosterRow;
        private readonly VisualElement _demons;
        private readonly List<DemonLine> _lines = new();
        private UnitProducer _portal;

        private sealed class DemonLine
        {
            public VisualElement Root, Icon;
            public Label Level, Loyalty;
            public TooltipManipulator Tip;
        }

        public PortalPanel()
        {
            VisualElement orders = Row();
            foreach (PortalOrder order in Enum.GetValues(typeof(PortalOrder)))
            {
                PortalOrder o = order;
                _orders[o] = Option(orders, GameIcons.Named("Order_" + o), o.ToString(), "order-" + o, OrderTip(o), () => _portal.Order = o);
            }
            VisualElement policies = Row();
            foreach (BonesPolicy policy in Enum.GetValues(typeof(BonesPolicy)))
            {
                BonesPolicy p = policy;
                _policies[p] = Option(policies, GameIcons.Named("Bones_" + p), p + " bones", "bones-" + p, PolicyTip(p), () => _portal.Policy = p);
            }
            _rosterRow = Row();
            VisualElement marks = Row();
            _markRaid = Mark(marks, "Mark_Raid", "Raid target", "mark-raid", PortalMark.Raid);
            _markRally = Mark(marks, "Mark_Rally", "Rally point", "mark-rally", PortalMark.Rally);
            _demons = new VisualElement { name = "portal-demons", style = { marginTop = 2 } };
            Add(_demons);
        }

        /// <summary>Shows <paramref name="portal"/> (null hides the panel).</summary>
        public void Show(UnitProducer portal)
        {
            _portal = portal;
            style.display = portal != null ? DisplayStyle.Flex : DisplayStyle.None;
            if (portal == null) return;
            if (_roster.Count == 0) BuildRoster();
            Refresh();
        }

        /// <summary>Redraws the state: what is in force, what the next click sets, and the demons.</summary>
        public void Refresh()
        {
            if (_portal == null) return;
            foreach (KeyValuePair<PortalOrder, IconButton> b in _orders) b.Value.Picked = _portal.Order == b.Key;
            foreach (KeyValuePair<BonesPolicy, IconButton> b in _policies) b.Value.Picked = _portal.Policy == b.Key;
            foreach (KeyValuePair<DemonType, IconButton> b in _roster) b.Value.Picked = _portal.Roster == b.Key;
            _markRaid.Picked = PortalMarking.Armed == PortalMark.Raid;
            _markRally.Picked = PortalMarking.Armed == PortalMark.Rally;
            _markRaid.EnableInClassList("isekai-icon-button--set", _portal.HasRaidTarget);
            _markRally.EnableInClassList("isekai-icon-button--set", _portal.HasRally);

            int shown = 0;
            for (int i = 0; i < _portal.Units.Count && shown < MaxDemonLines; i++)
            {
                DemonLoyalty d = DemonLoyalty.Of(_portal.Units[i].gameObject);
                if (d != null) Fill(shown++, _portal.Units[i], d);
            }
            for (int i = 0; i < _lines.Count; i++) _lines[i].Root.style.display = i < shown ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void Fill(int index, DemonBrain demon, DemonLoyalty loyalty)
        {
            while (_lines.Count <= index) _lines.Add(NewLine());
            DemonLine line = _lines[index];
            GameStyle.SetIcon(line.Icon, GameIcons.Of(_portal.PrefabOf(demon.Type)), 24);
            string level = loyalty.Level.ToString(), left = Mathf.CeilToInt(loyalty.Loyalty).ToString();
            if (line.Level.text != level) line.Level.text = level;
            if (line.Loyalty.text != left) line.Loyalty.text = left;
            line.Loyalty.EnableInClassList("isekai-number--short", loyalty.Loyalty < 50f);
            line.Tip.Header = NameOf(demon.Type);
            line.Tip.Content = loyalty.Describe();
        }

        private DemonLine NewLine()
        {
            var line = new DemonLine { Root = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 2 } } };
            line.Root.Add(GameStyle.UnitIcon(null, 24, out line.Icon));
            VisualElement level = GameStyle.IconNumber(GameIcons.Named("Level"), "1", out line.Level, "Level");
            level.style.marginLeft = 6;
            line.Root.Add(level);
            VisualElement loyalty = GameStyle.IconNumber(GameIcons.Named("Loyalty"), "100", out line.Loyalty, "Loyalty");
            loyalty.style.marginLeft = 6;
            line.Root.Add(loyalty);
            line.Tip = line.Root.AddTooltip(string.Empty);
            _demons.Add(line.Root);
            return line;
        }

        private void BuildRoster()
        {
            foreach (DemonType type in new[] { DemonType.Imp, DemonType.BoneDemon })
            {
                DemonType t = type;
                TooltipKey tip = t == DemonType.BoneDemon ? TooltipKey.PortalSummonBoneDemon : TooltipKey.PortalSummonImp;
                _roster[t] = Option(_rosterRow, GameIcons.Of(_portal.PrefabOf(t)), NameOf(t), "roster-" + t, tip, () => _portal.Roster = t, unit: true);
            }
        }

        private VisualElement Row()
        {
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, alignItems = Align.Center } };
            Add(row);
            return row;
        }

        private IconButton Option(VisualElement row, Sprite icon, string label, string elementName, TooltipKey tip, Action pick, bool unit = false)
        {
            var button = new IconButton(icon, label, () =>
            {
                if (_portal == null) return;
                pick();
                Refresh();
            }, box: unit ? 56 : 32, lit: unit) { name = elementName, Tooltip = TooltipText.Get(tip) };
            button.style.marginRight = 2;
            row.Add(button);
            return button;
        }

        private IconButton Mark(VisualElement row, string icon, string label, string elementName, PortalMark mark)
        {
            var button = new IconButton(GameIcons.Named(icon), label, () =>
            {
                PortalMarking.Arm(mark);
                Refresh();
            }) { name = elementName, Tooltip = TooltipText.Get(mark == PortalMark.Raid ? TooltipKey.PortalMarkRaid : TooltipKey.PortalMarkRally) };
            button.style.marginRight = 2;
            row.Add(button);
            return button;
        }

        private static string NameOf(DemonType type) => type == DemonType.BoneDemon ? "Bone demon" : "Imp";

        private static TooltipKey OrderTip(PortalOrder order) => order switch
        {
            PortalOrder.Guard => TooltipKey.PortalOrderGuard,
            PortalOrder.Raid => TooltipKey.PortalOrderRaid,
            _ => TooltipKey.PortalOrderFollow,
        };

        private static TooltipKey PolicyTip(BonesPolicy policy) => policy switch
        {
            BonesPolicy.Harvest => TooltipKey.PortalBonesHarvest,
            BonesPolicy.Destroy => TooltipKey.PortalBonesDestroy,
            _ => TooltipKey.PortalBonesIgnore,
        };
    }
}
