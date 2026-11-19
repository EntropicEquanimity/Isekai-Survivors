using UnityEngine;

namespace Isekai
{
    /// <summary>The Region's debug rows: capture it now, and open an Item draft. Both stand in for what will trigger them in play.</summary>
    [RegionSystem]
    public sealed class RegionDebug : MonoBehaviour
    {
        private void OnEnable()
        {
            DebugActions.Button(this, "Region", "Capture Region", () => RegionEvents.End(RegionEnd.Captured), "region");
            DebugActions.Button(this, "Region", "Open Item draft", () => FindAnyObjectByType<ItemDrafts>()?.Present(), "region");
        }

        private void OnDisable() => DebugActions.Remove(this);
    }
}
