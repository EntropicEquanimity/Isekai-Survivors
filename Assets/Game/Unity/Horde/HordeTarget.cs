using System.Collections.Generic;
using Blondie.Combat;
using Blondie.Core;
using Blondie.FlowFields;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// Something the horde walks to and attacks: the Character, Bases and other Buildings. The horde's flow field leads
    /// every enemy to the nearest living target; enemies that reach one hit it.
    /// </summary>
    [RequireComponent(typeof(Damageable))]
    public sealed class HordeTarget : MonoBehaviour
    {
        private static readonly List<HordeTarget> s_all = new();

        [Tooltip("The target's body radius: enemies stop and attack at this distance plus their reach.")]
        [SerializeField, Min(0.05f)] private float _radius = 0.3f;

        private Damageable _damageable;

        public static IReadOnlyList<HordeTarget> All => s_all;

        public float Radius => _radius;
        public Damageable Damageable => _damageable != null ? _damageable : _damageable = GetComponent<Damageable>();
        public bool IsAlive => isActiveAndEnabled && !Damageable.Unit.IsDead;

        /// <summary>Adds the flow-field cells this target covers.</summary>
        public void AddCells(FlowFieldGrid grid, List<Int2> cells)
        {
            Vector3 p = transform.position;
            float r = Mathf.Max(_radius - grid.CellSize * 0.5f, 0f);
            Int2 min = grid.WorldToCell(new Float3(p.x - r, p.y - r, 0f));
            Int2 max = grid.WorldToCell(new Float3(p.x + r, p.y + r, 0f));
            for (int x = min.x; x <= max.x; x++)
            for (int y = min.y; y <= max.y; y++)
                cells.Add(new Int2(x, y));
        }

        private void OnEnable() => s_all.Add(this);

        private void OnDisable() => s_all.Remove(this);
    }
}
