using System.Collections.Generic;
using UnityEngine;

namespace Isekai
{
    /// <summary>A Base: the ground within its radius can be built on, once the Base itself is built.</summary>
    [RequireComponent(typeof(Building))]
    public sealed class BaseArea : MonoBehaviour
    {
        private static readonly List<BaseArea> s_all = new();

        [SerializeField, Min(0f)] private float _buildRadius = 8f;

        private Building _building;

        public static IReadOnlyList<BaseArea> All => s_all;
        public float BuildRadius => _buildRadius;
        public bool IsActive => _building != null && _building.IsBuilt && _building.IsAlive;

        /// <summary>True if <paramref name="point"/> is inside any working Base's area.</summary>
        public static bool Covers(Vector2 point)
        {
            foreach (BaseArea area in s_all)
                if (area.IsActive && Vector2.Distance(area.transform.position, point) <= area._buildRadius) return true;
            return false;
        }

        private void Awake() => _building = GetComponent<Building>();

        private void OnEnable() => s_all.Add(this);

        private void OnDisable() => s_all.Remove(this);
    }
}
