using System;
using System.Collections.Generic;
using UnityEngine;

namespace Isekai
{
    /// <summary>A minable deposit (a rock, a tree) holding a set amount of one Resource. Hides itself when empty.</summary>
    public sealed class ResourceNode : MonoBehaviour
    {
        private static readonly List<ResourceNode> s_all = new();
        private object _reservedBy;

        [SerializeField] private ResourceType _type = ResourceType.Stone;
        [SerializeField, Min(1)] private int _amount = 50;

        public static IReadOnlyList<ResourceNode> All => s_all;

        /// <summary>A node was mined out, just before it switches itself off (its rock stops cutting sight, its ground opens).</summary>
        public static event Action<ResourceNode> Depleted;

        public ResourceType Type => _type;
        public int Remaining { get; private set; }
        public bool IsEmpty => Remaining <= 0;

        private void Awake() => Remaining = _amount;

        private void OnEnable() => s_all.Add(this);

        private void OnDisable() => s_all.Remove(this);

        /// <summary>Takes up to <paramref name="units"/> and returns how many it had.</summary>
        public int Take(int units)
        {
            int taken = Mathf.Clamp(units, 0, Remaining);
            Remaining -= taken;
            if (IsEmpty)
            {
                _reservedBy = null;
                Depleted?.Invoke(this);
                gameObject.SetActive(false);
            }
            return taken;
        }

        /// <summary>Marks this node as claimed by <paramref name="owner"/> while it is being mined.</summary>
        public bool TryReserve(object owner)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (IsEmpty || !isActiveAndEnabled) return false;
            if (_reservedBy == null || ReferenceEquals(_reservedBy, owner))
            {
                _reservedBy = owner;
                return true;
            }
            return false;
        }

        /// <summary>Releases this node if it was reserved by <paramref name="owner"/>.</summary>
        public void Release(object owner)
        {
            if (owner != null && ReferenceEquals(_reservedBy, owner)) _reservedBy = null;
        }

        /// <summary>Whether another miner has this node reserved.</summary>
        public bool IsReservedByOther(object owner) => _reservedBy != null && !ReferenceEquals(_reservedBy, owner);

        /// <summary>The nearest non-empty node within <paramref name="reach"/> of <paramref name="position"/>, or null.</summary>
        public static ResourceNode NearestTo(Vector2 position, float reach)
        {
            ResourceNode best = null;
            float bestDistance = reach;
            foreach (ResourceNode node in s_all)
            {
                if (node.IsEmpty) continue;
                float d = Vector2.Distance(position, node.transform.position);
                if (d <= bestDistance) { bestDistance = d; best = node; }
            }
            return best;
        }
    }
}
