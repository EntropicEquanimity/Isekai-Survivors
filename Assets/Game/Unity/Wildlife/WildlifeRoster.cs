using System;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// The prefab of each <see cref="WildlifeType"/> (a Slime, a Werewolf, a Werebear). Loaded from Resources by
    /// <see cref="RegionWildlife"/>, so spawning wildlife needs no scene wiring.
    /// </summary>
    [CreateAssetMenu(menuName = "Isekai/Wildlife Roster")]
    public sealed class WildlifeRoster : ScriptableObject
    {
        public const string ResourceName = "WildlifeRoster";

        [Serializable]
        public struct Entry
        {
            public WildlifeType Type;
            public GameObject Prefab;
        }

        [SerializeField] private Entry[] _entries = Array.Empty<Entry>();

        /// <summary>The prefab of a type, or null when none is set.</summary>
        public GameObject PrefabOf(WildlifeType type)
        {
            foreach (Entry e in _entries)
                if (e.Type == type) return e.Prefab;
            return null;
        }
    }
}
