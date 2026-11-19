using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Isekai
{
    /// <summary>
    /// Draws a <see cref="PlanetRun"/>: Regions as buttons in rings around the Capital, joined by their links.
    /// Captured Regions are green, ones that may be entered yellow, the current one blue, the rest grey; the Capital is
    /// red. In pick mode clicking an available Region calls back; hovering shows its name, depth and buff.
    /// </summary>
    public sealed class PlanetMapView : VisualElement
    {
        private static readonly Color Captured = new(0.35f, 0.75f, 0.35f);
        private static readonly Color Open = new(0.95f, 0.8f, 0.3f);
        private static readonly Color Locked = new(0.27f, 0.27f, 0.31f);
        private static readonly Color CurrentColor = new(0.35f, 0.6f, 1f);
        private static readonly Color CapitalColor = new(0.85f, 0.25f, 0.25f);

        /// <summary>The bundled pixel font's native size; text sizes must be whole multiples of it.</summary>

        private readonly PlanetRun _run;
        private readonly Action<RegionInfo> _onPick;
        private readonly Vector2 _size;
        private readonly Dictionary<string, Button> _nodes = new();
        private readonly Label _info;
        private const int InfoLineHeight = 26;

        /// <param name="size">Width and height in UI pixels. The rings are drawn as an ellipse filling it: the boxes are wide.</param>
        public PlanetMapView(PlanetRun run, Vector2 size, Action<RegionInfo> onPick)
        {
            _run = run;
            _onPick = onPick;
            _size = size;
            style.width = size.x;
            style.height = size.y;
            style.flexShrink = 0;
            style.marginBottom = InfoLineHeight;   // the hover line sits in this space, below the map
            generateVisualContent += DrawLinks;

            foreach (RegionInfo region in run.Planet.Regions)
            {
                RegionInfo r = region;
                var node = new Button(() => Pick(r)) { text = r.IsCapital ? "Capital" : r.Name, name = "region-" + r.Id };
                float w = r.IsCapital ? 72f : 96f;
                Vector2 at = PointOf(r);
                node.style.position = Position.Absolute;
                node.style.left = Mathf.Round(at.x - w * 0.5f);
                node.style.top = Mathf.Round(at.y - 10f);
                node.style.width = w;
                node.style.height = 20f;
                node.style.fontSize = GameStyle.FontSize;   // the pixel font's native size: anything else blurs
                node.style.whiteSpace = WhiteSpace.NoWrap;
                node.style.paddingLeft = 0;
                node.style.paddingRight = 0;
                node.style.marginLeft = 0;
                node.style.marginRight = 0;
                node.RegisterCallback<PointerEnterEvent>(_ => Describe(r));
                _nodes[r.Id] = node;
                Add(node);
            }
            _info = new Label { pickingMode = PickingMode.Ignore, style = { fontSize = GameStyle.FontSize, position = Position.Absolute, left = 0, right = 0, bottom = -InfoLineHeight + 2, unityTextAlign = TextAnchor.MiddleCenter, color = Color.white } };
            Add(_info);
            Refresh();
        }

        public Button NodeFor(RegionInfo region) => _nodes.TryGetValue(region.Id, out Button b) ? b : null;

        public void Refresh()
        {
            var open = new HashSet<RegionInfo>(_run.Available());
            foreach (RegionInfo r in _run.Planet.Regions)
            {
                Button node = _nodes[r.Id];
                bool current = _run.Current == r && _run.State == PlanetRunState.InRegion;
                Color c = _run.IsCaptured(r) ? Captured : current ? CurrentColor : open.Contains(r) ? Open : Locked;
                node.style.backgroundColor = c;
                node.style.color = c == Locked ? new Color(0.75f, 0.75f, 0.8f) : Color.black;
                Color border = r.IsCapital ? CapitalColor : new Color(0f, 0f, 0f, 0.6f);
                node.style.borderTopColor = border;
                node.style.borderBottomColor = border;
                node.style.borderLeftColor = border;
                node.style.borderRightColor = border;
                float bw = r.IsCapital ? 2f : 1f;
                node.style.borderTopWidth = bw;
                node.style.borderBottomWidth = bw;
                node.style.borderLeftWidth = bw;
                node.style.borderRightWidth = bw;
                // Never disabled: a disabled button is drawn see-through. Clicking a locked Region simply does nothing.
                node.style.opacity = 1f;
            }
            MarkDirtyRepaint();
        }

        private void Pick(RegionInfo region)
        {
            if (_onPick != null && _run.CanEnter(region)) _onPick(region);
        }

        private void Describe(RegionInfo r)
        {
            string state = _run.IsCaptured(r) ? "captured" : _run.Available().Contains(r) ? "open" : "out of reach";
            string buff = string.IsNullOrEmpty(r.BuffId) ? "" : "  -  " + RegionBuffs.Describe(r.BuffId);
            _info.text = r.IsCapital ? "The Capital: capture it to win the Run" : $"{r.Name}  -  depth {r.Depth}  -  {state}{buff}";
        }

        private Vector2 PointOf(RegionInfo r)
        {
            Vector2 half = _size * 0.5f;
            float reachX = half.x - 52f, reachY = half.y - 14f;
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
