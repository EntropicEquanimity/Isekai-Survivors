using System;
using Blondie.Core;

namespace Isekai
{
    /// <summary>
    /// One named byte per ground cell, beside the flow-field costs and never feeding them: vision, explored ground,
    /// corruption. Writing tells <see cref="Changed"/> listeners only when the value really changes, so an overlay
    /// redraws just the cells that moved and a per-frame update of unchanged cells costs nothing.
    /// </summary>
    public sealed class CellLayer
    {
        private readonly byte[] _values;

        public CellLayer(string name, int width, int height)
        {
            if (width < 1) throw new ArgumentOutOfRangeException(nameof(width));
            if (height < 1) throw new ArgumentOutOfRangeException(nameof(height));
            Name = name;
            Width = width;
            Height = height;
            _values = new byte[width * height];
        }

        public string Name { get; }
        public int Width { get; }
        public int Height { get; }

        /// <summary>A cell's value changed: the cell, the value before and the value after.</summary>
        public event CellChangedHandler Changed;

        public bool Contains(Int2 cell) => (uint)cell.x < (uint)Width && (uint)cell.y < (uint)Height;

        /// <summary>The value at a cell (0 outside the map).</summary>
        public byte Get(Int2 cell) => Contains(cell) ? _values[cell.x + cell.y * Width] : (byte)0;

        /// <summary>Sets a cell; true when the value changed (nothing happens outside the map).</summary>
        public bool Set(Int2 cell, byte value)
        {
            if (!Contains(cell)) return false;
            int i = cell.x + cell.y * Width;
            byte before = _values[i];
            if (before == value) return false;
            _values[i] = value;
            Changed?.Invoke(cell, before, value);
            return true;
        }
    }

    /// <summary>Listens to <see cref="CellLayer.Changed"/>.</summary>
    public delegate void CellChangedHandler(Int2 cell, byte before, byte after);
}
