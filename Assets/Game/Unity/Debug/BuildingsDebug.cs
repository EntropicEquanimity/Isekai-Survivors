using UnityEngine;

namespace Isekai
{
    /// <summary>The Buildings' debug row: finish every construction at once.</summary>
    [RegionSystem]
    public sealed class BuildingsDebug : MonoBehaviour
    {
        private void OnEnable() => DebugActions.Button(this, "Buildings", "Finish buildings", FinishBuildings);

        private void OnDisable() => DebugActions.Remove(this);

        public static void FinishBuildings()
        {
            foreach (Building b in FindObjectsByType<Building>(FindObjectsInactive.Exclude)) b.FinishConstruction();
        }
    }
}
