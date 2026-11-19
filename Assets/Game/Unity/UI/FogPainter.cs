using Blondie.Core;
using Blondie.FlowFields;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// The map screen's fog of war, with the same rules as the world: units not on your side are drawn only while your
    /// side sees their cell, a destroyed Building stays until its ground is seen again, and ground your side has never
    /// seen is black. It draws over the painters before it, so it erases hidden units' pixels and blacks out unseen cells.
    /// </summary>
    [RegionSystem]
    public sealed class FogPainter : MonoBehaviour, IMapPainter
    {
        private static readonly Color32 Unseen = new(8, 9, 12, 255);
        private static readonly Color32 Ground = new(56, 82, 38, 255);   // as GroundPainter draws it
        private static readonly Color32 Rock = new(118, 116, 110, 255);
        private static readonly Color32 Footprint = new(40, 70, 46, 255);

        public int Order => MapOrder.Overlays;

        private void OnEnable() => MapPainters.Add(this);

        private void OnDisable() => MapPainters.Remove(this);

        public void Paint(MapCanvas canvas)
        {
            RegionVision vision = RegionVision.Current;
            if (vision == null || !vision.IsReady || !RegionVision.IsOn) return;
            EraseHiddenUnits(canvas, vision);
            DrawGhosts(canvas);
            BlackOutUnseen(canvas, vision);
        }

        // A unit is drawn as a square up to 6 pixels across (an elite's outline): put the ground back under it.
        private static void EraseHiddenUnits(MapCanvas canvas, RegionVision vision)
        {
            var living = RegionEntities.Living;
            for (int i = 0; i < living.Count; i++)
            {
                RegionEntity unit = living[i];
                if (unit.Kind == UnitKind.Building || unit.Side == Side.Yours || vision.CanSee(Side.Yours, unit.Cell)) continue;
                Vector2Int p = canvas.ToPixel(unit.GameObject.transform.position);
                for (int y = p.y - 3; y < p.y + 3; y++)
                for (int x = p.x - 3; x < p.x + 3; x++)
                {
                    if (!canvas.InMap(x, y)) continue;
                    var cell = new Int2(x / MapCanvas.PixelsPerCell, y / MapCanvas.PixelsPerCell);
                    if (vision.StateOf(Side.Yours, cell) == FogState.Visible) continue;   // a neighbour cell in sight keeps what it shows
                    canvas.Set(x, y, canvas.Grid.GetCost(cell) == FlowFieldGrid.Blocked ? Rock : Ground);
                }
            }
        }

        private static void DrawGhosts(MapCanvas canvas)
        {
            FogUnits units = FogUnits.Current;
            if (units == null) return;
            foreach (FogUnits.Ghost g in units.Ghosts)
            {
                Vector2Int centre = canvas.ToPixel(g.Position);
                int w = g.Size.x * MapCanvas.PixelsPerCell, h = g.Size.y * MapCanvas.PixelsPerCell;
                canvas.Rect(centre.x - w / 2, centre.y - h / 2, w, h, Footprint);
                canvas.Icon(centre, RegionMapView.Icons.TryGetValue(g.Name, out string[] icon) ? icon : RegionMapView.Icons["Building"], g.Players ? RegionMapView.Ally : RegionMapView.Hostile);
            }
        }

        private static void BlackOutUnseen(MapCanvas canvas, RegionVision vision)
        {
            FlowFieldGrid grid = canvas.Grid;
            for (int cy = 0; cy < grid.Height; cy++)
            for (int cx = 0; cx < grid.Width; cx++)
            {
                var cell = new Int2(cx, cy);
                if (vision.StateOf(Side.Yours, cell) == FogState.Unseen) canvas.Cell(cell, Unseen);
            }
        }
    }
}
