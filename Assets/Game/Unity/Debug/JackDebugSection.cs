using UnityEngine;

namespace Isekai
{
    /// <summary>Jack-specific debug rows that are missing from the existing soul/character debug sections.</summary>
    [RegionSystem]
    public sealed class JackDebugSection : MonoBehaviour
    {
        private void OnEnable()
        {
            DebugActions.Button(this, "Jack", "Possess selected demon", PossessSelectedDemon);
            DebugActions.Info(this, "Jack", Readout);
        }

        private void OnDisable() => DebugActions.Remove(this);

        private static void PossessSelectedDemon()
        {
            RegionPossession possession = RegionPossession.Current;
            if (possession == null || !DebugUnitSelection.TrySelected(out RegionEntity selected)) return;
            possession.TryPossess(selected.GameObject);
        }

        private static string Readout()
        {
            RegionSouls souls = RegionSouls.Current;
            Stockpile stockpile = RegionEconomy.Current != null ? RegionEconomy.Current.Stockpile : null;
            if (souls == null || stockpile == null) return null;
            int stone = stockpile.Get(ResourceType.Stone);
            int wood = stockpile.Get(ResourceType.Wood);
            int soulsNow = stockpile.Get(ResourceType.Souls);
            return $"Jack: Souls {soulsNow} · Stone {stone} · Wood {wood} · soul level {souls.Ledger.Level}";
        }
    }
}
