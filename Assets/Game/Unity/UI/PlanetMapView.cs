using System;
using System.Collections.Generic;
using System.Linq;
using Blondie.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Isekai
{
    /// <summary>
    /// Draws a <see cref="PlanetRun"/> over whatever room it is given: Regions as icons in rings around the Capital, joined by
    /// their links, with the Region's name under each and the icon's frame saying its state (captured green, open yellow, the
    /// current one blue, the rest grey; the Capital has its own icon). In pick mode clicking an available Region calls back;
    /// hovering explains it.
    /// </summary>
    public sealed class PlanetMapView : VisualElement
    {
        private static readonly Color Captured = new(0.35f, 0.75f, 0.35f);
        private static readonly Color Open = new(0.95f, 0.8f, 0.3f);
        private static readonly Color Locked = new(0.35f, 0.33f, 0.36f);
        private static readonly Color CurrentColor = new(0.35f, 0.6f, 1f);

        private const float NodeWidth = 220f;   // room for a name at the biggest text size
        private const float EdgeX = 110f, EdgeY = 70f;

        private readonly PlanetRun _run;
        private readonly Action<RegionInfo> _onPick;
        private readonly Dictionary<string, VisualElement> _places = new();
        private readonly Dictionary<string, IconButton> _nodes = new();
        private readonly Dictionary<string, VisualElement> _escaped = new();

        public PlanetMapView(PlanetRun run, Action<RegionInfo> onPick)
        {
            _run = run;
            _onPick = onPick;
            style.flexGrow = 1;
            style.alignSelf = Align.Stretch;
            generateVisualContent += DrawLinks;

            foreach (RegionInfo region in run.Planet.Regions)
            {
                RegionInfo r = region;
                var place = new VisualElement { pickingMode = PickingMode.Ignore, style = { position = Position.Absolute, width = NodeWidth, alignItems = Align.Center } };
                string name = r.IsCapital ? "Capital" : r.Name;
                var node = new IconButton(GameIcons.Named(r.IsCapital ? "Capital" : "Region"), name, () => Pick(r)) { name = "region-" + r.Id };
                node.AddToClassList("isekai-region");
                place.Add(node);
                var under = new VisualElement { pickingMode = PickingMode.Ignore, style = { flexDirection = FlexDirection.Row, alignItems = Align.Center } };
                if (node.HasIcon)
                {
                    Label label = GameStyle.Label(name);
                    label.pickingMode = PickingMode.Ignore;
                    label.style.whiteSpace = WhiteSpace.NoWrap;
                    under.Add(label);
                }
                VisualElement escaped = GameStyle.Icon(GameIcons.Named("RulerEscaped"));
                under.Add(escaped);
                place.Add(under);
                _nodes[r.Id] = node;
                _places[r.Id] = place;
                _escaped[r.Id] = escaped;
                Add(place);
            }
            RegisterCallback<GeometryChangedEvent>(_ => Arrange());
            Refresh();
        }

        public Button NodeFor(RegionInfo region) => _nodes.TryGetValue(region.Id, out IconButton b) ? b : null;

        public void Refresh()
        {
            var open = new HashSet<RegionInfo>(_run.Available());
            foreach (RegionInfo r in _run.Planet.Regions)
            {
                IconButton node = _nodes[r.Id];
                bool current = _run.Current == r && _run.State == PlanetRunState.InRegion;
                bool captured = _run.IsCaptured(r);
                Color c = captured ? Captured : current ? CurrentColor : open.Contains(r) ? Open : Locked;
                node.style.borderTopColor = node.style.borderBottomColor = node.style.borderLeftColor = node.style.borderRightColor = c;
                node.Tooltip = DescribeText(r);
                _places[r.Id].style.opacity = captured || current || open.Contains(r) ? 1f : 0.75f;   // out of reach: dimmed (a click does nothing)
                _escaped[r.Id].style.display = _run.EscapedRulersAt(r).Any() ? DisplayStyle.Flex : DisplayStyle.None;
            }
            MarkDirtyRepaint();
        }

        private void Arrange()
        {
            foreach (RegionInfo r in _run.Planet.Regions)
            {
                Vector2 at = PointOf(r);
                VisualElement place = _places[r.Id];
                place.style.left = Mathf.Round(at.x - NodeWidth * 0.5f);
                place.style.top = Mathf.Round(at.y - 21f);   // the icon's middle sits on the point
            }
            MarkDirtyRepaint();
        }

        private void Pick(RegionInfo region)
        {
            if (_onPick != null && _run.CanEnter(region)) _onPick(region);
        }

        private string DescribeText(RegionInfo r)
        {
            string state = _run.IsCaptured(r) ? "captured" : _run.Available().Contains(r) ? "open" : "out of reach";
            string buff = string.IsNullOrEmpty(r.BuffId) ? "" : "  -  " + RegionBuffs.Describe(r.BuffId);
            string escaped = EscapeText(r);
            return r.IsCapital ? "The Capital: capture it to win the Run" + escaped : $"{r.Name}  -  depth {r.Depth}  -  {state}{buff}{escaped}";
        }

        private string EscapeText(RegionInfo region)
        {
            EscapedRuler latest = _run.EscapedRulersAt(region).LastOrDefault();
            if (latest == null) return string.Empty;
            string who = string.IsNullOrEmpty(latest.Name) ? "A ruler" : latest.Name;
            string traits = latest.Traits == null || latest.Traits.Length == 0
                ? "traits unknown"
                : string.Join(", ", latest.Traits.Select(t => TraitTable.Get(t).Name));
            return $"  -  Escaped ruler: {who} ({traits})";
        }

        private Vector2 PointOf(RegionInfo r)
        {
            Vector2 half = contentRect.size * 0.5f;
            float reachX = Mathf.Max(0f, half.x - EdgeX), reachY = Mathf.Max(0f, half.y - EdgeY);
            return new Vector2(Mathf.Round(half.x + Mathf.Cos(r.Angle) * r.Radius * reachX), Mathf.Round(half.y + Mathf.Sin(r.Angle) * r.Radius * reachY));
        }

        private void DrawLinks(MeshGenerationContext context)
        {
            Painter2D p = context.painter2D;
            p.lineWidth = 2f;
            foreach (RegionInfo a in _run.Planet.Regions)
                foreach (RegionInfo b in a.Neighbours.Select(id => _run.Planet[id]))
                {
                    if (string.CompareOrdinal(a.Id, b.Id) >= 0) continue;   // each link once
                    bool walked = _run.IsCaptured(a) || _run.IsCaptured(b);
                    p.strokeColor = walked ? new Color(0.6f, 0.85f, 0.6f, 0.8f) : new Color(1f, 1f, 1f, 0.25f);
                    p.BeginPath();
                    p.MoveTo(PointOf(a));
                    p.LineTo(PointOf(b));
                    p.Stroke();
                }
        }
    }
}
