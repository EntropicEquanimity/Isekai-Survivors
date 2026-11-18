using Blondie.Abilities;
using Blondie.Combat;
using Blondie.Controllers;
using Blondie.Core;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// Hands the Character between the player and its own fighting. While the player steers, the <see cref="PlayerBrain"/>
    /// drives the motor and the weapons stay quiet. After a moment without input the <see cref="AutoCaster"/> fires at
    /// enemies in range, and the Character walks to enemies inside its leash and back to where it was left
    /// (<see cref="AutoFight"/>).
    /// </summary>
    [RequireComponent(typeof(CharacterMotor), typeof(PlayerBrain), typeof(AutoCaster))]
    public sealed class CharacterControl : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _idleDelay = 1.5f;
        [SerializeField, Min(0f)] private float _leash = 6f;
        [Tooltip("How far the Character's weapons reach; it stops walking once an enemy is this close.")]
        [SerializeField, Min(0f)] private float _attackRange = 4f;
        [SerializeField, Min(0.1f)] private float _thinkInterval = 0.2f;

        private readonly AutoFight _fight = new();
        private readonly Collider2D[] _hits = new Collider2D[64];
        private CharacterMotor _motor;
        private PlayerBrain _brain;
        private AutoCaster _autoCaster;
        private Team _team;
        private Vector3 _anchor;
        private float _nextThink;
        private AutoFightOrder _order = AutoFightOrder.PlayerControl;

        public AutoFightOrder Order => _order;
        public bool IsAutoFighting => _fight.IsActive;

        private void Awake()
        {
            _motor = GetComponent<CharacterMotor>();
            _brain = GetComponent<PlayerBrain>();
            _autoCaster = GetComponent<AutoCaster>();
            _team = GetComponent<Team>();
            _fight.IdleDelay = _idleDelay;
            _fight.Leash = _leash;
            SetPlayerControl(true);
        }

        private void Update()
        {
            bool input = PlayerGaveInput();
            bool wasActive = _fight.IsActive;
            _fight.Tick(Time.deltaTime, input);

            if (!_fight.IsActive)
            {
                if (wasActive) SetPlayerControl(true);
                return;
            }
            if (!wasActive)
            {
                _anchor = transform.position;
                SetPlayerControl(false);
                _nextThink = 0f;
            }
            if (Time.time < _nextThink) return;
            _nextThink = Time.time + _thinkInterval;
            Think();
        }

        private void Think()
        {
            Transform enemy = NearestEnemy(out float distance);
            float fromAnchor = enemy != null ? Vector2.Distance(enemy.position, _anchor) : 0f;
            _order = _fight.Decide(enemy != null ? distance : null, fromAnchor, _attackRange, Vector2.Distance(transform.position, _anchor));
            switch (_order)
            {
                case AutoFightOrder.Approach: _motor.MoveTo(enemy.position); break;
                case AutoFightOrder.Return: _motor.MoveTo(_anchor); break;
                default: if (_motor.HasDestination) _motor.Stop(); break;
            }
        }

        private void SetPlayerControl(bool player)
        {
            _order = player ? AutoFightOrder.PlayerControl : AutoFightOrder.Hold;
            _brain.enabled = player;
            _autoCaster.enabled = !player;
            if (player && _motor.HasDestination) _motor.Stop();
        }

        private static bool PlayerGaveInput()
        {
            Float2 move = ActionInput.ReadVector(PlayerBrain.MoveId);
            return move.x * move.x + move.y * move.y > 0.01f || ActionInput.IsHeld("Interact");
        }

        // Phase 3 replaces this physics query with the horde's spatial lookup.
        private Transform NearestEnemy(out float distance)
        {
            distance = float.MaxValue;
            Transform best = null;
            int count = Physics2D.OverlapCircle(_anchor, _leash + _attackRange, ContactFilter2D.noFilter, _hits);
            for (int i = 0; i < count; i++)
            {
                Collider2D hit = _hits[i];
                if (hit.attachedRigidbody != null && hit.attachedRigidbody.gameObject == gameObject) continue;
                Team team = hit.GetComponentInParent<Team>();
                Damageable damageable = hit.GetComponentInParent<Damageable>();
                if (team == null || damageable == null || damageable.Unit.IsDead) continue;
                if (_team != null && team.Id == _team.Id) continue;
                float d = Vector2.Distance(transform.position, hit.transform.position);
                if (d < distance) { distance = d; best = damageable.transform; }
            }
            return best;
        }
    }
}
