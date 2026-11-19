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
        [Tooltip("The horde's team: it walks to and hits only targets hostile to this team (Blondie Combat's Teams).")]
        [SerializeField] private string _team = TeamIds.Enemy;
        [SerializeField] private FlowFieldCrowd _crowd;
        [SerializeField, Min(0.25f)] private float _hashCellSize = 1f;
        [Tooltip("An enemy's body radius, for ability hits.")]
        [SerializeField, Min(0f)] private float _memberRadius = 0.25f;
        [Tooltip("Roaming enemies this close to a player target (the Character, a Unit, a Building) wake and attack.")]
        [SerializeField, Min(0f)] private float _aggroRadius = 7f;
        [Tooltip("When a roaming enemy is hurt, the roamers this close to it wake too.")]
        [SerializeField, Min(0f)] private float _alarmRadius = 5f;
        [SerializeField, Min(0.05f)] private float _aggroInterval = 0.25f;

        private readonly List<HordeMember> _members = new();
        private readonly List<HordeMember> _nearby = new();
        private readonly List<HordeTarget> _targets = new();
        private readonly List<Int2> _targetCells = new();
        private readonly List<Int2> _lastCells = new();
        private SpatialHash<HordeMember> _hash;
        private float _nextAggro;
        private HordeWorld _world;
        private IAbilityWorld _previousWorld;

        public static HordeDirector Current { get; private set; }

        public int Count => _members.Count;

        /// <summary>Every horde enemy now, roaming or attacking.</summary>
        public IReadOnlyList<HordeMember> Members => _members;

        /// <summary>Enemies in the attacking crowd (the rest are roaming).</summary>
        public int Attacking => _crowd.Count;

        /// <summary>Horde enemies killed since this director started.</summary>
        public int Kills { get; private set; }
        public FlowFieldGrid Grid => _grid.Grid;

        /// <summary>The team this horde fights for.</summary>
        public string Team => _team;
        public SpatialHash<HordeMember> Hash => _hash ??= new SpatialHash<HordeMember>(_hashCellSize);

        // When the Region's walls are baked again, the field over the targets must be requested again.
        private void OnGridBaked() => _lastCells.Clear();

        private void OnEnable()
        {
            Current = this;
            if (RegionGrid.Current != null) RegionGrid.Current.Baked += OnGridBaked;
            _world ??= new HordeWorld(Hash, _memberRadius);
            _previousWorld = AbilityWorld.Current;
            AbilityWorld.Current = _world;
            foreach (HordeMember m in FindObjectsByType<HordeMember>(FindObjectsInactive.Exclude)) Register(m);
        }

        private void OnDisable()
        {
            if (Current == this) Current = null;
            if (RegionGrid.Current != null) RegionGrid.Current.Baked -= OnGridBaked;
            if (AbilityWorld.Current == _world) AbilityWorld.Current = _previousWorld;
        }

        internal void Register(HordeMember member)
        {
            if (_members.Contains(member)) return;
            _members.Add(member);
            if (!member.IsRoaming) _crowd.Add(member.transform, member.SpeedMultiplier);
        }

        /// <summary>A roaming member starts attacking: it joins the crowd that follows the field.</summary>
        internal void Wake(HordeMember member)
        {
            if (_members.Contains(member) && !_crowd.Contains(member.transform)) _crowd.Add(member.transform, member.SpeedMultiplier);
        }

        /// <summary>A member stops and roams: it leaves the crowd.</summary>
        internal void Sleep(HordeMember member)
        {
            if (_crowd != null) _crowd.Remove(member.transform);
        }

        /// <summary>Wakes every roaming member within <paramref name="radius"/> of <paramref name="point"/>.</summary>
        public void WakeAround(Vector3 point, float radius)
        {
            _nearby.Clear();
            Hash.Query(point.x, point.y, radius, _nearby);
            foreach (HordeMember m in _nearby)
                if (m != null && m.IsAlive && m.IsRoaming) m.Wake();
        }

        internal void Alarm(HordeMember hurt) => WakeAround(hurt.transform.position, _alarmRadius);

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
            if (Time.time < _nextAggro) return;
            _nextAggro = Time.time + _aggroInterval;
            foreach (HordeTarget target in HordeTarget.All)
                if (target.IsAlive && IsHostile(target)) WakeAround(target.transform.position, _aggroRadius + target.Radius);
        }

        // One field over every living target's cells; a new one only when that set of cells changes.
        private void RetargetIfNeeded()
        {
            _targetCells.Clear();
            foreach (HordeTarget target in HordeTarget.All)
                if (target.IsAlive && target.AttractsHorde && IsHostile(target)) target.AddCells(Grid, _targetCells);
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
            // A copy: a hit can kill a target, which leaves the list (or clones the Character, which joins it).
            _targets.Clear();
            _targets.AddRange(HordeTarget.All);
            foreach (HordeTarget target in _targets)
            {
                if (target == null) continue;
                if (!target.IsAlive || !IsHostile(target)) continue;
                Vector3 p = target.transform.position;
                _nearby.Clear();
                Hash.Query(p.x, p.y, target.Radius + slack + 1f, _nearby);
                foreach (HordeMember m in _nearby)
                {
                    if (!m.IsAlive || !Teams.AreHostile(m.Unit, target.Damageable.Unit)) continue;
                    float reach = target.Radius + m.Reach + slack;
                    if ((m.transform.position - p).sqrMagnitude > reach * reach) continue;
                    if (!m.Attack.TryHit(now)) continue;
                    target.Damageable.Unit.TakeDamage(m.Attack.Damage, DamageRules.PhysicalType, m.Unit);
                    if (!target.IsAlive) break;
                }
            }
        }

        private bool IsHostile(HordeTarget target) => Teams.Relation(_team, target.Team) == TeamRelation.Hostile;

        private static bool SameCells(List<Int2> a, List<Int2> b)
        {
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
                if (a[i].x != b[i].x || a[i].y != b[i].y) return false;
            return true;
        }
    }
}
