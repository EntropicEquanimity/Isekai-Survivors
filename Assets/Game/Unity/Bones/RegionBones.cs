using System;
using System.Collections.Generic;
using Blondie.Pooling;
using UnityEngine;
using Random = System.Random;

namespace Isekai
{
    /// <summary>
    /// The Region's bone piles. A mortal or wildlife death leaves one where it fell; piles crumble away with time and
    /// are used once: whoever uses a pile (the bone demons and a Portal's Harvest policy, ticket #19) calls
    /// <see cref="Take"/>, and whoever destroys it (the Destroy policy) calls <see cref="Remove"/>. Mortals that see
    /// five piles raise the alarm (<see cref="RegionAlarms"/>).
    /// </summary>
    [RegionSystem]
    public sealed class RegionBones : MonoBehaviour, IRegionSetup
    {
        public const string PrefabResource = "Bone Pile";
        private const float AgeInterval = 0.5f;

        private readonly List<BonePile> _piles = new();
        private Random _random = new(0);
        private GameObject _prefab;
        private float _nextAge;

        public static RegionBones Current { get; private set; }

        /// <summary>Every pile lying now, oldest first.</summary>
        public IReadOnlyList<BonePile> Piles => _piles;

        public int Count => _piles.Count;

        /// <summary>A pile was left.</summary>
        public event Action<BonePile> Added;

        /// <summary>A pile is gone (taken, removed or crumbled; see <see cref="BonePile.Fate"/>).</summary>
        public event Action<BonePile> Gone;

        int IRegionSetup.SetupOrder => RegionOrder.Settings;

        private void OnEnable()
        {
            Current = this;
            RegionEntities.Died += OnDied;
        }

        private void OnDisable()
        {
            if (Current == this) Current = null;
            RegionEntities.Died -= OnDied;
            _piles.Clear();
        }

        void IRegionSetup.Setup(RegionContext region)
        {
            _random = region.Random("bones");
            _prefab = Resources.Load<GameObject>(PrefabResource);
            if (_prefab != null) Pools.Prewarm(_prefab, 64);
        }

        /// <summary>Leaves a pile of <paramref name="kind"/> at <paramref name="at"/>. Null when there is no pile prefab or no Region ground.</summary>
        public BonePile Drop(Vector3 at, BoneKind kind)
        {
            RegionGrid grid = RegionGrid.Current;
            if (_prefab == null || grid == null) return null;
            if (_piles.Count >= BoneRules.MaxPiles) Retire(_piles[0], BoneFate.Crumbled);
            GameObject go = Pools.Spawn(_prefab, at, Quaternion.identity);
            if (!go.TryGetComponent(out BonePile pile)) { Pools.Despawn(go); return null; }
            pile.Place(kind, _random.Next(), grid.CellAt(at), Time.time);
            _piles.Add(pile);
            Added?.Invoke(pile);
            return pile;
        }

        /// <summary>Uses a pile: true once, and the pile is gone. False when it is already gone.</summary>
        public bool Take(BonePile pile) => pile != null && pile.IsAvailable && Retire(pile, BoneFate.Taken);

        /// <summary>Destroys a pile without using it: true once. False when it is already gone.</summary>
        public bool Remove(BonePile pile) => pile != null && pile.IsAvailable && Retire(pile, BoneFate.Removed);

        private void OnDied(UnitDeath death)
        {
            if (!BoneRules.LeavesBones(death.Kind)) return;
            GameObject body = death.Unit.GameObject;
            Vector3 at = body != null ? body.transform.position : RegionGrid.Current.CentreOf(death.Cell);
            Drop(at, BoneRules.KindOf(death.Kind));
        }

        private void Update()
        {
            if (Time.time < _nextAge) return;
            _nextAge = Time.time + AgeInterval;
            // Piles are in the order they were left, so the oldest crumble first.
            while (_piles.Count > 0 && BoneRules.Expired(Time.time - _piles[0].BornAt)) Retire(_piles[0], BoneFate.Crumbled);
            foreach (BonePile pile in _piles) pile.Age(Time.time);
        }

        private bool Retire(BonePile pile, BoneFate fate)
        {
            if (!_piles.Remove(pile)) return false;   // a few hundred at most, and piles leave rarely
            pile.Fate = fate;
            Gone?.Invoke(pile);
            Pools.Despawn(pile.gameObject);
            return true;
        }
    }
}
