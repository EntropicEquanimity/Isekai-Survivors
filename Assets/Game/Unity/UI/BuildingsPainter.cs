using UnityEngine;

namespace Isekai
{
    /// <summary>What stands on the ground: Resource nodes as dots, and Buildings as pixel icons on their footprint.</summary>
    [RegionSystem]
    public sealed class BuildingsPainter : MonoBehaviour, IMapPainter
    {
        private static readonly Color32 Stone = new(196, 196, 186, 255);
        private static readonly Color32 Footprint = new(40, 70, 46, 255);

        public int Order => MapOrder.Places;

        private void OnEnable() => MapPainters.Add(this);

        private void OnDisable() => MapPainters.Remove(this);

        public void Paint(MapCanvas canvas)
        {
            foreach (ResourceNode node in ResourceNode.All)
                if (!node.IsEmpty) canvas.Square(canvas.ToPixel(node.transform.position), 2, Stone);
            foreach (Building b in Building.All)
                if (b.IsAlive) Draw(canvas, b);
        }

        private static void Draw(MapCanvas canvas, Building b)
        {
            Vector2Int size = b.Asset != null ? b.Asset.Size : new Vector2Int(1, 1);
            Vector2Int centre = canvas.ToPixel(b.transform.position);
            int w = size.x * MapCanvas.PixelsPerCell, h = size.y * MapCanvas.PixelsPerCell;
            canvas.Rect(centre.x - w / 2, centre.y - h / 2, w, h, Footprint);
            string name = b.Asset != null ? b.Asset.DisplayName : "Building";
            Color32 colour = !b.IsPlayers ? RegionMapView.Hostile : b.IsMainBase ? RegionMapView.Home : RegionMapView.Ally;   // a defender's Building reads as hostile
            if (!b.IsBuilt) colour = new Color32((byte)(colour.r / 2), (byte)(colour.g / 2), (byte)(colour.b / 2), 255);
            canvas.Icon(centre, RegionMapView.Icons.TryGetValue(name, out string[] icon) ? icon : RegionMapView.Icons["Building"], colour);
        }
    }
}
