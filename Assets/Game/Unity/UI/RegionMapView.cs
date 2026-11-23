using System.Collections.Generic;
using Blondie.Core;
using Blondie.FlowFields;
using UnityEngine;
using UnityEngine.UIElements;

namespace Isekai
{
    /// <summary>
    /// The strategic map of the Region: a small point-filtered texture, a few pixels per ground cell, redrawn on
    /// <see cref="Redraw"/> by the registered <see cref="IMapPainter"/>s in order: ground and rock, Stone and
    /// Buildings, mortals as red squares (darker while roaming, bigger for elites), demons blue, the Character white,
    /// and the camera's view as an outline.
    /// </summary>
    public sealed class RegionMapView : VisualElement
    {
        public const int PixelsPerCell = 2;

        private static readonly Color32 Outside = new(8, 9, 12, 255);
        public static readonly Color32 Roamer = new(185, 55, 55, 255);
        public static readonly Color32 Attacker = new(255, 72, 48, 255);
        public static readonly Color32 Demon = new(90, 150, 255, 255);
        public static readonly Color32 You = new(255, 255, 255, 255);
        public static readonly Color32 Ally = new(120, 220, 120, 255);
        public static readonly Color32 Home = new(255, 214, 90, 255);
        public static readonly Color32 Hostile = new(235, 70, 60, 255);

        /// <summary>7×7 pixel icons, top row first.</summary>
        public static readonly IReadOnlyDictionary<string, string[]> Icons = new Dictionary<string, string[]>
        {
            ["Main Base"] = new[] { "...#...", "..###..", ".#####.", "#######", ".#...#.", ".#.#.#.", ".#####." },
            ["Portal"] = new[] { "#.....#", ".#...#.", "..#.#..", "...#...", "..#.#..", ".#...#.", "#.....#" },
            ["Spire"] = new[] { "#.#.#.#", "#######", ".#####.", ".##.##.", ".#####.", ".#####.", "#######" },
            ["Outpost"] = new[] { ".####..", ".#####.", ".####..", ".#.....", ".#.....", ".#.....", "###...." },
            ["Building"] = new[] { "#######", "#.....#", "#.....#", "#.....#", "#.....#", "#.....#", "#######" },
        };

        private readonly Texture2D _texture;
        private readonly Color32[] _pixels;
        private readonly MapCanvas _canvas;
        private readonly int _width, _height;
        private int _scale = 1;

        public Texture2D Texture => _texture;

        public RegionMapView(FlowFieldGrid grid)
        {
            _width = grid.Width * PixelsPerCell;
            _height = grid.Height * PixelsPerCell;
            _pixels = new Color32[_width * _height];
            _canvas = new MapCanvas(grid, _pixels);
            _texture = new Texture2D(_width, _height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "Region Map" };
            style.width = _width;
            style.height = _height;
            style.flexShrink = 0;
            style.backgroundImage = Background.FromTexture2D(_texture);
            style.borderTopWidth = style.borderBottomWidth = style.borderLeftWidth = style.borderRightWidth = 1;
            style.borderTopColor = style.borderBottomColor = style.borderLeftColor = style.borderRightColor = GameStyle.Border;
            pickingMode = PickingMode.Ignore;
            Redraw();
        }

        /// <summary>Shows the map at the biggest whole scale that fits <paramref name="room"/>.</summary>
        public void Fit(Vector2 room)
        {
            _scale = Mathf.Max(1, Mathf.FloorToInt(Mathf.Min(room.x / _width, room.y / _height)));
            style.width = _width * _scale;
            style.height = _height * _scale;
        }

        /// <summary>Releases the texture.</summary>
        public void Dispose() => Object.Destroy(_texture);

        /// <summary>The map pixel over a world point (for tests).</summary>
        public Color32 PixelAt(Vector3 world)
        {
            Vector2Int p = _canvas.ToPixel(world);
            return InMap(p.x, p.y) ? _pixels[p.x + p.y * _width] : Outside;
        }

        /// <summary>The world point under a screen position when it is over the map (a click on the map screen).</summary>
        public bool TryWorldAt(Vector2 screen, out Vector3 world)
        {
            world = default;
            if (panel == null) return false;
            Vector2 inPanel = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(screen.x, Screen.height - screen.y));
            Vector2 local = this.WorldToLocal(inPanel);
            float x = (local.x - resolvedStyle.borderLeftWidth) / _scale, y = (local.y - resolvedStyle.borderTopWidth) / _scale;
            if (x < 0f || y < 0f || x >= _width || y >= _height) return false;
            world = _canvas.ToWorld(new Vector2(x, _height - y));
            return true;
        }

        /// <summary>Runs every registered <see cref="IMapPainter"/> in order and uploads the pixels.</summary>
        public void Redraw()
        {
            foreach (IMapPainter painter in MapPainters.All) painter.Paint(_canvas);
            _texture.SetPixels32(_pixels);
            _texture.Apply(false);
        }

        private bool InMap(int x, int y) => (uint)x < (uint)_width && (uint)y < (uint)_height;

        /// <summary>A small texture of one icon (for the legend), each icon pixel drawn <paramref name="scale"/> wide.</summary>
        public static Texture2D IconTexture(string[] rows, Color32 colour, int scale = 2)
        {
            int h = rows.Length, w = rows[0].Length;
            var tex = new Texture2D(w * scale, h * scale, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var px = new Color32[w * h * scale * scale];
            for (int r = 0; r < h; r++)
            for (int c = 0; c < w; c++)
            for (int y = 0; y < scale; y++)
            for (int x = 0; x < scale; x++)
                px[c * scale + x + ((h - 1 - r) * scale + y) * w * scale] = rows[r][c] == '#' ? colour : new Color32(0, 0, 0, 0);
            tex.SetPixels32(px);
            tex.Apply(false);
            return tex;
        }
    }
}
