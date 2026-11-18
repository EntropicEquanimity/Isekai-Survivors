using Blondie.Abilities;
using Blondie.Combat;
using Blondie.Controllers;
using Blondie.Core;
using System.Collections.Generic;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// Hands the Character's walking between the player and its own fighting. Its weapons (the <see cref="AutoCaster"/>)
    /// always fire at enemies in range, steered or not. While the player steers, the <see cref="PlayerBrain"/> drives the
    /// motor; after a moment without input the Character walks to enemies inside its leash and back to where it was left
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
        private readonly List<CombatUnit> _units = new();
        private CharacterMotor _motor;
        private PlayerBrain _brain;
        private AutoCaster _autoCaster;
        private Team _team;
        private Damageable _damageable;
        private Renderer[] _renderers;
        private Vector3 _anchor;
        private float _nextThink;
        private AutoFightOrder _order = AutoFightOrder.PlayerControl;

        public AutoFightOrder Order => _order;
        public bool IsAutoFighting => _fight.IsActive;
        public bool IsDead => _damageable != null && _damageable.Unit.IsDead;

        private void Awake()
        {
            _motor = GetComponent<CharacterMotor>();
            _brain = GetComponent<PlayerBrain>();
            _autoCaster = GetComponent<AutoCaster>();
            _team = GetComponent<Team>();
            _damageable = GetComponent<Damageable>();
            _renderers = GetComponentsInChildren<Renderer>(true);
            _fight.IdleDelay = _idleDelay;
            _fight.Leash = _leash;
            SetPlayerControl(true);
            if (_damageable == null) return;
            _damageable.Died += OnDied;
            _damageable.Revived += OnRevived;
        }

        private void OnDestroy()
        {
            if (_damageable == null) return;
            _damageable.Died -= OnDied;
            _damageable.Revived -= OnRevived;
        }

        // Dead: hidden and still until a Cloning Pod brings it back (CloningPods).
        private void OnDied(DamageResult result)
        {
            _brain.enabled = false;
            _autoCaster.enabled = false;
            _motor.Stop();
            _motor.MoveInput = Vector2.zero;
            foreach (Renderer r in _renderers) r.enabled = false;
        }

        private void OnRevived()
        {
            foreach (Renderer r in _renderers) r.enabled = true;
            _autoCaster.enabled = true;
            _fight.Tick(0f, playerInput: true);
            SetPlayerControl(true);
        }

        private void Update()
        {
            if (IsDead) return;
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
            if (player && _motor.HasDestination) _motor.Stop();
        }

        private static bool PlayerGaveInput()
        {
            Float2 move = ActionInput.ReadVector(PlayerBrain.MoveId);
            return move.x * move.x + move.y * move.y > 0.01f || ActionInput.IsHeld("Interact");
        }

        // Every unit near the anchor through the ability world (the horde's spatial hash plus physics), nearest hostile one.
        private Transform NearestEnemy(out float distance)
        {
            distance = float.MaxValue;
            Transform best = null;
            _units.Clear();
            Vector3 a = _anchor;
            AbilityWorld.Current.FindUnits(new Float3(a.x, a.y, a.z), _leash + _attackRange, true, _units);
            string ownTeam = _team != null ? _team.Id : null;
            Vector3 p = transform.position;
            foreach (CombatUnit unit in _units)
            {
                if (unit.IsDead || unit.Team == ownTeam || string.IsNullOrEmpty(unit.Team)) continue;
                Float3 u = unit.Position;
                float d = Vector2.Distance(p, new Vector2(u.x, u.y));
                if (d >= distance) continue;
                GameObject go = unit.GameObject();
                if (go == null) continue;
                distance = d;
                best = go.transform;
            }
            return best;
        }
    }
}
