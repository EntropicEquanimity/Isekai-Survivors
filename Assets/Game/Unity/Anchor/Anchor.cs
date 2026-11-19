using System.Collections.Generic;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// Marks a Main Base prefab as one of the Anchors (<see cref="AnchorKind"/>). A Shrine starts hidden: its
    /// <see cref="HordeTarget"/> is off, so it is no target and the mortals know nothing of it, until <see cref="Reveal"/>
    /// (a patrol saw it, see <see cref="RegionAnchor"/>) switches it on and it attracts the horde from then on.
    /// </summary>
    [RequireComponent(typeof(Building))]
    public sealed class Anchor : MonoBehaviour
    {
        private static readonly List<Anchor> s_all = new();

        [SerializeField] private AnchorKind _kind;

        private Building _building;
        private HordeTarget _target;

        public static IReadOnlyList<Anchor> All => s_all;

        public AnchorKind Kind => _kind;
        public Building Building => _building;

        /// <summary>The mortals know nothing of it yet (a Shrine not yet seen).</summary>
        public bool IsHidden { get; private set; }

        private void Awake()
        {
            _building = GetComponent<Building>();
            _target = GetComponent<HordeTarget>();
            if (!AnchorTuning.StartsHidden(_kind) || _target == null) return;
            IsHidden = true;
            _target.enabled = false;
        }

        private void OnEnable() => s_all.Add(this);

        private void OnDisable() => s_all.Remove(this);

        /// <summary>Found: the Anchor becomes a horde target (and a unit the mortals can see). False if it was not hidden.</summary>
        public bool Reveal()
        {
            if (!IsHidden) return false;
            IsHidden = false;
            if (_target != null) _target.enabled = true;
            return true;
        }
    }
}
