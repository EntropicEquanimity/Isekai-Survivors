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
        private Damageable _damageable;
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
            _damageable = GetComponent<Damageable>();
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

        // The nearest hostile unit around the post.
        private CombatUnit Nearest(float radius, out float distance) =>
            Targeting.NearestHostile(_damageable != null ? _damageable.Unit : null, _post, Mathf.Max(radius, _attackRange + 0.5f), transform.position, _units, out distance);

        private static float Distance(Float3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.y), b);
    }
}
