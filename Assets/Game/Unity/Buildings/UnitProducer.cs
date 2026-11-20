using System;
using System.Collections.Generic;
using Blondie.Pooling;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// A Portal: it keeps demons alive, one every few seconds up to a cap (Imps or bone demons, <see cref="Roster"/>),
    /// and gives them a standing <see cref="Order"/> (Guard its rally point, Raid the marked target, Follow Jack), a
    /// <see cref="Rally"/> point to gather at and a <see cref="Policy"/> for bone piles (Harvest, Destroy, Ignore). The
    /// demons read these every time they think, so a change reaches them at once. Switched on by its
    /// <see cref="Building"/> once built; set from the Portal panel (<see cref="PortalPanel"/>).
    /// </summary>
    public sealed class UnitProducer : MonoBehaviour
    {
        [Tooltip("The Imp: the basic Portal demon.")]
        [SerializeField] private GameObject _unitPrefab;
        [SerializeField] private GameObject _boneDemonPrefab;
        [SerializeField, Min(0.1f)] private float _interval = 6f;
        [SerializeField, Min(0)] private int _cap = 4;
        [SerializeField] private PortalOrder _order = PortalOrder.Guard;
        [SerializeField] private BonesPolicy _policy = BonesPolicy.Harvest;
        [SerializeField] private DemonType _roster = DemonType.Imp;
        [Tooltip("Where Units appear, from the Building's centre.")]
        [SerializeField] private Vector2 _exit = new(0f, -1.4f);

        private readonly List<DemonBrain> _units = new();
        private Production _production;
        private Vector3 _rally;
        private Vector3 _raid;
        private bool _hasRally;
        private bool _hasRaid;
        private int _extras;

        /// <summary>The standing order its demons follow.</summary>
        public PortalOrder Order
        {
            get => _order;
            set { if (_order == value) return; _order = value; Changed?.Invoke(); }
        }

        /// <summary>What its demons do about bone piles.</summary>
        public BonesPolicy Policy
        {
            get => _policy;
            set { if (_policy == value) return; _policy = value; Changed?.Invoke(); }
        }

        /// <summary>The kind of demon it summons next (the ones already summoned stay).</summary>
        public DemonType Roster
        {
            get => _roster;
            set { if (_roster == value || value == DemonType.Skeleton) return; _roster = value; Changed?.Invoke(); }
        }

        /// <summary>Where its demons gather under Guard: the point set from the panel, else the Portal itself.</summary>
        public Vector3 Rally => _hasRally ? _rally : transform.position;
        public bool HasRally => _hasRally;

        /// <summary>The target a Raid order sends them to.</summary>
        public bool HasRaidTarget => _hasRaid;
        public Vector3 RaidTarget => _raid;

        public IReadOnlyList<DemonBrain> Units => _units;
        public Production Production => _production;

        /// <summary>The order, rally point, raid target, policy or roster changed.</summary>
        public event Action Changed;

        public void SetRally(Vector3 at)
        {
            _rally = new Vector3(at.x, at.y, 0f);
            _hasRally = true;
            Changed?.Invoke();
        }

        /// <summary>Marks a target to raid and orders a Raid.</summary>
        public void MarkRaid(Vector3 at)
        {
            _raid = new Vector3(at.x, at.y, 0f);
            _hasRaid = true;
            _order = PortalOrder.Raid;
            Changed?.Invoke();
        }

        /// <summary>Where its demons stand now: the order, resolved against the rally point, the mark and Jack.</summary>
        public DemonPost Resolve()
        {
            CharacterControl jack = Player.Character;
            bool hasJack = jack != null && !jack.IsDead;
            Vector3 rally = Rally;
            Vector3 jackAt = hasJack ? jack.transform.position : Vector3.zero;
            return DemonPosts.Resolve(_order, rally.x, rally.y, _hasRaid, _raid.x, _raid.y, hasJack, jackAt.x, jackAt.y);
        }

        /// <summary>Puts a demon of this Portal's (a raised skeleton) in a slot, without counting it against the cap.</summary>
        internal void Adopt(DemonBrain unit) => unit.Assign(this, _units.Count + 1 + _extras++ % 8);

        /// <summary>A demon leaves (it turned rogue): it stops following this Portal's order and its place under the cap is free.</summary>
        internal void Release(DemonBrain unit) => _units.Remove(unit);

        /// <summary>Whether the Portal works and has room under its cap for another demon.</summary>
        public bool HasRoom => isActiveAndEnabled && _production != null && _units.Count < _production.Cap;

        /// <summary>Takes a demon back under this Portal's order (one Jack won back), when there is room. False when there is none.</summary>
        internal bool TryTake(DemonBrain unit)
        {
            if (!HasRoom || _units.Contains(unit)) return false;
            unit.Assign(this, _units.Count);
            _units.Add(unit);
            return true;
        }

        private void Awake() => _production = new Production(_interval, _cap);

        private void Update()
        {
            _units.RemoveAll(u => u == null || !u.isActiveAndEnabled);
            if (!_production.Tick(Time.deltaTime, _units.Count)) return;
            GameObject prefab = _roster == DemonType.BoneDemon && _boneDemonPrefab != null ? _boneDemonPrefab : _unitPrefab;
            Vector3 at = transform.position + (Vector3)_exit;
            GameObject go = Pools.Spawn(prefab, at, Quaternion.identity);
            var unit = go.GetComponent<DemonBrain>();
            unit.Assign(this, _units.Count);
            _units.Add(unit);
        }
    }
}
