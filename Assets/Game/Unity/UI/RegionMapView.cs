using System.Collections.Generic;
using Blondie.Core;
using Blondie.FlowFields;
using UnityEngine;
using UnityEngine.UIElements;

namespace Isekai
{
    /// <summary>
    /// The strategic map of the Region: a small point-filtered texture, a few pixels per ground cell, redrawn on
    /// <see cref="Redraw"/>. Ground and rock, the Bases' build areas, Stone as dots, Buildings as pixel
    /// icons, enemies as red squares (darker while roaming, bigger for elites), Militia blue, the Character white, and
    /// the camera's view as an outline.
    /// </summary>
    public sealed class RegionMapView : VisualElement
    {
        public const int PixelsPerCell = 2;

        private static readonly Color32 Outside = new(8, 9, 12, 255);
        private static readonly Color32 Ground = new(56, 82, 38, 255);
        private static readonly Color32 Rock = new(118, 116, 110, 255);
        private static readonly Color32 Stone = new(196, 196, 186, 255);
        public static readonly Color32 Roamer = new(185, 55, 55, 255);
        public static readonly Color32 Attacker = new(255, 72, 48, 255);
        public static readonly Color32 Militia = new(90, 150, 255, 255);
        public static readonly Color32 You = new(255, 255, 255, 255);
        public static readonly Color32 Ally = new(120, 220, 120, 255);
        public static readonly Color32 Home = new(255, 214, 90, 255);
        public static readonly Color32 Hostile = new(235, 70, 60, 255);
        private static readonly Color32 Footprint = new(40, 70, 46, 255);
        private static readonly Color32 Ink = new(10, 10, 12, 255);
        private static readonly Color32 View = new(235, 235, 240, 255);

        /// <summary>7×7 pixel icons, top row first.</summary>
        public static readonly IReadOnlyDictionary<string, string[]> Icons = new Dictionary<string, string[]>
        {
            ["Main Base"] = new[] { "...#...", "..###..", ".#####.", "#######", ".#...#.", ".#.#.#.", ".#####." },
            ["Portal"] = new[] { "#.....#", ".#...#.", "..#.#..", "...#...", "..#.#..", ".#...#.", "#.....#" },
            ["Spire"] = new[] { "#.#.#.#", "#######", ".#####.", ".##.##.", ".#####.", ".#####.", "#######" },
            ["Outpost"] = new[] { ".####..", ".#####.", ".####..", ".#.....", ".#.....", ".#.....", "###...." },
            ["Building"] = new[] { "#######", "#.....#", "#.....#", "#.....#", "#.....#", "#.....#", "#######" },
        };

        private readonly FlowFieldGrid _grid;
        private readonly Texture2D _texture;
        private readonly Color32[] _pixels;
        private readonly int _width, _height;
        private readonly Vector2 _origin;
        private readonly List<HordeMember> _elites = new();

        public Texture2D Texture => _texture;

