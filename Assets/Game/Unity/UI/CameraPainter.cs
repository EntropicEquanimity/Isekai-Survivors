using UnityEngine;

namespace Isekai
{
    /// <summary>The camera's view rectangle, drawn last.</summary>
    [RegionSystem]
    public sealed class CameraPainter : MonoBehaviour, IMapPainter
    {
        private static readonly Color32 View = new(235, 235, 240, 255);

        public int Order => MapOrder.Camera;

        private void OnEnable() => MapPainters.Add(this);

        private void OnDisable() => MapPainters.Remove(this);

        public void Paint(MapCanvas canvas)
        {
            Camera cam = Camera.main;
            if (cam == null || !cam.orthographic) return;
            float halfH = cam.orthographicSize, halfW = halfH * cam.aspect;
            Vector3 c = cam.transform.position;
            Vector2Int a = canvas.ToPixel(new Vector3(c.x - halfW, c.y - halfH, 0f)), b = canvas.ToPixel(new Vector3(c.x + halfW, c.y + halfH, 0f));
            for (int x = a.x; x <= b.x; x++) { canvas.Set(x, a.y, View); canvas.Set(x, b.y, View); }
            for (int y = a.y; y <= b.y; y++) { canvas.Set(a.x, y, View); canvas.Set(b.x, y, View); }
        }
    }
}
