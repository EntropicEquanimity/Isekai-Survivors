using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// A spawn circle becomes discovered once Jack's side sees it (a few times a second, from the vision map), and stays
    /// discovered: the map keeps it marked (<see cref="SpawnCirclePainter"/>) until it is destroyed.
    /// </summary>
    [RegionSystem]
    public sealed class SpawnCircleDiscovery : MonoBehaviour
    {
        private const float ScanSeconds = 0.5f;

        private float _next;

        private void Update()
        {
            if (Time.time < _next) return;
            _next = Time.time + ScanSeconds;
            VigilanceDirector director = VigilanceDirector.Current;
            RegionGrid grid = RegionGrid.Current;
            if (director == null || grid == null) return;
            RegionVision vision = RegionVision.IsOn ? RegionVision.Current : null;
            bool everythingShown = vision == null;   // fog is off: the whole Region is seen
            if (!everythingShown && !vision.IsReady) return;
            foreach (VigilanceSpawnCircle circle in director.Circles)
            {
                if (circle == null || !circle.IsAlive || circle.Discovered) continue;
                if (everythingShown || vision.CanSee(Side.Yours, circle.transform.position)) circle.MarkDiscovered();
            }
        }
    }
}
