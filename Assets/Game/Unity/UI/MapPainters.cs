using System.Collections.Generic;
using Blondie.Core;

namespace Isekai
{
    /// <summary>One layer of the Region map: it draws onto the <see cref="MapCanvas"/> each redraw, after every painter with a lower <see cref="Order"/>.</summary>
    public interface IMapPainter
    {
        /// <summary>Where it draws (lower first); see <see cref="MapOrder"/>.</summary>
        int Order { get; }

        void Paint(MapCanvas canvas);
    }

    /// <summary>Named layers of the Region map, bottom to top. A new layer (fog of war, corruption) picks a slot between these.</summary>
    public static class MapOrder
    {
        public const int Ground = 0;
        public const int Places = 100;
        public const int Units = 200;
        public const int Overlays = 300;
        public const int Camera = 900;
    }

    /// <summary>
    /// The painters the Region map draws with. A system adds its painter when it starts and removes it when it stops
    /// (see <see cref="RegionSystemAttribute"/>); <see cref="RegionMapView"/> runs them in <see cref="IMapPainter.Order"/>.
    /// Cleared when a Region starts.
    /// </summary>
    public static class MapPainters
    {
        private static readonly List<IMapPainter> s_painters = new();

        static MapPainters() => StaticReset.Register(Reset);

        /// <summary>Every painter, in drawing order.</summary>
        public static IReadOnlyList<IMapPainter> All => s_painters;

        public static void Add(IMapPainter painter)
        {
            if (s_painters.Contains(painter)) return;
            int at = s_painters.Count;
            while (at > 0 && s_painters[at - 1].Order > painter.Order) at--;   // after equal orders: first added draws first
            s_painters.Insert(at, painter);
        }

        public static void Remove(IMapPainter painter) => s_painters.Remove(painter);

        /// <summary>A new Region is starting.</summary>
        public static void Reset() => s_painters.Clear();
    }
}
