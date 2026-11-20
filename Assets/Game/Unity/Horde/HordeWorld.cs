using System.Collections.Generic;
using Blondie.Abilities;
using Blondie.Combat;
using Blondie.Core;

namespace Isekai
{
    /// <summary>
    /// How abilities find units in a Region: horde enemies come from the director's spatial hash (they have no
    /// colliders), everything else (the Character, Buildings) from physics. Line of sight and presence use physics.
    /// </summary>
    public sealed class HordeWorld : IAbilityWorld
    {
        private readonly SpatialHash<HordeMember> _hash;
        private readonly float _memberRadius;
        private readonly List<HordeMember> _found = new();

        public HordeWorld(SpatialHash<HordeMember> hash, float memberRadius)
        {
            _hash = hash;
            _memberRadius = memberRadius;
        }

        public void FindUnits(Float3 center, float radius, bool is2D, List<CombatUnit> results)
        {
            if (radius <= 0f) return;
            _found.Clear();
            _hash.Query(center.x, is2D ? center.y : center.z, radius + _memberRadius, _found);
            foreach (HordeMember member in _found)
                if (member != null && member.IsAlive) results.Add(member.Unit);
            PhysicsAbilityWorld.Instance.FindUnits(center, radius, is2D, results);
        }

        public bool HasLineOfSight(Float3 from, Float3 to, bool is2D) => PhysicsAbilityWorld.Instance.HasLineOfSight(from, to, is2D);

        public bool IsPresent(CombatUnit unit) => PhysicsAbilityWorld.Instance.IsPresent(unit);
    }
}
