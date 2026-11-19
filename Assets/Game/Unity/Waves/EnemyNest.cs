using System.Collections.Generic;
using Blondie.Combat;
using Blondie.Pooling;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// An enemy Base: a roaming pack lives around it and regrows over time. Hurting the nest wakes its pack. Every nest
    /// must be destroyed to capture the Region (<see cref="RegionGoal"/>).
    /// </summary>
    [RequireComponent(typeof(Damageable))]
    public sealed class EnemyNest : MonoBehaviour
    {
        private static readonly List<EnemyNest> s_all = new();

        [SerializeField] private GameObject _memberPrefab;
        [SerializeField, Min(0)] private int _packSize = 25;
        [SerializeField, Min(0.5f)] private float _packRadius = 4f;
        [Tooltip("Seconds between new pack members while the pack is below its size.")]
        [SerializeField, Min(0.1f)] private float _regrowSeconds = 6f;
        [SerializeField, Min(0f)] private float _alarmRadius = 9f;

        private readonly List<HordeMember> _pack = new();
        private Damageable _damageable;
        private float _regrowTimer;
        private bool _destroyed;

        public static IReadOnlyList<EnemyNest> All => s_all;

        /// <summary>An enemy Base was destroyed.</summary>
        public static event System.Action<EnemyNest> Destroyed;
        public bool IsDestroyed => _destroyed;
        public int PackCount => _pack.Count;

        private void Awake()
        {
            _damageable = GetComponent<Damageable>();
            _damageable.Damaged += OnDamaged;
            _damageable.Died += OnDied;
        }

        private void OnEnable() => s_all.Add(this);

        private void OnDisable() => s_all.Remove(this);

        private void Start()
        {
            RegionGoal.Current?.NestAdded();
            for (int i = 0; i < _packSize; i++) SpawnMember();
        }

        private void Update()
        {
            using (PerfMarkers.Nests.Auto()) Tick();
        }

        private void Tick()
        {
            if (_destroyed) return;
            _pack.RemoveAll(m => m == null || !m.IsAlive || !m.IsRoaming);
            if (_pack.Count >= _packSize) { _regrowTimer = 0f; return; }
            _regrowTimer += Time.deltaTime;
            if (_regrowTimer < _regrowSeconds) return;
            _regrowTimer = 0f;
            SpawnMember();
        }

        private void SpawnMember()
        {
            if (_memberPrefab == null) return;
            Vector2 offset = Random.insideUnitCircle * _packRadius;
            GameObject go = Pools.Spawn(_memberPrefab, transform.position + (Vector3)offset, Quaternion.identity);
            var member = go.GetComponent<HordeMember>();
            member.SetRoaming(true);
            _pack.Add(member);
        }

        private void OnDamaged(DamageResult result) => HordeDirector.Current?.WakeAround(transform.position, _alarmRadius);

        private void OnDied(DamageResult result)
        {
            if (_destroyed) return;
            _destroyed = true;
            RegionGoal.Current?.NestDestroyed();
            Destroyed?.Invoke(this);
            gameObject.SetActive(false);
        }
    }
}
