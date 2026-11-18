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
    public sealed class WaveDirector : MonoBehaviour
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
        [Tooltip("How far in from the map's edge Waves appear.")]
        [SerializeField, Min(1)] private int _edgeBand = 6;
        [SerializeField, Min(0f)] private float _minDistanceFromTargets = 20f;
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

        private void Awake() => _random = new Random(_seed);

        private void Update()
        {
            _clock += Time.deltaTime;
            _due.Clear();
            Schedule.Release(_clock, _due);
            foreach (Wave wave in _due) Queue(wave);
            SpawnPending();
        }

        private void Queue(Wave wave)
        {
            _released++;
            FlowFieldGrid grid = HordeDirector.Current != null ? HordeDirector.Current.Grid : null;
            if (grid == null) return;
            bool squad = wave.Type == WaveType.Squad && _squadPrefab != null;
            GameObject prefab = squad ? _squadPrefab : _chaserPrefab;
            int count = Escalation.Scale(squad ? Mathf.Max(1, wave.Count / 4) : wave.Count, Multiplier);
            int queued = 0;
            bool clumped = wave.Type is WaveType.Horde or WaveType.Squad;
            Vector3 centre = default;
            if (clumped && !TryEdgeSpot(grid, out centre)) clumped = false;
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
                        found = grid.IsWalkable(new Float3(at.x, at.y, 0f));
                    }
                    if (!found) continue;
                }
                else if (!TryEdgeSpot(grid, out at)) continue;
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

        // A walkable cell in the band along the map's edges, far from every player target.
        private bool TryEdgeSpot(FlowFieldGrid grid, out Vector3 spot)
        {
            float min2 = _minDistanceFromTargets * _minDistanceFromTargets;
            int band = Mathf.Min(_edgeBand, Mathf.Min(grid.Width, grid.Height) / 2);
            for (int attempt = 0; attempt < 30; attempt++)
            {
                int side = _random.Next(4);
                int along = _random.Next(side < 2 ? grid.Width : grid.Height);
                int depth = _random.Next(band);
                Int2 cell = side switch
                {
                    0 => new Int2(along, depth),
                    1 => new Int2(along, grid.Height - 1 - depth),
                    2 => new Int2(depth, along),
                    _ => new Int2(grid.Width - 1 - depth, along),
                };
                if (!grid.IsWalkable(cell)) continue;
                Float3 w = grid.CellToWorld(cell);
                spot = new Vector3(w.x, w.y, 0f);
                bool far = true;
                foreach (HordeTarget t in HordeTarget.All)
                    if ((t.transform.position - spot).sqrMagnitude < min2) { far = false; break; }
                if (far) return true;
            }
            spot = default;
            return false;
        }
    }
}
