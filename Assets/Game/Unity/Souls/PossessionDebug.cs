using UnityEngine;

namespace Isekai
{
    /// <summary>Possession's debug rows: possess the nearest demon of Jack's, summon a Demon King at the Anchor.</summary>
    [RegionSystem]
    public sealed class PossessionDebug : MonoBehaviour
    {
        private void OnEnable()
        {
            DebugActions.Button(this, "Souls", "Possess nearest demon", () => PossessNearest(), "possess");
            DebugActions.Button(this, "Souls", "Summon Demon King", () => RegionPossession.Current?.TrySummonDemonKing(), "possess");
            DebugActions.Info(this, "Info", () => RegionPossession.Current is { } p
                ? $"Possess {p.Cost()} Souls · {(DemonKing.IsKing(Player.Character) ? "Demon King" : "ordinary demon")}" + (p.Refusal != null ? $" · {p.Refusal}" : "")
                : null);
        }

        private void OnDisable() => DebugActions.Remove(this);

        public static bool PossessNearest()
        {
            RegionPossession possession = RegionPossession.Current;
            RegionSouls souls = RegionSouls.Current;
            if (possession == null || souls == null) return false;
            Vector3 from = souls.IsBodiless && souls.Wisp != null ? souls.Wisp.transform.position
                : Player.Character != null ? Player.Character.transform.position : Vector3.zero;
            GameObject demon = possession.Nearest(from, float.MaxValue);
            return demon != null && possession.TryPossess(demon);
        }
    }
}
