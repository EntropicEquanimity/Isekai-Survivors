using Blondie.Core;
using Blondie.FlowFields;
using UnityEngine;

namespace Isekai
{
    /// <summary>The map's ground: open ground, and rock where the flow field is blocked.</summary>
    [RegionSystem]
    public sealed class GroundPainter : MonoBehaviour, IMapPainter
    {
        private static readonly Color32 Ground = new(56, 82, 38, 255);
        private static readonly Color32 Rock = new(118, 116, 110, 255);

        public int Order => MapOrder.Ground;

        private void OnEnable() => MapPainters.Add(this);

        private void OnDisable() => MapPainters.Remove(this);

        public void Paint(MapCanvas canvas)
        {
            FlowFieldGrid grid = canvas.Grid;
            for (int cy = 0; cy < grid.Height; cy++)
            for (int cx = 0; cx < grid.Width; cx++)
            {
                var cell = new Int2(cx, cy);
                canvas.Cell(cell, grid.GetCost(cell) == FlowFieldGrid.Blocked ? Rock : Ground);
            }
        }
    }
}
