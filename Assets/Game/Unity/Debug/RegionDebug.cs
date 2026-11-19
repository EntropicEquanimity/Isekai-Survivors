using UnityEngine;

namespace Isekai
{
    /// <summary>The Region's debug rows: take out the Seat, and raze the next village, farm or camp. Both do it the way a fight would, by destroying Buildings.</summary>
    [RegionSystem]
    public sealed class RegionDebug : MonoBehaviour
    {
        private void OnEnable()
        {
            DebugActions.Button(this, "Region", "Destroy the Seat", () => RegionSettlements.Current?.DestroySeat(), "region");
            DebugActions.Button(this, "Region", "Raze a settlement", () => RegionSettlements.Current?.RazeNext(), "region");
        }

        private void OnDisable() => DebugActions.Remove(this);
    }
}
