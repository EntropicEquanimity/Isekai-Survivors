using System;
using UnityEngine;

namespace Isekai
{
    /// <summary>The sounds of each <see cref="Cue"/> (a few per cue; one is picked at random each time).</summary>
    [CreateAssetMenu(menuName = "Isekai/Sfx Library", fileName = "SfxLibrary")]
    public sealed class SfxLibrary : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public Cue Cue;
            public AudioClip[] Clips;
        }

        public Entry[] Entries;

        /// <summary>The clips of <paramref name="cue"/> (empty when it has none).</summary>
        public AudioClip[] ClipsFor(Cue cue)
        {
            if (Entries != null)
                foreach (Entry entry in Entries)
                    if (entry.Cue == cue && entry.Clips != null) return entry.Clips;
            return Array.Empty<AudioClip>();
        }
    }
}
