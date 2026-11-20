using System;
using System.Collections.Generic;
using Blondie.Combat;
using Blondie.Core;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// A placed Building. It goes up over its asset's build time, drawn with its construction sprite while its health
    /// climbs from a tenth to full (so it can be destroyed unfinished), then shows its finished art and switches on the
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
        [Tooltip("Shown on the first art renderer while the Building goes up (its finished sprite comes back when done).")]
        [SerializeField] private Sprite _constructionSprite;
        [SerializeField] private bool _startBuilt;
        [Tooltip("Losing this Building loses the Run.")]
        [SerializeField] private bool _isMainBase;

        private const float StartingHealth = 0.1f;

        private Construction _construction;
        private Damageable _damageable;
        private Sprite _builtSprite;
        private float _healthGiven;

        public static IReadOnlyList<Building> All => s_all;

        public BuildingAsset Asset => _asset;
        public bool IsBuilt => _construction == null || _construction.IsComplete;
        public float Progress => _construction?.Progress ?? 1f;
        public bool IsMainBase => _isMainBase;
        public bool IsAlive => _damageable != null && !_damageable.Unit.IsDead;
        public Int2 Origin { get; internal set; }

        /// <summary>The side that owns it (its Blondie Combat team, <see cref="TeamIds"/>): the player's or a defender's.</summary>
        public string Team => _damageable != null ? _damageable.Unit.Team : GetComponent<Damageable>().Unit.Team;

        public bool IsPlayers => Team == TeamIds.Player;
        public Damageable Damageable => _damageable;

        public event Action<Building> Completed;
        public event Action<Building> Destroyed;

        /// <summary>Starts construction of a freshly placed Building.</summary>
        internal void BeginConstruction(BuildingAsset asset)
        {
            _asset = asset;
            _construction = new Construction(asset.BuildSeconds);
            SetWorking(false);
            // A site starts at a tenth of its health and gains the rest as it goes up.
            CombatUnit cu = _damageable.Unit;
            cu.TakeDamage(cu.MaxHealth * (1f - StartingHealth), DamageRules.TrueType);
            _healthGiven = 0f;
            DrawProgress();
        }

        /// <summary>Finishes construction at once (the debug menu).</summary>
        public void FinishConstruction()
        {
            if (_construction == null || _construction.IsComplete) return;
            _construction.Tick(float.MaxValue);
            GainHealth();
            DrawProgress();
            SetWorking(true);
            Completed?.Invoke(this);
        }

        private void Awake()
        {
            _damageable = GetComponent<Damageable>();
            _damageable.Died += OnDied;
            if (_art.Length > 0 && _art[0] != null) _builtSprite = _art[0].sprite;
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
            GainHealth();
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

        // Health follows progress: the share of the remaining nine tenths that construction has reached so far.
        private void GainHealth()
        {
            if (_damageable == null || _damageable.IsDead) return;
            float owed = _damageable.MaxHealth * (1f - StartingHealth) * Progress;
            if (owed <= _healthGiven) return;
            _damageable.Heal(owed - _healthGiven);
            _healthGiven = owed;
        }

        private void DrawProgress()
        {
            // With a construction sprite the site looks like a site; without one it is drawn faded.
            bool site = !IsBuilt && _constructionSprite != null;
            float alpha = IsBuilt || site ? 1f : Mathf.Lerp(0.3f, 0.75f, Progress);
            for (int i = 0; i < _art.Length; i++)
            {
                SpriteRenderer r = _art[i];
                if (r == null) continue;
                if (i == 0 && _builtSprite != null) r.sprite = site ? _constructionSprite : _builtSprite;
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
