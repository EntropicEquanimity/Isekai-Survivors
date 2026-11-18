using System.Collections.Generic;
using Blondie.Abilities;
using Blondie.Combat;
using Blondie.Controllers;
using Blondie.Core;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// A Unit from a <see cref="UnitProducer"/>: its weapons fire at enemies in range (<see cref="AutoCaster"/>), and it
    /// walks to enemies and back to its post by the producer's <see cref="Stance"/> (<see cref="Engagement"/>).
    /// </summary>
    [RequireComponent(typeof(CharacterMotor), typeof(AutoCaster))]
    public sealed class DefenderBrain : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _attackRange = 1.2f;
        [SerializeField, Min(0.05f)] private float _thinkInterval = 0.25f;

        private readonly List<CombatUnit> _units = new();
        private CharacterMotor _motor;
        private Team _team;
        private UnitProducer _producer;
        private Vector3 _post;
        private float _nextThink;

        public Vector3 Post => _post;
        public AutoFightOrder Order { get; private set; }
        public Stance Stance => _producer != null ? _producer.Stance : Stance.Hold;

        internal void Assign(UnitProducer producer, Vector3 post)
        {
            _producer = producer;
            _post = post;
            _motor.MoveTo(post);
        }

        private void Awake()
        {
            _motor = GetComponent<CharacterMotor>();
            _team = GetComponent<Team>();
            _post = transform.position;
        }

        private void Update()
        {
            if (Time.time < _nextThink) return;
            _nextThink = Time.time + _thinkInterval;
            float leash = Stance.Leash();
            CombatUnit enemy = Nearest(leash + _attackRange, out float distance);
            float fromPost = enemy != null ? Distance(enemy.Position, _post) : 0f;
            Order = Engagement.Decide(enemy != null ? distance : null, fromPost, _attackRange, leash, Vector2.Distance(transform.position, _post));
            switch (Order)
            {
                case AutoFightOrder.Approach: _motor.MoveTo(new Vector3(enemy.Position.x, enemy.Position.y, 0f)); break;
                case AutoFightOrder.Return: _motor.MoveTo(_post); break;
                default: if (_motor.HasDestination) _motor.Stop(); break;
            }
        }

        private CombatUnit Nearest(float radius, out float distance)
        {
            distance = float.MaxValue;
            CombatUnit best = null;
            _units.Clear();
            AbilityWorld.Current.FindUnits(new Float3(_post.x, _post.y, 0f), Mathf.Max(radius, _attackRange + 0.5f), true, _units);
            string own = _team != null ? _team.Id : null;
            foreach (CombatUnit unit in _units)
            {
                if (unit.IsDead || string.IsNullOrEmpty(unit.Team) || unit.Team == own) continue;
                float d = Distance(unit.Position, transform.position);
                if (d < distance) { distance = d; best = unit; }
            }
            return best;
        }

        private static float Distance(Float3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.y), b);
    }
}
