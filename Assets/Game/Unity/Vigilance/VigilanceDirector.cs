using System;
using System.Collections.Generic;
using Blondie.Core;
using Blondie.Pooling;
using UnityEngine;
using Random = System.Random;

namespace Isekai
{
    /// <summary>
    /// A Region's Vigilance: raises it when Jack's side kills mortals, decays it while nothing is seen, drives the
    /// Region's <see cref="IThreat"/>, and sends Waves of mortals at the player's base: from the map edge, or from a
    /// <see cref="VigilanceSpawnCircle"/> while one stands (every second Wave). The rules are in
    /// <see cref="VigilanceMachine"/> and <see cref="VigilanceWavePlanner"/>.
    /// </summary>
    [RegionSystem]
    public sealed class VigilanceDirector : MonoBehaviour, IRegionSetup, IRegionPopulate, IThreat
    {
        /// <summary>One Wave: the mortals it sent that are still alive.</summary>
        public sealed class Wave
        {
            public readonly List<HordeMember> Members = new();
            public Vector3 Origin;
            public bool FromCircle;
        }

        private const float MemberScatterCells = 3f;
        private const float PruneInterval = 0.5f;

        private readonly VigilanceMachine _machine = new();
        private readonly VigilanceWavePlanner _planner = new(VigilanceTuning.OuterRingWaveCap, VigilanceTuning.CrusadeRepeatSeconds);
        private readonly List<Wave> _alive = new();
        private readonly Dictionary<GameObject, Wave> _waveOf = new();
        private readonly List<VigilanceSpawnCircle> _circles = new();

        private Random _random;
        private GameObject _prefab;
        private float _retryAt;
        private float _nextPrune;

        public static VigilanceDirector Current { get; private set; }

        public VigilanceStage Stage => _machine.Stage;
        public float Points => _machine.Points;
        public float ProgressToNext => _machine.ProgressToNext;

        /// <summary>Waves sent so far, and how many came from the edge and from circles.</summary>
        public int WavesSent { get; private set; }
        public int WavesFromEdge { get; private set; }
        public int WavesFromCircle { get; private set; }

        public int AliveWaves => _planner.Alive;
        public int PendingWaves => _planner.Pending;
        public Wave LastWave { get; private set; }
        public IReadOnlyList<VigilanceSpawnCircle> Circles => _circles;

        /// <summary>How many spawn circles still stand.</summary>
        public int StandingCircles
        {
            get
            {
                int n = 0;
                foreach (VigilanceSpawnCircle c in _circles)
                    if (c != null && c.IsAlive) n++;
                return n;
            }
        }

        /// <summary>What new mortals' health is multiplied by at this stage.</summary>
        public float HealthMultiplier => VigilanceTuning.HealthMultiplier(_machine.Stage);

        int IRegionSetup.SetupOrder => RegionOrder.Settings;
        int IRegionPopulate.PopulateOrder => RegionOrder.Patrols;

        private void OnEnable()
        {
            Current = this;
            RegionEntities.Died += OnDied;
        }

        private void OnDisable()
        {
            if (Current == this) Current = null;
            RegionEntities.Died -= OnDied;
        }

        void IRegionSetup.Setup(RegionContext region)
        {
            region.Threat = this;
            _random = region.Random("vigilance");
            HordeSpawner spawner = FindAnyObjectByType<HordeSpawner>();
            _prefab = spawner != null ? spawner.Prefab : null;
            if (_prefab != null) Pools.Prewarm(_prefab, VigilanceTuning.WaveSize(VigilanceStage.Crusade) * VigilanceTuning.OuterRingWaveCap);
        }

        void IRegionPopulate.Populate(RegionContext region)
        {
            RegionGrid grid = RegionGrid.Current;
            if (grid == null) return;
            Transform jack = Player.Character != null ? Player.Character.transform : null;
            for (int i = 0; i < VigilanceTuning.SpawnCircleCount; i++)
            {
                bool placed = grid.TryPickSpot(_random, grid.EdgeBand(VigilanceTuning.EdgeBandCells + 6),
                    at => HordeTarget.NoneHostileWithin(at, VigilanceTuning.MinSpawnDistanceFromTargets)
                          && (jack == null || (at - jack.position).magnitude >= VigilanceTuning.SpawnCircleMinDistanceFromJack),
                    out Vector3 spot, attempts: 60);
                if (placed) _circles.Add(VigilanceSpawnCircle.Create(spot, transform));
            }
        }

        private void Update()
        {
            using (PerfMarkers.HordeAggro.Auto())
            {
                float dt = Time.deltaTime;
                _machine.Tick(dt);
                _planner.Tick(dt, _machine.Stage);
                if (Time.time >= _nextPrune) { _nextPrune = Time.time + PruneInterval; Prune(); }
                if (_planner.CanSend && Time.time >= _retryAt) TrySend();
            }
        }

        // Jack's side killed a mortal.
        private void OnDied(UnitDeath death)
        {
            ForgetMember(death.Unit.GameObject);
            if (death.KillerSide != Side.Yours || death.Side != Side.Mortals) return;
            // Witnessed kills are ticket #17: until then every kill counts as unseen.
            if (_machine.OnKill(death.Kind, witnessed: false)) Reached();
        }

