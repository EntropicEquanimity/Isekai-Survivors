using Blondie.Combat;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// One enemy in the horde. It has no collider and no Update: the <see cref="HordeDirector"/>'s crowd moves it, its
    /// spatial hash finds it, and it hits player targets in reach on a cooldown. Pooled: registers while enabled.
    /// </summary>
    [RequireComponent(typeof(Damageable))]
    public sealed class HordeMember : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _contactDamage = 5f;
        [SerializeField, Min(0.05f)] private float _attackInterval = 1f;
        [Tooltip("How far past a target's edge this enemy can hit.")]
        [SerializeField, Min(0f)] private float _reach = 0.4f;
        [SerializeField, Min(0f)] private float _speedMultiplier = 1f;

        internal ContactAttack Attack;
        private Damageable _damageable;

        public Damageable Damageable => _damageable != null ? _damageable : _damageable = GetComponent<Damageable>();
        public CombatUnit Unit => Damageable.Unit;
        public bool IsAlive => isActiveAndEnabled && !Unit.IsDead;
        public float Reach => _reach;
        public float SpeedMultiplier => _speedMultiplier;

        private void OnEnable()
        {
            Attack = new ContactAttack(_contactDamage, _attackInterval, _reach);
            HordeDirector.Current?.Register(this);
        }

        private void OnDisable() => HordeDirector.Current?.Unregister(this);
    }
}
