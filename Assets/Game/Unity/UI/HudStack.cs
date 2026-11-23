using System.Collections.Generic;
using Blondie.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Isekai
{
    /// <summary>A place on the Region's HUD where boxes stack up.</summary>
    public enum HudCorner { TopLeft, TopCentre, TopRight, BottomLeft, BottomCentre, BottomRight }

    /// <summary>
    /// The Region HUD's layout: one column per corner (or edge) that every HUD box goes into, so boxes push each other along
    /// instead of drawing on top of one another, at every text size. A HUD builds its box and calls <see cref="Add"/>; it
    /// never places itself on screen.
    /// </summary>
    [RegionSystem]
    public sealed class HudStack : MonoBehaviour
    {
        private const string PanelName = "HUD";
        private const int Margin = 4;
        private const int Gap = 3;

        private readonly Dictionary<HudCorner, VisualElement> _columns = new();
        private readonly Dictionary<VisualElement, int> _orders = new();
        private VisualElement _root;

        private static HudStack s_current;

        private void Awake() => s_current = this;

        private void OnDestroy()
        {
            if (s_current == this) s_current = null;
        }

        /// <summary>The column at <paramref name="corner"/> (made when first needed), for tests and layout checks.</summary>
        public static VisualElement Column(HudCorner corner) => Ensure().ColumnAt(corner);

        /// <summary>Puts <paramref name="box"/> in a corner's column; lower <paramref name="order"/>s sit higher on screen.</summary>
        public static void Add(HudCorner corner, VisualElement box, int order)
        {
            HudStack stack = Ensure();
            VisualElement column = stack.ColumnAt(corner);
            stack._orders[box] = order;
            box.style.marginBottom = Gap;
            int at = 0;
            while (at < column.childCount && stack._orders.TryGetValue(column[at], out int other) && other <= order) at++;
            column.Insert(at, box);
        }

        private static HudStack Ensure()
        {
            if (s_current != null) return s_current;
            return new GameObject("HUD Stack").AddComponent<HudStack>();   // a Region without its systems (a test)
        }

        private VisualElement ColumnAt(HudCorner corner)
        {
            if (_columns.TryGetValue(corner, out VisualElement known)) return known;
            if (_root == null)
            {
                _root = GamePanels.Mount(this, PanelName, null, out PanelRenderer panel);
                UIPanels.Created(panel);
            }
            bool top = corner is HudCorner.TopLeft or HudCorner.TopCentre or HudCorner.TopRight;
            var column = new VisualElement { name = "hud-" + corner, pickingMode = PickingMode.Ignore };
            IStyle s = column.style;
            s.position = Position.Absolute;
            if (top) s.top = Margin;
            else s.bottom = Margin - Gap;
            switch (corner)
            {
                case HudCorner.TopLeft:
                case HudCorner.BottomLeft:
                    s.left = Margin;
                    s.alignItems = Align.FlexStart;
                    break;
                case HudCorner.TopRight:
                case HudCorner.BottomRight:
                    s.right = Margin;
                    s.alignItems = Align.FlexEnd;
                    break;
                default:
                    s.left = 0;
                    s.right = 0;
                    s.alignItems = Align.Center;
                    break;
            }
            _columns[corner] = column;
            _root.Add(column);
            return column;
        }
    }
}
