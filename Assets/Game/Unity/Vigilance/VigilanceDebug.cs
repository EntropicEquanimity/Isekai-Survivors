using UnityEngine;

namespace Isekai
{
    /// <summary>Vigilance's debug rows: raise or lower a stage, send the next Wave now, and a readout.</summary>
    [RegionSystem]
    public sealed class VigilanceDebug : MonoBehaviour
    {
        private void OnEnable()
        {
            DebugActions.Button(this, "Mortals", "Raise Vigilance", () => VigilanceDirector.Current?.RaiseStage(), "vigilance");
            DebugActions.Button(this, "Mortals", "Lower Vigilance", () => VigilanceDirector.Current?.LowerStage(), "vigilance");
            DebugActions.Button(this, "Mortals", "Send next Wave", () => VigilanceDirector.Current?.SendWaveNow(), "vigilance");
            DebugActions.Info(this, "Info", () => VigilanceDirector.Current?.Readout());
        }

        private void OnDisable() => DebugActions.Remove(this);
    }
}
