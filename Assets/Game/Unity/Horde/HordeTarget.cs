using System.Collections.Generic;
using Blondie.Combat;
using Blondie.Core;
using Blondie.FlowFields;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// Something a horde walks to and attacks: the Character, Bases and other Buildings. A horde's flow field leads
    /// every member to the nearest living target hostile to the horde's team; members that reach one hit it.
    /// </summary>
    [RequireComponent(typeof(Damageable))]
    public sealed class HordeTarget : MonoBehaviour
    {
        private static readonly List<HordeTarget> s_all = new();

        [Tooltip("The target's body radius: enemies stop and attack at this distance plus their reach.")]
        [SerializeField, Min(0.05f)] private float _radius = 0.3f;
        [Tooltip("The horde's flow field leads to it. Off for Units: enemies still hit them in passing, and the field isn't rebuilt every time one moves.")]
        [SerializeField] private bool _attractsHorde = true;

        private Damageable _damageable;
        private RegionUnit _unit;

        public static IReadOnlyList<HordeTarget> All => s_all;

        public float Radius => _radius;
        public bool AttractsHorde => _attractsHorde;
        public Damageable Damageable => _damageable != null ? _damageable : _damageable = GetComponent<Damageable>();
        public bool IsAlive => isActiveAndEnabled && !Damageable.Unit.IsDead;

        /// <summary>The target's team (from its Blondie Combat Team); hordes go only for targets hostile to theirs.</summary>
        public string Team => Damageable.Unit.Team;

        /// <summary>
        /// Whether no living target hostile to <paramref name="team"/> is within <paramref name="distance"/> of
        /// <paramref name="point"/>: where that team's enemies may appear without landing on top of their foes.
        /// </summary>
        public static bool NoneHostileWithin(Vector3 point, float distance, string team = TeamIds.Enemy)
        {
            float d2 = distance * distance;
            foreach (HordeTarget t in s_all)
                if (t.IsAlive && Teams.Relation(team, t.Team) == TeamRelation.Hostile && (t.transform.position - point).sqrMagnitude < d2) return false;
            return true;
        }

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

        // A target is a Building, the Character or a Unit: what the Region registers it as.
        private void Awake()
        {
            UnitKind kind = TryGetComponent(out Building _) ? UnitKind.Building : TryGetComponent(out CharacterControl _) ? UnitKind.Jack : UnitKind.Demon;
            _unit = new RegionUnit(Damageable, kind);
        }

        private void OnEnable()
        {
            s_all.Add(this);
            _unit.Enter();
        }

        private void OnDisable()
        {
            s_all.Remove(this);
            _unit?.Leave();
        }
    }
}
