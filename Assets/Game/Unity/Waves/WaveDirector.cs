using System;
using System.Collections.Generic;
using Blondie.Combat;
using Blondie.Core;
using Blondie.FlowFields;
using Blondie.Pooling;
using UnityEngine;
using Random = System.Random;

namespace Isekai
{
    /// <summary>
    /// Sends the Region's Waves (<see cref="WaveSchedule"/>) at the player from the map's edges, scaled by
    /// <see cref="Escalation"/>: Horde from one side, Squad as a tight clump of tougher enemies, Mix (and Flight, for
    /// now) spread around the edges. Wave enemies attack at once. The final Wave's deaths count toward
    /// <see cref="RegionGoal"/>. Spawns a batch per frame so a big Wave doesn't hitch.
    /// </summary>
    public sealed class WaveDirector : MonoBehaviour, IRegionSetup, IThreat, IRegionEndListener
    {
        [SerializeField] private Wave[] _waves = Array.Empty<Wave>();
        [SerializeField] private GameObject _chaserPrefab;
        [Tooltip("Squad Waves use these, a quarter as many.")]
        [SerializeField] private GameObject _squadPrefab;
        [Header("Escalation")]
        [SerializeField, Min(0f)] private float _perMinute = 0.05f;
        [SerializeField, Min(0f)] private float _perDepth = 0.25f;
        [Tooltip("Steps from the Planet's edge toward the Capital. Phase 7 sets it from the Planet map.")]
        [SerializeField, Min(0)] private int _depth;
        [Header("Spawning")]
        [Tooltip("Waves appear in a ring around the Main Base, at least this far from every player target...")]
        [SerializeField, Min(0f)] private float _minDistanceFromTargets = 30f;
        [Tooltip("...and at most this far from the Main Base, so they arrive in good time on a big map.")]
        [SerializeField, Min(1f)] private float _maxDistanceFromBase = 50f;
        [Tooltip("Without a Main Base, how far in from the map's edge Waves appear.")]
        [SerializeField, Min(1)] private int _edgeBand = 6;
        [SerializeField, Min(1)] private int _perFrame = 80;
        [SerializeField] private int _seed = 7;

        private readonly List<Wave> _due = new();
        private readonly Queue<(GameObject prefab, Vector3 at, bool final)> _pending = new();
        private WaveSchedule _schedule;
        private Escalation _escalation;
        private Random _random;
        private float _clock;
        private float _runSecondsAtStart;
        private int _released;

        public WaveSchedule Schedule => _schedule ??= new WaveSchedule(_waves);
        public float Clock => _clock;
        public int Released => _released;
        /// <summary>Enemies of released Waves still waiting to spawn.</summary>
        public int PendingSpawns => _pending.Count;
        public float Multiplier => (_escalation ??= new Escalation { PerMinute = _perMinute, PerDepth = _perDepth }).Multiplier(_runSecondsAtStart + _clock, _depth);

        /// <summary>Where this Region sits in the Run: its depth toward the Capital and the Run time before it began.</summary>
        public void SetRun(int depth, float runSecondsAtStart)
        {
            _depth = Mathf.Max(0, depth);
            _runSecondsAtStart = Mathf.Max(0f, runSecondsAtStart);
        }

        public int Depth => _depth;
        public float SecondsUntilNext => Schedule.SecondsUntilNext(_clock);

        /// <summary>Jumps the Region clock forward (tests and debugging).</summary>
        public void Skip(float seconds) => _clock += Mathf.Max(0f, seconds);

        /// <summary>How fast the Wave clock runs (the debug menu's fast Waves); 1 is normal.</summary>
        public float ClockSpeed { get; set; } = 1f;

        private void Awake() => _random = new Random(_seed);

        /// <summary>Depth and Run clock from the Region; its own random stream; it is the Region's threat for now.</summary>
        void IRegionSetup.Setup(RegionContext region)
        {
            if (region.Region != null) SetRun(region.Region.Depth, region.Run?.RunSeconds ?? 0f);
            _random = region.Random("waves");
            region.Threat = this;
            PrewarmPools();
        }

        // Every Wave enemy made at load, not mid-fight: the biggest Wave at its likely escalation, plus a margin.
        private void PrewarmPools()
        {
            int largest = 0;
            foreach (Wave w in _waves) largest = Mathf.Max(largest, w.Count);
            int chasers = Mathf.Min(Escalation.Scale(largest, Multiplier) * 2, 600);
            if (_chaserPrefab != null) Pools.Prewarm(_chaserPrefab, chasers);
            if (_squadPrefab != null) Pools.Prewarm(_squadPrefab, Mathf.Max(8, chasers / 4));
        }

