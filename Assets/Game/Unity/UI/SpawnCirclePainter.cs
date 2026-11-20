using UnityEngine;

namespace Isekai
{
    /// <summary>The spawn circles Jack's side has discovered, marked on the map for good (until one is destroyed).</summary>
    [RegionSystem]
    public sealed class SpawnCirclePainter : MonoBehaviour, IMapPainter
    {
        private static readonly Color32 Mark = new(214, 96, 255, 255);

        // A ring with a dot in it.
        private static readonly string[] Icon =
        {
            "..###..",
            ".#...#.",
            "#..#..#",
            ".#...#.",
            "..###..",
        };

        public int Order => MapOrder.Places + 10;

        private void OnEnable() => MapPainters.Add(this);

        private void OnDisable() => MapPainters.Remove(this);

        public void Paint(MapCanvas canvas)
        {
            VigilanceDirector director = VigilanceDirector.Current;
            if (director == null) return;
            foreach (VigilanceSpawnCircle circle in director.Circles)
                if (circle != null && circle.IsAlive && circle.Discovered)
                    canvas.Icon(canvas.ToPixel(circle.transform.position), Icon, Mark);
        }
    }
}
