using UnityEngine;

namespace Isekai
{
    /// <summary>The tracks the <see cref="MusicPlayer"/> plays, one per <see cref="MusicState"/> (capture and victory share one).</summary>
    [CreateAssetMenu(menuName = "Isekai/Music Library", fileName = "MusicLibrary")]
    public sealed class MusicLibrary : ScriptableObject
    {
        public AudioClip Menu;
        public AudioClip Planet;
        public AudioClip RegionCalm;
        public AudioClip RegionTense;
        public AudioClip Capture;
        public AudioClip Defeat;

        public AudioClip For(MusicState state) => state switch
        {
            MusicState.Menu => Menu,
            MusicState.Planet => Planet,
            MusicState.RegionCalm => RegionCalm,
            MusicState.RegionTense => RegionTense,
            MusicState.Captured or MusicState.Victory => Capture,
            MusicState.Defeat => Defeat,
            _ => null,
        };
    }
}