        /// <summary>A rider reached the Seat (ticket #17 raises this).</summary>
        public void RiderReachedSeat()
        {
            if (_machine.OnRiderReachedSeat()) Reached();
        }

        /// <summary>Jack or the Anchor was spotted (ticket #17 raises this).</summary>
        public void Spotted()
        {
            if (_machine.OnSpotted()) Reached();
        }

        /// <summary>Debug: raises a stage.</summary>
        public void RaiseStage()
        {
            if (_machine.RaiseStage()) Reached();
        }

        /// <summary>Debug: lowers a stage.</summary>
        public void LowerStage()
        {
            if (_machine.LowerStage()) Reached();
        }

        /// <summary>Debug: sends the next Wave now (unless the cap of Waves is already alive).</summary>
        public bool SendWaveNow()
        {
            if (!_planner.OweNow()) return false;
            return TrySend();
        }

        public string Readout() =>
            $"Vigilance {VigilanceStageNames.Name(_machine.Stage)} {_machine.Points:0.#} · Waves {_planner.Alive}/{_planner.Cap} alive, {_planner.Pending} due, {WavesSent} sent · circles {StandingCircles}";

        private void Reached() => _planner.Reached(_machine.Highest);

        private bool TrySend()
        {
            if (_prefab == null || RegionGrid.Current == null) return false;
            VigilanceSpawnCircle circle = null;
            bool fromCircle = WavesSent % 2 == 1 && TryPickCircle(out circle);
            Vector3 origin;
            if (fromCircle) { origin = circle.transform.position; }
            else if (!RegionGrid.Current.TryPickSpot(_random, RegionGrid.Current.EdgeBand(VigilanceTuning.EdgeBandCells),
                         at => HordeTarget.NoneHostileWithin(at, VigilanceTuning.MinSpawnDistanceFromTargets), out origin, attempts: 60))
            {
                _retryAt = Time.time + VigilanceTuning.WaveRetrySeconds;
                return false;
            }

            var wave = new Wave { Origin = origin, FromCircle = fromCircle };
            SpawnMembers(wave, VigilanceTuning.WaveSize(_machine.Stage));
            if (wave.Members.Count == 0)
            {
                _retryAt = Time.time + VigilanceTuning.WaveRetrySeconds;
                return false;
            }

            _planner.Sent();
            _alive.Add(wave);
            LastWave = wave;
            WavesSent++;
            if (fromCircle) WavesFromCircle++; else WavesFromEdge++;
            return true;
        }

        private bool TryPickCircle(out VigilanceSpawnCircle circle)
        {
            circle = null;
            int standing = StandingCircles;
            if (standing == 0) return false;
            int pick = _random.Next(standing);
            foreach (VigilanceSpawnCircle c in _circles)
            {
                if (c == null || !c.IsAlive) continue;
                if (pick-- == 0) { circle = c; return true; }
            }
            return false;
        }

        // The mortals appear in a clump round the Wave's origin, already attacking (not roaming).
        private void SpawnMembers(Wave wave, int count)
        {
            RegionGrid grid = RegionGrid.Current;
            Int2 centre = grid.CellAt(wave.Origin);
            float multiplier = HealthMultiplier;
            for (int i = 0; i < count; i++)
            {
                if (!grid.TryPickSpot(_random, SpawnPlacement.Near(centre, Mathf.CeilToInt(MemberScatterCells)),
                        at => HordeTarget.NoneHostileWithin(at, VigilanceTuning.MinSpawnDistanceFromTargets),
                        out Vector3 spot, jitter: 0.6f, attempts: 12))
                    spot = wave.Origin;
                GameObject go = Pools.Spawn(_prefab, spot, Quaternion.identity);
                if (!go.TryGetComponent(out HordeMember member)) { Pools.Despawn(go); continue; }
                member.SetRoaming(false);
                if (!Mathf.Approximately(multiplier, 1f)) member.ScaleHealth(multiplier);
                wave.Members.Add(member);
                _waveOf[go] = wave;
            }
        }

        // A dead (or despawned) mortal leaves its Wave; a Wave with none left is over.
        private void ForgetMember(GameObject go)
        {
            if (_waveOf.Count == 0 || go == null || !_waveOf.Remove(go, out Wave wave)) return;
            for (int i = wave.Members.Count - 1; i >= 0; i--)
                if (wave.Members[i] == null || wave.Members[i].gameObject == go) wave.Members.RemoveAt(i);
            EndIfOver(wave);
        }

        private void Prune()
        {
            for (int w = _alive.Count - 1; w >= 0; w--)
            {
                Wave wave = _alive[w];
                for (int i = wave.Members.Count - 1; i >= 0; i--)
                {
                    HordeMember m = wave.Members[i];
                    if (m != null && m.IsAlive) continue;
                    if (m != null) _waveOf.Remove(m.gameObject);
                    wave.Members.RemoveAt(i);
                }
                EndIfOver(wave);
            }
        }

        private void EndIfOver(Wave wave)
        {
            if (wave.Members.Count > 0 || !_alive.Remove(wave)) return;
            _planner.Ended();
        }
    }
}
