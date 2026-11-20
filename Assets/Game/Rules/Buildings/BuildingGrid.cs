using System;
using System.Collections.Generic;
using Blondie.Core;

namespace Isekai
{
    /// <summary>
    /// Which ground cells Buildings stand on. A footprint fits where every cell is buildable ground (the caller says
    /// what that is, e.g. not a wall) and no other Building stands. Cells are the same ones the horde's flow field uses.
    /// </summary>
    public sealed class BuildingGrid<T> where T : class
    {
        private readonly Dictionary<Int2, T> _occupant = new();
        private readonly Func<Int2, bool> _isGround;

        /// <param name="isGround">Whether a cell is ground a Building may stand on.</param>
        public BuildingGrid(Func<Int2, bool> isGround) => _isGround = isGround ?? throw new ArgumentNullException(nameof(isGround));

        public int OccupiedCount => _occupant.Count;

        public T At(Int2 cell) => _occupant.TryGetValue(cell, out T t) ? t : null;

        public bool CanPlace(Int2 origin, Int2 size)
        {
            if (size.x < 1 || size.y < 1) return false;
            for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
            {
                var cell = new Int2(origin.x + x, origin.y + y);
                if (_occupant.ContainsKey(cell) || !_isGround(cell)) return false;
            }
            return true;
        }

        public bool Place(T building, Int2 origin, Int2 size)
        {
            if (building == null) throw new ArgumentNullException(nameof(building));
            if (!CanPlace(origin, size)) return false;
            for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
                _occupant[new Int2(origin.x + x, origin.y + y)] = building;
            return true;
        }

        /// <summary>Frees every cell <paramref name="building"/> stands on.</summary>
        public void Remove(T building)
        {
            var cells = new List<Int2>();
            foreach (KeyValuePair<Int2, T> pair in _occupant)
                if (pair.Value == building) cells.Add(pair.Key);
            foreach (Int2 cell in cells) _occupant.Remove(cell);
        }

        /// <summary>The origin that centres a footprint of <paramref name="size"/> on <paramref name="cell"/>.</summary>
        public static Int2 OriginCentredOn(Int2 cell, Int2 size) => new(cell.x - (size.x - 1) / 2, cell.y - (size.y - 1) / 2);
    }
}
