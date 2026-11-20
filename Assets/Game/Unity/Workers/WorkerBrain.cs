using System;
using System.Collections.Generic;
using Blondie.Controllers;
using Blondie.Core;
using Blondie.UI;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// A worker demon: picks the nearest resource node on corrupted ground, mines up to its carry capacity, delivers to
    /// the nearest drop-off (Anchor or built Outpost), then repeats.
    /// </summary>
    [RequireComponent(typeof(CharacterMotor))]
    public sealed class WorkerBrain : MonoBehaviour, IRegionEndListener
    {
        private enum WorkerState { Idle, ToNode, Mining, ToDropOff }

        private static readonly List<WorkerBrain> s_all = new();

        [SerializeField, Min(0.05f)] private float _thinkInterval = 0.25f;
        [SerializeField, Min(0.1f)] private float _mineReach = 1.15f;
        [SerializeField, Min(0.1f)] private float _dropOffReach = 1.6f;
        [SerializeField, Min(0f)] private float _baseMineRate = 1f;
        [SerializeField, Min(1)] private int _baseCarryCapacity = 8;
        [SerializeField, Min(0f)] private float _idleRadius = 1.4f;

        private readonly MiningProgress _progress = new();
        private CharacterMotor _motor;
        private IStatModifierTarget _stats;
        private WorkerState _state;
        private ResourceNode _node;
        private Building _dropOff;
        private ResourceType _carriedType;
        private int _carriedAmount;
        private float _nextThink;
        private float _elapsed;
        private Vector3 _idleOffset;

        public static IReadOnlyList<WorkerBrain> All => s_all;

        private float MineRate => _stats != null && _stats.HasStat(nameof(StatId.MiningSpeed)) ? _stats.GetStatValue(nameof(StatId.MiningSpeed)) : _baseMineRate;

        private int CarryCapacity
        {
            get
            {
                float value = _stats != null && _stats.HasStat(nameof(StatId.CarryCapacity))
                    ? _stats.GetStatValue(nameof(StatId.CarryCapacity))
                    : _baseCarryCapacity;
                return Mathf.Max(1, Mathf.FloorToInt(value));
            }
        }

        private void Awake()
        {
            _motor = GetComponent<CharacterMotor>();
            _stats = GetComponent<IStatModifierTarget>();
            float angle = (GetHashCode() & 1023) * 2.399963f;
            _idleOffset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * _idleRadius;
            _nextThink = Time.time;
        }

        private void OnEnable() => s_all.Add(this);

        private void OnDisable()
        {
            s_all.Remove(this);
            ReleaseNode();
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            if (Time.time < _nextThink) return;
            float seconds = _elapsed;
            _elapsed = 0f;
            _nextThink = Time.time + _thinkInterval;
            Think(seconds);
        }

        private void Think(float seconds)
        {
            if (_carriedAmount > 0 && _state == WorkerState.Idle) StartDropOff();
            switch (_state)
            {
                case WorkerState.ToNode:
                    TickToNode();
                    break;
                case WorkerState.Mining:
                    TickMining(seconds);
                    break;
                case WorkerState.ToDropOff:
                    TickDropOff();
                    break;
                default:
                    AcquireNodeOrIdle();
                    break;
            }
        }

        private void AcquireNodeOrIdle()
        {
            if (_carriedAmount > 0)
            {
                StartDropOff();
                return;
            }

            ResourceNode node = FindNearestNode();
            if (node == null)
            {
                MoveToIdle();
                return;
            }
            _node = node;
            _state = WorkerState.ToNode;
            _motor.MoveTo(node.transform.position);
        }

        private void TickToNode()
        {
            if (!HasNode())
            {
                ReleaseNode();
                _state = WorkerState.Idle;
                return;
            }

            Vector3 target = _node.transform.position;
            if ((transform.position - target).sqrMagnitude <= _mineReach * _mineReach)
            {
                _progress.Reset();
                _state = WorkerState.Mining;
                if (_motor.HasDestination) _motor.Stop();
                return;
            }

            _motor.MoveTo(target);
        }

        private void TickMining(float seconds)
        {
            if (!HasNode())
            {
                ReleaseNode();
                if (_carriedAmount > 0) StartDropOff();
                else _state = WorkerState.Idle;
                return;
            }

            if ((transform.position - _node.transform.position).sqrMagnitude > _mineReach * _mineReach)
            {
                _state = WorkerState.ToNode;
                return;
            }

            int space = CarryCapacity - _carriedAmount;
            if (space <= 0)
            {
                ReleaseNode();
                StartDropOff();
                return;
            }

            int maxThisTick = Math.Min(space, _node.Remaining);
            int mined = _node.Take(_progress.Tick(seconds, MineRate, maxThisTick));
            if (mined > 0)
            {
                if (_carriedAmount == 0) _carriedType = _node.Type;
                _carriedAmount += mined;
            }

            if (_carriedAmount >= CarryCapacity || _node.IsEmpty)
            {
                ReleaseNode();
                StartDropOff();
            }
        }

        private void StartDropOff()
        {
            _dropOff = FindNearestDropOff();
            _state = WorkerState.ToDropOff;
            if (_dropOff != null) _motor.MoveTo(_dropOff.transform.position);
            else MoveToIdle();
        }

        private void TickDropOff()
        {
            if (_carriedAmount <= 0)
            {
                _state = WorkerState.Idle;
                return;
            }

            if (!IsValidDropOff(_dropOff))
            {
                _dropOff = FindNearestDropOff();
                if (_dropOff == null)
                {
                    MoveToIdle();
                    return;
                }
            }

            Vector3 target = _dropOff.transform.position;
            if ((transform.position - target).sqrMagnitude <= _dropOffReach * _dropOffReach)
            {
                RegionEconomy economy = RegionEconomy.Current;
                if (economy != null) economy.Stockpile.Add(_carriedType, _carriedAmount);
                FloatingText.Spawn(transform.position + Vector3.up, $"+{_carriedAmount} {_carriedType}", Color.white);
                _carriedAmount = 0;
                _state = WorkerState.Idle;
                return;
            }

            _motor.MoveTo(target);
        }

        private void MoveToIdle()
        {
            Building anchor = RegionWorkers.FindAnchor();
            if (anchor == null)
            {
                if (_motor.HasDestination) _motor.Stop();
                return;
            }

            Vector3 idleSpot = anchor.transform.position + _idleOffset;
            if ((transform.position - idleSpot).sqrMagnitude <= 0.25f * 0.25f)
            {
                if (_motor.HasDestination) _motor.Stop();
                return;
            }
            _motor.MoveTo(idleSpot);
        }

        private ResourceNode FindNearestNode()
        {
            RegionGrid grid = RegionGrid.Current;
            if (grid == null) return null;
            CellLayer corruption = grid.Layer(RegionCorruption.LayerName);
            for (int pass = 0; pass < 2; pass++)
            {
                ResourceNode best = null;
                float bestDistance = float.MaxValue;
                IReadOnlyList<ResourceNode> nodes = ResourceNode.All;
                for (int i = 0; i < nodes.Count; i++)
                {
                    ResourceNode node = nodes[i];
                    if (node == null || node.IsEmpty || !node.isActiveAndEnabled || node.IsReservedByOther(this)) continue;
                    if (corruption.Get(grid.CellAt(node.transform.position)) < CorruptionTuning.BuildLevel) continue;
                    float d = (node.transform.position - transform.position).sqrMagnitude;
                    if (d >= bestDistance) continue;
                    bestDistance = d;
                    best = node;
                }
                if (best == null) return null;
                if (best.TryReserve(this)) return best;
            }
            return null;
        }

        private Building FindNearestDropOff()
        {
            Building best = null;
            float bestDistance = float.MaxValue;
            IReadOnlyList<Building> all = Building.All;
            for (int i = 0; i < all.Count; i++)
            {
                Building building = all[i];
                if (!IsValidDropOff(building)) continue;
                float d = (building.transform.position - transform.position).sqrMagnitude;
                if (d >= bestDistance) continue;
                bestDistance = d;
                best = building;
            }
            return best;
        }

        private static bool IsValidDropOff(Building building)
        {
            if (building == null || !building.IsPlayers || !building.IsAlive || !building.IsBuilt) return false;
            if (building.IsMainBase) return true;
            return building.Asset != null && building.Asset.DisplayName == RegionCorruption.OutpostName;
        }

        private bool HasNode() => _node != null && _node.isActiveAndEnabled && !_node.IsEmpty;

        private void ReleaseNode()
        {
            if (_node != null) _node.Release(this);
            _node = null;
            _progress.Reset();
        }

        void IRegionEndListener.OnRegionEnded(RegionEnd end)
        {
            if (end == RegionEnd.Defeated) enabled = false;
        }
    }
}
