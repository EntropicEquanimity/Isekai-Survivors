using System.Collections.Generic;
using Blondie.Pooling;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// What makes a bone demon a bone demon: when its Portal's bones policy is Harvest it walks to a pile near its post,
    /// spends it (<see cref="RegionBones.Take"/>, so each pile is used once) and raises a skeleton there, a few at a time.
    /// The skeletons fight on your side under the same Portal's order and crumble after <see cref="SkeletonSeconds"/>.
    /// Asked by its <see cref="DefenderBrain"/> when it chooses Work.
    /// </summary>
    public sealed class BoneRaiser : MonoBehaviour
    {
        [SerializeField] private GameObject _skeletonPrefab;

        private readonly List<Skeleton> _raised = new();
        private float _readyAt;

        /// <summary>How long a skeleton this demon raises lasts (<see cref="DemonTuning.SkeletonSeconds"/>; tests shorten it).</summary>
        public float SkeletonSeconds { get; set; } = DemonTuning.SkeletonSeconds;

        /// <summary>The skeletons it raised that still stand.</summary>
        public int Alive
        {
            get
            {
                _raised.RemoveAll(s => s == null || !s.isActiveAndEnabled);
                return _raised.Count;
            }
        }

        /// <summary>How many skeletons it has raised so far.</summary>
        public int Raised { get; private set; }

        private void OnEnable()
        {
            _raised.Clear();
            _readyAt = 0f;
        }

        /// <summary>
        /// One think of work. True with <paramref name="goal"/> set when it wants to walk to a pile; false when it has nothing
        /// to do (the policy is not Harvest, it has all the skeletons it may keep, or no pile is near) or just spent a pile.
        /// </summary>
        public bool Work(UnitProducer portal, Vector3 post, float leash, out Vector3 goal)
        {
            goal = default;
            RegionBones bones = RegionBones.Current;
            if (portal == null || portal.Policy != BonesPolicy.Harvest || bones == null || _skeletonPrefab == null) return false;
            if (Time.time < _readyAt || Alive >= DemonTuning.SkeletonsPerDemon) return false;
            BonePile pile = NearestPile(bones, post, leash);
            if (pile == null) return false;
            Vector3 at = pile.transform.position;
            if (((Vector2)at - (Vector2)transform.position).sqrMagnitude > DemonTuning.BoneReach * DemonTuning.BoneReach)
            {
                goal = at;
                return true;
            }
            Raise(bones, pile, portal);
            return false;
        }

        private BonePile NearestPile(RegionBones bones, Vector3 post, float leash)
        {
            BonePile best = null;
            float bestDistance = float.MaxValue;
            float reach = leash + DemonTuning.BoneSearchRadius;
            Vector2 self = transform.position;
            IReadOnlyList<BonePile> piles = bones.Piles;
            for (int i = 0; i < piles.Count; i++)
            {
                BonePile pile = piles[i];
                if (!pile.IsAvailable) continue;
                Vector2 at = pile.transform.position;
                if ((at - (Vector2)post).sqrMagnitude > reach * reach) continue;
                float d = (at - self).sqrMagnitude;
                if (d < bestDistance) { bestDistance = d; best = pile; }
            }
            return best;
        }

        private void Raise(RegionBones bones, BonePile pile, UnitProducer portal)
        {
            Vector3 at = pile.transform.position;
            if (!bones.Take(pile)) return;
            GameObject go = Pools.Spawn(_skeletonPrefab, at, Quaternion.identity);
            if (go.TryGetComponent(out Skeleton skeleton))
            {
                skeleton.Raise(SkeletonSeconds);
                _raised.Add(skeleton);
            }
            if (go.TryGetComponent(out DefenderBrain brain)) portal.Adopt(brain);
            Raised++;
            _readyAt = Time.time + DemonTuning.RaiseCooldown;
        }
    }
}
