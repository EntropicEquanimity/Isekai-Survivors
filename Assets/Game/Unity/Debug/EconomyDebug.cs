using System;
using UnityEngine;

namespace Isekai
{
    /// <summary>The Economy's debug rows: free Resources.</summary>
    [RegionSystem]
    public sealed class EconomyDebug : MonoBehaviour
    {
        private void OnEnable()
        {
            DebugActions.Button(this, "Resources", "+100 each", () => AddResources(100), "give");
            DebugActions.Button(this, "Resources", "+1000 each", () => AddResources(1000), "give");
        }

        private void OnDisable() => DebugActions.Remove(this);

        public static void AddResources(int amount)
        {
            Stockpile pile = RegionEconomy.Current != null ? RegionEconomy.Current.Stockpile : null;
            if (pile == null) return;
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType))) pile.Add(type, amount);
        }
    }
}
