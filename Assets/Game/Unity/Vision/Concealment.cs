using System.Collections.Generic;
using Blondie.Core;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// The one hook the Traits need in Vision: units that are hidden from the other side even in plain sight (a Sneaky
    /// unit until it attacks), and units that see further or less far than their kind (Vigilant). The Trait system sets
    /// them; fog of war (<see cref="FogUnits"/> hides a concealed unit from your side), what the mortals notice
    /// (<see cref="MortalSenses"/>) and the viewers' ranges (<see cref="RegionVision"/>) read them. A set lookup, so it costs
    /// nothing per unit that has no Trait.
    /// </summary>
    public static class Concealment
    {
        private static readonly HashSet<GameObject> s_hidden = new();
        private static readonly Dictionary<GameObject, float> s_sight = new();

        static Concealment() => StaticReset.Register(Reset);

        /// <summary>How many units are concealed now.</summary>
        public static int Count => s_hidden.Count;

        /// <summary>The unit is hidden from the other side whatever the fog says.</summary>
        public static bool IsHidden(GameObject unit) => s_hidden.Count > 0 && unit != null && s_hidden.Contains(unit);

        public static bool IsHidden(RegionEntity unit) => s_hidden.Count > 0 && unit != null && s_hidden.Contains(unit.GameObject);

        public static void SetHidden(GameObject unit, bool hidden)
        {
            if (unit == null) return;
            if (hidden) s_hidden.Add(unit);
            else s_hidden.Remove(unit);
        }

        /// <summary>How much further than its kind's range the unit sees (1 unchanged, 1.5 for a Vigilant one).</summary>
        public static float SightFactor(GameObject unit) => s_sight.Count > 0 && unit != null && s_sight.TryGetValue(unit, out float f) ? f : 1f;

        public static void SetSightFactor(GameObject unit, float factor)
        {
            if (unit == null) return;
            if (Mathf.Approximately(factor, 1f)) s_sight.Remove(unit);
            else s_sight[unit] = factor;
        }

        /// <summary>A new Region is starting.</summary>
        public static void Reset()
        {
            s_hidden.Clear();
            s_sight.Clear();
        }
    }
}