        public RegionMapView(FlowFieldGrid grid)
        {
            _grid = grid;
            _width = grid.Width * PixelsPerCell;
            _height = grid.Height * PixelsPerCell;
            Float3 first = grid.CellToWorld(new Int2(0, 0));
            _origin = new Vector2(first.x - grid.CellSize * 0.5f, first.y - grid.CellSize * 0.5f);
            _pixels = new Color32[_width * _height];
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

        /// <summary>Releases the texture.</summary>
        public void Dispose() => Object.Destroy(_texture);

        /// <summary>The map pixel over a world point (for tests).</summary>
        public Color32 PixelAt(Vector3 world)
        {
            Vector2Int p = ToPixel(world);
            return InMap(p.x, p.y) ? _pixels[p.x + p.y * _width] : Outside;
        }

        public void Redraw()
        {
            DrawGround();
            foreach (ResourceNode node in ResourceNode.All)
                if (!node.IsEmpty) Square(ToPixel(node.transform.position), 2, Stone);
            foreach (Building b in Building.All)
                if (b.IsAlive) DrawBuilding(b);

            _elites.Clear();
            if (HordeDirector.Current != null)
            {
                // Roamers first so attackers draw over them; elites last and bigger.
                foreach (HordeMember m in HordeDirector.Current.Members)
                    if (m.IsAlive && m.IsRoaming) { if (m.IsElite) _elites.Add(m); else Square(ToPixel(m.transform.position), 2, Roamer); }
                foreach (HordeMember m in HordeDirector.Current.Members)
                    if (m.IsAlive && !m.IsRoaming) { if (m.IsElite) _elites.Add(m); else Square(ToPixel(m.transform.position), 2, Attacker); }
            }
            foreach (HordeMember m in _elites) Outlined(ToPixel(m.transform.position), 4, m.IsRoaming ? Roamer : Attacker);
            foreach (DefenderBrain unit in Object.FindObjectsByType<DefenderBrain>(FindObjectsInactive.Exclude))
                Square(ToPixel(unit.transform.position), 2, Militia);
            CharacterControl character = Player.Character;
            if (character != null && !character.IsDead) Outlined(ToPixel(character.transform.position), 4, You);
            DrawView();

            _texture.SetPixels32(_pixels);
            _texture.Apply(false);
        }

        private void DrawGround()
        {
            for (int cy = 0; cy < _grid.Height; cy++)
            for (int cx = 0; cx < _grid.Width; cx++)
            {
                Color32 c = _grid.GetCost(new Int2(cx, cy)) == FlowFieldGrid.Blocked ? Rock : Ground;
                for (int y = 0; y < PixelsPerCell; y++)
                for (int x = 0; x < PixelsPerCell; x++)
                    _pixels[cx * PixelsPerCell + x + (cy * PixelsPerCell + y) * _width] = c;
            }
        }

        private void DrawBuilding(Building b)
        {
            Vector2Int size = b.Asset != null ? b.Asset.Size : new Vector2Int(1, 1);
            Vector2Int centre = ToPixel(b.transform.position);
            int w = size.x * PixelsPerCell, h = size.y * PixelsPerCell;
            Rect(centre.x - w / 2, centre.y - h / 2, w, h, Footprint);
            string name = b.Asset != null ? b.Asset.DisplayName : "Building";
            Color32 colour = !b.IsPlayers ? Hostile : b.IsMainBase ? Home : Ally;   // a defender's Building reads as hostile
            if (!b.IsBuilt) colour = new Color32((byte)(colour.r / 2), (byte)(colour.g / 2), (byte)(colour.b / 2), 255);
            Icon(centre, Icons.TryGetValue(name, out string[] icon) ? icon : Icons["Building"], colour);
        }

        // The camera's view rectangle.
        private void DrawView()
        {
            Camera cam = Camera.main;
            if (cam == null || !cam.orthographic) return;
            float halfH = cam.orthographicSize, halfW = halfH * cam.aspect;
            Vector3 c = cam.transform.position;
            Vector2Int a = ToPixel(new Vector3(c.x - halfW, c.y - halfH, 0f)), b = ToPixel(new Vector3(c.x + halfW, c.y + halfH, 0f));
            for (int x = a.x; x <= b.x; x++) { Set(x, a.y, View); Set(x, b.y, View); }
            for (int y = a.y; y <= b.y; y++) { Set(a.x, y, View); Set(b.x, y, View); }
        }

        private void Icon(Vector2Int centre, string[] rows, Color32 colour)
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

        private void Outlined(Vector2Int centre, int size, Color32 colour)
        {
            Square(centre, size + 2, Ink);
            Square(centre, size, colour);
        }

        private void Square(Vector2Int centre, int size, Color32 colour) => Rect(centre.x - size / 2, centre.y - size / 2, size, size, colour);

        private void Rect(int x0, int y0, int w, int h, Color32 colour)
        {
            for (int y = y0; y < y0 + h; y++)
            for (int x = x0; x < x0 + w; x++) Set(x, y, colour);
        }

        private void Set(int x, int y, Color32 colour)
        {
            if (InMap(x, y)) _pixels[x + y * _width] = colour;
        }

        private bool InMap(int x, int y) => (uint)x < (uint)_width && (uint)y < (uint)_height;

        private Vector2Int ToPixel(Vector3 world) => new(
            Mathf.FloorToInt((world.x - _origin.x) / _grid.CellSize * PixelsPerCell),
            Mathf.FloorToInt((world.y - _origin.y) / _grid.CellSize * PixelsPerCell));

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
