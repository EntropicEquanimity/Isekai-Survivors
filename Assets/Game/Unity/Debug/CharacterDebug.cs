using Blondie.Combat;
using UnityEngine;

namespace Isekai
{
    /// <summary>The Character's debug rows: god mode (it can't be hurt while on) and healing.</summary>
    [RegionSystem]
    public sealed class CharacterDebug : MonoBehaviour
    {
        private bool _godMode;

        private void OnEnable()
        {
            DebugActions.Toggle(this, "Character", "God mode", () => _godMode, on => { _godMode = on; ApplyGodMode(); });
            DebugActions.Button(this, "Character", "Heal", Heal);
        }

        private void OnDisable() => DebugActions.Remove(this);

        // Follows whichever body the player controls.
        private void Update()
        {
            if (_godMode) ApplyGodMode();
        }

        private void ApplyGodMode()
        {
            CharacterControl character = Player.Character;
            if (character != null && character.TryGetComponent(out Damageable d)) d.Invulnerable = _godMode;
        }

        private static void Heal()
        {
            CharacterControl character = Player.Character;
            if (character != null && character.TryGetComponent(out Damageable d) && !d.IsDead) d.Heal(d.MaxHealth);
        }
    }
}
