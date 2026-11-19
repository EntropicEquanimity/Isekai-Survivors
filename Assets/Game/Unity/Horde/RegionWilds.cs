using System;
using System.Collections.Generic;
using Blondie.Core;
using Blondie.FlowFields;
using Blondie.Pooling;
using UnityEngine;
using Random = System.Random;

namespace Isekai
{
    /// <summary>
    /// Fills the open Region with life once the horde's grid is ready: small groups of roaming enemies that amble around
    /// their home spot until a player target comes near (then they attack like any roamer), and extra Resource nodes.
    /// Both keep away from the Main Base. Wanderers get the same health escalation as the Region's Waves.
    /// </summary>
    public sealed class RegionWilds : MonoBehaviour, IRegionPopulate
    {
        [Header("Wanderers")]
        [SerializeField] private GameObject _wandererPrefab;
        [SerializeField, Min(0)] private int _groups = 45;
        [SerializeField] private Vector2Int _groupSize = new(2, 6);
        [SerializeField, Min(0f)] private float _wanderRadius = 4f;
        [SerializeField, Min(0f)] private float _wanderSpeed = 0.5f;
        [Header("Resources")]
        [SerializeField] private GameObject _nodePrefab;
        [SerializeField, Min(0)] private int _nodes = 24;
        [Header("Placement")]
        [Tooltip("Nothing appears closer than this to a player target.")]
        [SerializeField, Min(0f)] private float _minDistanceFromTargets = 25f;

        private readonly List<Group> _wanderers = new();
        private Random _random;
        private bool _populated;

        public int GroupCount => _wanderers.Count;

        /// <summary>Living wanderers still roaming.</summary>
        public int Roaming
        {
            get
            {
                int n = 0;
                foreach (Group g in _wanderers)
                    foreach (HordeMember m in g.Members)
                        if (m != null && m.IsAlive && m.IsRoaming) n++;
                return n;
            }
        }

        private sealed class Group
        {
            public Vector3 Home;
            public Vector3 Goal;
            public float Rest;
            public readonly List<HordeMember> Members = new();
            public readonly List<Vector3> Offsets = new();
        }

        private void Update()
        {
            if (_populated) Wander(Time.deltaTime);
        }

        /// <summary>Fills the Region once its walls are known: Resource nodes, then wandering groups.</summary>
        void IRegionPopulate.Populate(RegionContext region)
        {
            RegionGrid grid = RegionGrid.Current;
            if (grid == null) return;
            _populated = true;
            _random = region.Random("wilds");
            float health = region.HealthMultiplier;

            for (int i = 0; i < _nodes && _nodePrefab != null; i++)
                if (TryPickSpot(grid, out Vector3 at)) Instantiate(_nodePrefab, at, Quaternion.identity, transform);

            for (int i = 0; i < _groups && _wandererPrefab != null; i++)
            {
                if (!TryPickSpot(grid, out Vector3 home)) continue;
                var group = new Group { Home = home, Goal = home, Rest = (float)_random.NextDouble() * 4f };
                int size = _random.Next(Mathf.Max(1, _groupSize.x), Mathf.Max(_groupSize.x, _groupSize.y) + 1);
                for (int k = 0; k < size; k++)
                {
                    var offset = new Vector3((float)_random.NextDouble() * 1.6f - 0.8f, (float)_random.NextDouble() * 1.6f - 0.8f, 0f);
                    GameObject go = Pools.Spawn(_wandererPrefab, home + offset, Quaternion.identity);
                    var member = go.GetComponent<HordeMember>();
                    member.SetRoaming(true);
                    if (!Mathf.Approximately(health, 1f)) member.ScaleHealth(health);
                    group.Members.Add(member);
                    group.Offsets.Add(offset);
                }
                _wanderers.Add(group);
            }
        }

        // Each group strolls to a spot near its home, rests, and picks another. Woken members leave the group's control.
        private void Wander(float dt)
        {
            RegionGrid grid = RegionGrid.Current;
            foreach (Group g in _wanderers)
            {
                // Dead or woken members leave for good: the pool may hand the same object to a nest or a Wave later.
                for (int i = 0; i < g.Members.Count; i++)
                    if (g.Members[i] != null && (!g.Members[i].IsAlive || !g.Members[i].IsRoaming)) g.Members[i] = null;
                if (g.Rest > 0f) { g.Rest -= dt; continue; }
                bool arrived = true;
                for (int i = 0; i < g.Members.Count; i++)
                {
                    HordeMember m = g.Members[i];
                    if (m == null) continue;
                    Transform t = m.transform;
                    Vector3 target = g.Goal + g.Offsets[i];
                    Vector3 next = Vector3.MoveTowards(t.position, target, _wanderSpeed * dt);
                    if (grid != null && !grid.IsWalkable(next)) continue;
                    t.position = next;
                    if ((target - next).sqrMagnitude > 0.0025f) arrived = false;
                }
                if (!arrived) continue;
                g.Rest = 2f + (float)_random.NextDouble() * 5f;
                var step = new Vector2((float)_random.NextDouble() * 2f - 1f, (float)_random.NextDouble() * 2f - 1f) * _wanderRadius;
                Vector3 goal = g.Home + new Vector3(step.x, step.y, 0f);
                if (grid == null || grid.IsWalkable(goal)) g.Goal = goal;
            }
        }

        private bool TryPickSpot(RegionGrid grid, out Vector3 spot) =>
            grid.TryPickSpot(_random, grid.Anywhere(), at => HordeTarget.NoneHostileWithin(at, _minDistanceFromTargets), out spot, attempts: 40);
    }
}
