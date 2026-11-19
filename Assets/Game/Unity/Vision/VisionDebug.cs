using UnityEngine;

namespace Isekai
{
    /// <summary>The fog's debug rows: switch it off to see the whole Region, and how many units look out and hide.</summary>
    [RegionSystem]
    public sealed class VisionDebug : MonoBehaviour
    {
        private void OnEnable()
        {
            DebugActions.Toggle(this, "Region", "Fog of war", () => RegionVision.IsOn, on => RegionVision.IsOn = on);
            DebugActions.Info(this, "Info", () =>
            {
                RegionVision vision = RegionVision.Current;
                return vision != null && vision.IsReady ? $"Viewers {vision.ViewerCount} · hidden in fog {FogUnits.Current?.HiddenCount ?? 0}" : null;
            });
        }

        private void OnDisable() => DebugActions.Remove(this);
    }
}
