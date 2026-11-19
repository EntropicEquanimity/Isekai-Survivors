using System.Collections.Generic;
using Blondie.Abilities;
using Blondie.Combat;
using Blondie.Core;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// Finding who to fight. Hostility is Blondie Combat's <see cref="Teams"/> (team relations, so neutral and allied
    /// factions work), and the search goes through the ability world (the horde's spatial hash plus physics), so
    /// horde members and physics units are found alike. Every AI brain uses this rather than its own search.
    /// </summary>
    public static class Targeting
    {
        /// <summary>
        /// The living unit hostile to <paramref name="self"/> within <paramref name="radius"/> of <paramref name="around"/>
        /// that is closest to <paramref name="from"/>; null when there is none. <paramref name="buffer"/> is the
        /// caller's reusable list.
        /// </summary>
        public static CombatUnit NearestHostile(CombatUnit self, Vector3 around, float radius, Vector3 from, List<CombatUnit> buffer, out float distance)
        {
            distance = float.MaxValue;
            if (self == null || AbilityWorld.Current == null) return null;
            buffer.Clear();
            AbilityWorld.Current.FindUnits(new Float3(around.x, around.y, 0f), radius, true, buffer);
            CombatUnit best = null;
            foreach (CombatUnit unit in buffer)
            {
                if (unit.IsDead || !Teams.AreHostile(self, unit)) continue;
                float d = Vector2.Distance(from, new Vector2(unit.Position.x, unit.Position.y));
                if (d < distance) { distance = d; best = unit; }
            }
            return best;
        }
    }
}
