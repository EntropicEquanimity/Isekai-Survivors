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
        [Tooltip("A tougher enemy: drawn bigger on the map.")]
        [SerializeField] private bool _elite;

        internal ContactAttack Attack;
        private Damageable _damageable;
        private bool _subscribed;
        private RegionEntity _unit;

        public Damageable Damageable => _damageable != null ? _damageable : _damageable = GetComponent<Damageable>();
        public CombatUnit Unit => Damageable.Unit;
        public bool IsAlive => isActiveAndEnabled && !Unit.IsDead;

        /// <summary>Health before any escalation, as authored on the prefab.</summary>
        public float BaseHealth { get; private set; }

        /// <summary>Scales this enemy's health (escalation) and fills it.</summary>
        public void ScaleHealth(float multiplier)
        {
            Damageable.MaxHealth = BaseHealth * Mathf.Max(0.01f, multiplier);
            Unit.Heal(Damageable.MaxHealth);
        }

        private void Awake()
        {
            BaseHealth = Damageable.MaxHealth;
            _unit = new RegionEntity(Damageable, UnitKind.Mortal);
        }

        public float Reach => _reach;
        public float SpeedMultiplier => _speedMultiplier;
        public bool IsElite => _elite;

        /// <summary>
        /// Roaming enemies stand where they are until something wakes them: a player target coming near, or being hurt
        /// (which wakes the roamers around them too). Then they attack along the horde's flow field.
        /// </summary>
        public bool IsRoaming { get; private set; }

        /// <summary>Called right after spawning: roam (true) or attack at once (false, the default).</summary>
        public void SetRoaming(bool roaming)
        {
            if (IsRoaming == roaming) return;
            IsRoaming = roaming;
            if (roaming) HordeDirector.Current?.Sleep(this);
            else HordeDirector.Current?.Wake(this);
        }

        public void Wake() => SetRoaming(false);

        private void OnEnable()
        {
            Attack = new ContactAttack(_contactDamage, _attackInterval, _reach);
            IsRoaming = false;
            if (BaseHealth > 0f && !Mathf.Approximately(Damageable.MaxHealth, BaseHealth)) ScaleHealth(1f);   // undo a previous Wave's scaling
            if (!_subscribed) { Damageable.Damaged += OnDamaged; _subscribed = true; }
            HordeDirector.Current?.Register(this);
            _unit.Kind = TeamIds.SideOf(Unit.Team) is Side.Neutral or Side.Hostile ? UnitKind.Wildlife : UnitKind.Mortal;
            _unit.Enter();
        }

        private void OnDisable()
        {
            _unit?.Leave();
            HordeDirector.Current?.Unregister(this);
        }

        private void OnDestroy()
        {
            if (_subscribed && _damageable != null) _damageable.Damaged -= OnDamaged;
        }

        private void OnDamaged(DamageResult result)
        {
            if (!IsRoaming) return;
            Wake();
            HordeDirector.Current?.Alarm(this);
        }
    }
}
