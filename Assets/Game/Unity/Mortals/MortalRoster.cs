using System;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// The prefab of each <see cref="MortalType"/> (a Soldier, a Priest, ...) and the arrow the archers loose. Loaded
    /// from Resources by <see cref="RegionMortals"/>, so spawning mortals needs no scene wiring.
    /// </summary>
    [CreateAssetMenu(menuName = "Isekai/Mortal Roster")]
    public sealed class MortalRoster : ScriptableObject
    {
        public const string ResourceName = "MortalRoster";

        [Serializable]
        public struct Entry
        {
            public MortalType Type;
            public GameObject Prefab;
        }

        [SerializeField] private Entry[] _entries = Array.Empty<Entry>();
        [Tooltip("The arrow an archer's shot draws (art only: the hit itself is the horde's reach attack).")]
        [SerializeField] private GameObject _arrow;

        public GameObject Arrow => _arrow;

        /// <summary>The prefab of a type, or null when none is set.</summary>
        public GameObject PrefabOf(MortalType type)
        {
            foreach (Entry e in _entries)
                if (e.Type == type) return e.Prefab;
            return null;
        }
    }
}
