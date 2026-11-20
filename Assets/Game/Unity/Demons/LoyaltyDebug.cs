using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// Level and Loyalty debug rows: give the selected demon (or, with none selected, the one nearest Jack) experience, set its
    /// Loyalty, make it go rogue, or win it back.
    /// </summary>
    [RegionSystem]
    public sealed class LoyaltyDebug : MonoBehaviour
    {
        private void OnEnable()
        {
            DebugActions.Button(this, "Demons", "Give demon a level of XP", () => GiveLevel(Target()), "loyalty");
            DebugActions.Button(this, "Demons", "Set demon Loyalty to 10", () => Target()?.SetLoyalty(10f), "loyalty");
            DebugActions.Button(this, "Demons", "Make demon rogue", () => Target()?.MakeRogue(), "loyalty");
            DebugActions.Button(this, "Demons", "Win demon back", () => Target()?.Reclaim(), "loyalty");
            DebugActions.Info(this, "Demons", () => Target() is { } d ? $"{d.name}: {d.Describe()}" : "no demon with a Loyalty");
        }

        private void OnDisable() => DebugActions.Remove(this);

        public static void GiveLevel(DemonLoyalty demon)
        {
            if (demon == null) return;
            demon.AddXp(LoyaltyRules.XpToReach(demon.Level + 1) - demon.Xp);
        }

        /// <summary>The selected demon, else the demon nearest Jack.</summary>
        public static DemonLoyalty Target()
        {
            if (DebugUnitSelection.TrySelected(out RegionEntity selected) && DemonLoyalty.Of(selected.GameObject) is { } chosen) return chosen;
            RegionLoyalty loyalty = RegionLoyalty.Current;
            if (loyalty == null) return null;
            Vector3 from = Player.Character != null ? Player.Character.transform.position : Vector3.zero;
            DemonLoyalty best = null;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < loyalty.Tracked.Count; i++)
            {
                DemonLoyalty d = loyalty.Tracked[i];
                if (d == null || d.IsJacksBody) continue;
                float sqr = ((Vector2)(d.transform.position - from)).sqrMagnitude;
                if (sqr >= bestSqr) continue;
                bestSqr = sqr;
                best = d;
            }
            return best;
        }
    }
}