        float IThreat.HealthMultiplier => Multiplier;

        private void OnEnable()
        {
            DebugActions.Button(this, "Waves", "Next Wave now", () => Skip(SecondsUntilNext + 0.01f));
            DebugActions.Toggle(this, "Waves", "Fast Waves (×4 Wave clock)", () => ClockSpeed > 1f, on => ClockSpeed = on ? 4f : 1f);
            DebugActions.Info(this, "Waves", () => $"Next Wave in {SecondsUntilNext:0}s · ×{Multiplier:0.00} health");
        }

        private void OnDisable() => DebugActions.Remove(this);

        void IRegionEndListener.OnRegionEnded(RegionEnd end) => enabled = false;

        private void Update()
        {
            using (PerfMarkers.Waves.Auto()) Tick();
        }

        private void Tick()
        {
            _clock += Time.deltaTime * ClockSpeed;
            _due.Clear();
            Schedule.Release(_clock, _due);
            foreach (Wave wave in _due) Queue(wave);
            SpawnPending();
        }

        private void Queue(Wave wave)
        {
            _released++;
            RegionGrid grid = RegionGrid.Current;
            if (grid == null) return;
            bool squad = wave.Type == WaveType.Squad && _squadPrefab != null;
            GameObject prefab = squad ? _squadPrefab : _chaserPrefab;
            int count = Escalation.Scale(squad ? Mathf.Max(1, wave.Count / 4) : wave.Count, Multiplier);
            int queued = 0;
            bool clumped = wave.Type is WaveType.Horde or WaveType.Squad;
            Vector3 centre = default;
            if (clumped && !TrySpawnSpot(grid, out centre)) clumped = false;
            float clump = wave.Type == WaveType.Squad ? 1.5f : 4f;
            for (int i = 0; i < count; i++)
            {
                Vector3 at = default;
                if (clumped)
                {
                    // The clump may hang off the map or over walls: try a few spots, widening a little each time.
                    bool found = false;
                    for (int tries = 0; tries < 8 && !found; tries++)
                    {
                        float r = clump * (1f + tries * 0.25f);
                        at = centre + new Vector3((float)(_random.NextDouble() * 2 - 1) * r, (float)(_random.NextDouble() * 2 - 1) * r, 0f);
                        found = grid.IsWalkable(at);
                    }
                    if (!found) continue;
                }
                else if (!TrySpawnSpot(grid, out at)) continue;
                _pending.Enqueue((prefab, at, wave.IsFinal));
                queued++;
            }
            if (wave.IsFinal) RegionGoal.Current?.Capture.FinalWaveSpawned(queued);
            BLog.Info("Waves", $"Wave {_released}/{Schedule.Count} ({wave.Type}): {queued} enemies, x{Multiplier:F2}");
        }

        private void SpawnPending()
        {
            float multiplier = Multiplier;
            for (int i = 0; i < _perFrame && _pending.Count > 0; i++)
            {
                (GameObject prefab, Vector3 at, bool final) = _pending.Dequeue();
                GameObject go = Pools.Spawn(prefab, at, Quaternion.identity);
                var member = go.GetComponent<HordeMember>();
                member.ScaleHealth(multiplier);
                member.SetRoaming(false);
                if (final) CountTowardCapture(member.Damageable);
            }
        }

        private static void CountTowardCapture(Damageable damageable)
        {
            void OnDied(DamageResult _)
            {
                damageable.Died -= OnDied;
                RegionGoal.Current?.Capture.FinalWaveEnemyDied();
            }
            damageable.Died += OnDied;
        }

        private bool TrySpawnSpot(RegionGrid grid, out Vector3 spot) => TryRingSpot(grid, out spot) || TryEdgeSpot(grid, out spot);

        // A walkable cell in the ring around the Main Base, far from every player target.
        private bool TryRingSpot(RegionGrid grid, out Vector3 spot)
        {
            spot = default;
            Building home = null;
            foreach (Building b in Building.All)
                if (b.IsMainBase && b.IsPlayers && b.IsAlive) { home = b; break; }
            if (home == null) return false;
            float min = Mathf.Min(_minDistanceFromTargets, _maxDistanceFromBase);
            return grid.TryPickSpot(_random, grid.Ring(home.transform.position, min, _maxDistanceFromBase), FarFromTargets, out spot);
        }

        // A walkable cell in the band along the map's edges, far from every player target.
        private bool TryEdgeSpot(RegionGrid grid, out Vector3 spot) => grid.TryPickSpot(_random, grid.EdgeBand(_edgeBand), FarFromTargets, out spot);

        private bool FarFromTargets(Vector3 at) => HordeTarget.NoneHostileWithin(at, _minDistanceFromTargets);
    }
}
