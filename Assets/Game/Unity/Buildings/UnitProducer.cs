using System;
using System.Collections.Generic;
using Blondie.Pooling;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// A Building that keeps Units alive: one every few seconds up to a cap, each fighting around the Building by the
    /// <see cref="Stance"/> set on it. Switched on by its <see cref="Building"/> once built.
    /// </summary>
    public sealed class UnitProducer : MonoBehaviour
    {
        [SerializeField] private GameObject _unitPrefab;
        [SerializeField, Min(0.1f)] private float _interval = 6f;
        [SerializeField, Min(0)] private int _cap = 4;
        [SerializeField] private Stance _stance = Stance.Guard;
        [Tooltip("Where Units appear, from the Building's centre.")]
        [SerializeField] private Vector2 _exit = new(0f, -1.4f);

        private readonly List<DefenderBrain> _units = new();
        private Production _production;

        public Stance Stance
        {
            get => _stance;
            set { if (_stance == value) return; _stance = value; StanceChanged?.Invoke(value); }
        }

        public IReadOnlyList<DefenderBrain> Units => _units;
        public event Action<Stance> StanceChanged;

        private void Awake() => _production = new Production(_interval, _cap);

        private void Update()
        {
            _units.RemoveAll(u => u == null || !u.isActiveAndEnabled);
            if (!_production.Tick(Time.deltaTime, _units.Count)) return;
            Vector3 at = transform.position + (Vector3)_exit;
            GameObject go = Pools.Spawn(_unitPrefab, at, Quaternion.identity);
            var unit = go.GetComponent<DefenderBrain>();
            unit.Assign(this, PostFor(_units.Count));
            _units.Add(unit);
        }

        // Posts in a ring around the Building, so Units don't stand on each other.
        private Vector3 PostFor(int index)
        {
            float angle = index * 2.399963f;   // golden angle
            return transform.position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * 2f;
        }
    }
}
