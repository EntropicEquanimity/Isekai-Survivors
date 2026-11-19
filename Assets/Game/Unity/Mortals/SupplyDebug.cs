using Blondie.Combat;
using UnityEngine;

namespace Isekai
{
    /// <summary>Supply and troop rows for the debug menu: a readout, add Supply, raze farms.</summary>
    [RegionSystem]
    public sealed class SupplyDebug : MonoBehaviour
    {
        private void OnEnable()
        {
            DebugActions.Button(this, "Mortals", "Add Supply", () => RegionSupply.Current?.Pool.Add(25f), "supply");
            DebugActions.Button(this, "Mortals", "Raze farms", () => RazeFarms(), "supply");
            DebugActions.Info(this, "Info", () => RegionTroops.Current?.Readout());
        }

        private void OnDisable() => DebugActions.Remove(this);

        /// <summary>Razes every standing farm (their Supply stops); how many it razed.</summary>
        public static int RazeFarms()
        {
            RegionSettlements settlements = RegionSettlements.Current;
            if (settlements == null) return 0;
            int razed = 0;
            foreach (RegionSettlements.Settlement s in settlements.Settlements)
            {
                if (s.Kind != SettlementKind.Farm || s.IsRazed) continue;
                for (int i = 0; i < s.Buildings.Count; i++)
                {
                    if (s.Plan.Buildings[i].IsWall || s.Buildings[i] == null || !s.Buildings[i].IsAlive) continue;
                    CombatUnit unit = s.Buildings[i].Damageable.Unit;
                    unit.TakeDamage(unit.MaxHealth * 10f, DamageRules.TrueType);
                }
                razed++;
            }
            return razed;
        }
    }
}
