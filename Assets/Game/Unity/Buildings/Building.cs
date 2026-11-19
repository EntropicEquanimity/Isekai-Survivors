using System;
using System.Collections.Generic;
using Blondie.Combat;
using Blondie.Core;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// A placed Building. It goes up over its asset's build time (drawn faded until done), then switches on the
    /// behaviours that make it work (a producer, a tower's caster). When destroyed it frees its ground.
    /// Pre-placed Buildings (the Main Base) start built and register themselves.
    /// </summary>
    [RequireComponent(typeof(Damageable))]
    public sealed class Building : MonoBehaviour
    {
        private static readonly List<Building> s_all = new();

        [SerializeField] private BuildingAsset _asset;
        [Tooltip("Switched on when construction finishes.")]
        [SerializeField] private Behaviour[] _activeWhenBuilt = Array.Empty<Behaviour>();
        [SerializeField] private SpriteRenderer[] _art = Array.Empty<SpriteRenderer>();
        [SerializeField] private bool _startBuilt;
        [Tooltip("Losing this Building loses the Run.")]
        [SerializeField] private bool _isMainBase;

        private Construction _construction;
        private Damageable _damageable;

        public static IReadOnlyList<Building> All => s_all;

        public BuildingAsset Asset => _asset;
        public bool IsBuilt => _construction == null || _construction.IsComplete;
        public float Progress => _construction?.Progress ?? 1f;
        public bool IsMainBase => _isMainBase;
        public bool IsAlive => _damageable != null && !_damageable.Unit.IsDead;
        public Int2 Origin { get; internal set; }
        public Damageable Damageable => _damageable;

        public event Action<Building> Completed;
        public event Action<Building> Destroyed;

        /// <summary>Starts construction of a freshly placed Building.</summary>
        internal void BeginConstruction(BuildingAsset asset)
        {
            _asset = asset;
            _construction = new Construction(asset.BuildSeconds);
            SetWorking(false);
            DrawProgress();
        }

        /// <summary>Finishes construction at once (the debug menu).</summary>
        public void FinishConstruction()
        {
            if (_construction == null || _construction.IsComplete) return;
            _construction.Tick(float.MaxValue);
            DrawProgress();
            SetWorking(true);
            Completed?.Invoke(this);
        }

        private void Awake()
        {
            _damageable = GetComponent<Damageable>();
            _damageable.Died += OnDied;
        }

        private void Start()
        {
            if (_construction != null) return;   // placed by RegionBuildings
            SetWorking(true);
            if (_startBuilt) RegionBuildings.Current?.RegisterExisting(this);
        }

        private void OnEnable() => s_all.Add(this);

        private void OnDisable() => s_all.Remove(this);

        private void OnDestroy()
        {
            if (_damageable != null) _damageable.Died -= OnDied;
        }

        private void Update()
        {
            if (_construction == null || _construction.IsComplete) return;
            bool done = _construction.Tick(Time.deltaTime);
            DrawProgress();
            if (!done) return;
            SetWorking(true);
            Completed?.Invoke(this);
        }

        private void SetWorking(bool working)
        {
            foreach (Behaviour b in _activeWhenBuilt)
                if (b != null) b.enabled = working;
        }

        private void DrawProgress()
        {
            float alpha = IsBuilt ? 1f : Mathf.Lerp(0.3f, 0.75f, Progress);
            foreach (SpriteRenderer r in _art)
            {
                if (r == null) continue;
                Color c = r.color;
                c.a = alpha;
                r.color = c;
            }
        }

        private void OnDied(DamageResult result)
        {
            SetWorking(false);
            RegionBuildings.Current?.Remove(this);
            Destroyed?.Invoke(this);
            Destroy(gameObject, 0.1f);
        }
    }
}
