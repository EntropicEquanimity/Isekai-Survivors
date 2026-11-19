using Blondie.FlowFields;
using Blondie.Core;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// What an <see cref="IMapPainter"/> draws onto: the Region map's pixels (<see cref="PixelsPerCell"/> per ground
    /// cell, y up), with world and cell to pixel conversions and a few shape helpers. Out-of-map pixels are ignored.
    /// </summary>
    public sealed class MapCanvas
    {
        public const int PixelsPerCell = RegionMapView.PixelsPerCell;

        private static readonly Color32 Ink = new(10, 10, 12, 255);

        private readonly Color32[] _pixels;
        private readonly Vector2 _origin;

        public MapCanvas(FlowFieldGrid grid, Color32[] pixels)
        {
            Grid = grid;
            _pixels = pixels;
            Width = grid.Width * PixelsPerCell;
            Height = grid.Height * PixelsPerCell;
            Float3 first = grid.CellToWorld(new Int2(0, 0));
            _origin = new Vector2(first.x - grid.CellSize * 0.5f, first.y - grid.CellSize * 0.5f);
        }

        /// <summary>The Region's ground grid.</summary>
        public FlowFieldGrid Grid { get; }

        /// <summary>Width and height in pixels.</summary>
        public int Width { get; }
        public int Height { get; }

        public void Set(int x, int y, Color32 colour)
        {
            if (InMap(x, y)) _pixels[x + y * Width] = colour;
        }

        public bool InMap(int x, int y) => (uint)x < (uint)Width && (uint)y < (uint)Height;

        public Vector2Int ToPixel(Vector3 world) => new(
            Mathf.FloorToInt((world.x - _origin.x) / Grid.CellSize * PixelsPerCell),
            Mathf.FloorToInt((world.y - _origin.y) / Grid.CellSize * PixelsPerCell));

        /// <summary>The world point under a map pixel (the inverse of <see cref="ToPixel"/>, y up).</summary>
        public Vector3 ToWorld(Vector2 pixel) => new(
            _origin.x + pixel.x / PixelsPerCell * Grid.CellSize,
            _origin.y + pixel.y / PixelsPerCell * Grid.CellSize,
            0f);

        /// <summary>Fills the pixels of one ground cell.</summary>
        public void Cell(Int2 cell, Color32 colour)
        {
            for (int y = 0; y < PixelsPerCell; y++)
            for (int x = 0; x < PixelsPerCell; x++)
                Set(cell.x * PixelsPerCell + x, cell.y * PixelsPerCell + y, colour);
        }

        public void Rect(int x0, int y0, int w, int h, Color32 colour)
        {
            for (int y = y0; y < y0 + h; y++)
            for (int x = x0; x < x0 + w; x++) Set(x, y, colour);
        }

        public void Square(Vector2Int centre, int size, Color32 colour) => Rect(centre.x - size / 2, centre.y - size / 2, size, size, colour);

        /// <summary>A square with a dark edge.</summary>
        public void Outlined(Vector2Int centre, int size, Color32 colour)
        {
            Square(centre, size + 2, Ink);
            Square(centre, size, colour);
        }

        /// <summary>A pixel icon (rows of '#', top row first) with a dark edge.</summary>
        public void Icon(Vector2Int centre, string[] rows, Color32 colour)
        {
            int h = rows.Length, w = rows[0].Length;
            int left = centre.x - w / 2, top = centre.y + h / 2;
            for (int pass = 0; pass < 2; pass++)
            for (int r = 0; r < h; r++)
            for (int col = 0; col < w; col++)
            {
                if (rows[r][col] != '#') continue;
                int x = left + col, y = top - r;
                if (pass == 0) { Set(x - 1, y, Ink); Set(x + 1, y, Ink); Set(x, y - 1, Ink); Set(x, y + 1, Ink); }   // a dark edge first
                else Set(x, y, colour);
            }
        }
    }
}
