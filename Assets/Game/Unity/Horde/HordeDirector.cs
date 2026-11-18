using System.Collections.Generic;
using Blondie.Abilities;
using Blondie.Combat;
using Blondie.Core;
using Blondie.FlowFields;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// Runs a Region's horde. One <see cref="FlowFieldCrowd"/> moves every <see cref="HordeMember"/> toward the nearest
    /// living <see cref="HordeTarget"/> (one shared flow field over all targets' cells). After the crowd moves, a spatial
    /// hash of the members is rebuilt; it answers ability queries (<see cref="HordeWorld"/>, installed as
    /// <see cref="AbilityWorld.Current"/>) and finds which enemies are close enough to hit a target.
    /// </summary>
    public sealed class HordeDirector : MonoBehaviour
    {
        [SerializeField] private FlowFieldGridComponent _grid;
        [SerializeField] private FlowFieldCrowd _crowd;
        [SerializeField, Min(0.25f)] private float _hashCellSize = 1f;
        [Tooltip("An enemy's body radius, for ability hits.")]
        [SerializeField, Min(0f)] private float _memberRadius = 0.25f;

        private readonly List<HordeMember> _members = new();
        private readonly List<HordeMember> _nearby = new();
        private readonly List<Int2> _targetCells = new();
        private readonly List<Int2> _lastCells = new();
        private SpatialHash<HordeMember> _hash;
        private HordeWorld _world;
        private IAbilityWorld _previousWorld;

        public static HordeDirector Current { get; private set; }

        public int Count => _members.Count;

        /// <summary>Horde enemies killed since this director started.</summary>
        public int Kills { get; private set; }
        public FlowFieldGrid Grid => _grid.Grid;
        public SpatialHash<HordeMember> Hash => _hash ??= new SpatialHash<HordeMember>(_hashCellSize);

        private void OnEnable()
        {
            Current = this;
            _world ??= new HordeWorld(Hash, _memberRadius);
            _previousWorld = AbilityWorld.Current;
            AbilityWorld.Current = _world;
            foreach (HordeMember m in FindObjectsByType<HordeMember>(FindObjectsInactive.Exclude)) Register(m);
        }

        private void OnDisable()
        {
            if (Current == this) Current = null;
            if (AbilityWorld.Current == _world) AbilityWorld.Current = _previousWorld;
        }

        internal void Register(HordeMember member)
        {
            if (_members.Contains(member)) return;
            _members.Add(member);
            _crowd.Add(member.transform, member.SpeedMultiplier);
        }

        internal void Unregister(HordeMember member)
        {
            if (!_members.Remove(member)) return;
            if (member.Unit.IsDead) Kills++;
            if (_crowd != null) _crowd.Remove(member.transform);
        }

        private void Update() => RetargetIfNeeded();

        private void LateUpdate()
        {
            _crowd.CompleteJobs();
            RebuildHash();
            HitTargets();
        }

        // One field over every living target's cells; a new one only when that set of cells changes.
        private void RetargetIfNeeded()
        {
            _targetCells.Clear();
            foreach (HordeTarget target in HordeTarget.All)
                if (target.IsAlive && target.AttractsHorde) target.AddCells(Grid, _targetCells);
            if (SameCells(_targetCells, _lastCells)) return;
            _lastCells.Clear();
            _lastCells.AddRange(_targetCells);
            if (_targetCells.Count == 0) return;
            _crowd.Field = FlowFields.Request(Grid, _targetCells.ToArray());
        }

        private void RebuildHash()
        {
            SpatialHash<HordeMember> hash = Hash;
            hash.Clear();
            foreach (HordeMember m in _members)
            {
                Vector3 p = m.transform.position;
                hash.Insert(m, p.x, p.y);
            }
        }

        private void HitTargets()
        {
            float now = Time.time;
            // The crowd stops enemies anywhere inside a target's goal cells, up to about a cell from its centre.
            float slack = Grid.CellSize * 0.75f;
            foreach (HordeTarget target in HordeTarget.All)
            {
                if (!target.IsAlive) continue;
                Vector3 p = target.transform.position;
                _nearby.Clear();
                Hash.Query(p.x, p.y, target.Radius + slack + 1f, _nearby);
                foreach (HordeMember m in _nearby)
                {
                    if (!m.IsAlive) continue;
                    float reach = target.Radius + m.Reach + slack;
                    if ((m.transform.position - p).sqrMagnitude > reach * reach) continue;
                    if (!m.Attack.TryHit(now)) continue;
                    target.Damageable.Unit.TakeDamage(m.Attack.Damage, DamageRules.PhysicalType, m.Unit);
                    if (!target.IsAlive) break;
                }
            }
        }

        private static bool SameCells(List<Int2> a, List<Int2> b)
        {
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
                if (a[i].x != b[i].x || a[i].y != b[i].y) return false;
            return true;
        }
    }
}
