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
    public sealed class CharacterControl : MonoBehaviour, IRegionEndListener
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
        private Damageable _damageable;
        private Renderer[] _renderers;
        private Vector3 _post;
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
            _damageable = GetComponent<Damageable>();
            _renderers = GetComponentsInChildren<Renderer>(true);
            _fight.IdleDelay = _idleDelay;
            _fight.Leash = _leash;
            SetPlayerControl(true);
            Player.Control(this);
            if (_damageable == null) return;
            _damageable.Died += OnDied;
            _damageable.Revived += OnRevived;
        }

        private void OnDestroy()
        {
            Player.Release(this);
            if (_damageable == null) return;
            _damageable.Died -= OnDied;
            _damageable.Revived -= OnRevived;
        }

        // Dead: hidden and still until revived.
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
            using (PerfMarkers.Character.Auto()) Tick();
        }

        private void Tick()
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
                _post = transform.position;
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
            float fromPost = enemy != null ? Vector2.Distance(enemy.position, _post) : 0f;
            _order = _fight.Decide(enemy != null ? distance : null, fromPost, _attackRange, Vector2.Distance(transform.position, _post));
            switch (_order)
            {
                case AutoFightOrder.Approach: _motor.MoveTo(enemy.position); break;
                case AutoFightOrder.Return: _motor.MoveTo(_post); break;
                default: if (_motor.HasDestination) _motor.Stop(); break;
            }
        }

        void IRegionEndListener.OnRegionEnded(RegionEnd end)
        {
            if (end == RegionEnd.Defeated) Freeze();
        }

        /// <summary>Stops everything: no steering, no fighting, no weapons (the Run is lost).</summary>
        public void Freeze()
        {
            if (_brain != null) _brain.enabled = false;
            if (_autoCaster != null) _autoCaster.enabled = false;
            if (_motor != null && _motor.HasDestination) _motor.Stop();
            enabled = false;
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

        // The nearest hostile unit around the post (inside the leash plus weapon range).
        private Transform NearestEnemy(out float distance)
        {
            CombatUnit unit = Targeting.NearestHostile(_damageable != null ? _damageable.Unit : null, _post, _leash + _attackRange, transform.position, _units, out distance);
            GameObject go = unit?.GameObject();
            return go != null ? go.transform : null;
        }
    }
}
