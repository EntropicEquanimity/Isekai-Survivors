using UnityEngine;

namespace Isekai
{
    /// <summary>The targets your Portals are set to raid, marked on the map (a cross), and each Portal's rally point (a dot) while it is on Guard.</summary>
    [RegionSystem]
    public sealed class PortalMarksPainter : MonoBehaviour, IMapPainter
    {
        private static readonly Color32 Raid = new(255, 120, 60, 255);
        private static readonly Color32 Rally = new(120, 200, 255, 255);

        private static readonly string[] Cross =
        {
            "#...#",
            ".#.#.",
            "..#..",
            ".#.#.",
            "#...#",
        };

        private static readonly string[] Dot =
        {
            ".#.",
            "###",
            ".#.",
        };

        public int Order => MapOrder.Overlays + 10;   // over the fog: a mark is for a place you may not see

        private void OnEnable() => MapPainters.Add(this);

        private void OnDisable() => MapPainters.Remove(this);

        public void Paint(MapCanvas canvas)
        {
            foreach (Building building in Building.All)
            {
                if (building == null || !building.IsPlayers || !building.IsAlive || !building.TryGetComponent(out UnitProducer portal)) continue;
                if (portal.HasRaidTarget && portal.Order == PortalOrder.Raid) canvas.Icon(canvas.ToPixel(portal.RaidTarget), Cross, Raid);
                else if (portal.HasRally && portal.Order == PortalOrder.Guard) canvas.Icon(canvas.ToPixel(portal.Rally), Dot, Rally);
            }
        }
    }
}
