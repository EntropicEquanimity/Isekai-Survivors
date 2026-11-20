using Blondie.Combat;
using UnityEngine;

namespace Isekai
{
    /// <summary>The soul economy's debug rows: free Souls, kill Jack's body, raise soul level.</summary>
    [RegionSystem]
    public sealed class SoulsDebug : MonoBehaviour
    {
        private void OnEnable()
        {
            DebugActions.Button(this, "Souls", "+100 Souls", () => AddSouls(100), "souls");
            DebugActions.Button(this, "Souls", "Kill Jack's body", KillBody, "souls");
            DebugActions.Button(this, "Souls", "Raise soul level", () => RegionSouls.Current?.TryRaiseSoulLevel(), "souls");
            DebugActions.Info(this, "Info", () => RegionSouls.Current is { } s ? $"Souls {s.Souls} · soul level {s.Ledger.Level} · deaths {s.Ledger.Deaths}" + (s.IsBodiless ? $" · bodiless, -{s.DrainPerSecond:0.0}/s" : "") : null);
        }

        private void OnDisable() => DebugActions.Remove(this);

        public static void AddSouls(int amount) => RegionEconomy.Current?.Stockpile.Add(ResourceType.Souls, amount);

        public static void KillBody()
        {
            CharacterControl body = Player.Character;
            if (body != null && !body.IsDead && body.TryGetComponent(out Damageable d)) d.Kill();
        }
    }
}
