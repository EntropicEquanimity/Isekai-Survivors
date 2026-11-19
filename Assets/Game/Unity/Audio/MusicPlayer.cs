using Blondie.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Isekai
{
    /// <summary>
    /// The game's music, across scenes: picks the <see cref="MusicState"/> from the active scene, the Run and the Region
    /// (Vigilance and Waves) and crossfades to its track on Blondie Core's <see cref="GameAudio"/> music bus, so the
    /// player's Music and Master volumes (Blondie Settings) apply.
    /// </summary>
    public sealed class MusicPlayer : MonoBehaviour
    {
        public const float FadeSeconds = 1.5f;

        private MusicLibrary _library;

        public static MusicPlayer Current { get; private set; }

        public MusicState State { get; private set; }
        public AudioClip Clip { get; private set; }
        public MusicLibrary Library => _library;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (Current != null) return;
            var go = new GameObject("Music Player");
            DontDestroyOnLoad(go);
            go.AddComponent<MusicPlayer>();
        }

        private void Awake()
        {
            if (Current != null && Current != this) { Destroy(gameObject); return; }
            Current = this;
            _library = Resources.Load<MusicLibrary>("MusicLibrary");
            if (_library == null) BLog.Warning("Audio", "No MusicLibrary in Resources: there will be no music.");
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        private void Update()
        {
            if (_library == null) return;
            MusicState wanted = MusicStates.Pick(SceneManager.GetActiveScene().name, RunSession.Run,
                VigilanceDirector.Current, RegionEvents.HasEnded, RegionEvents.Outcome);
            if (wanted == State) return;
            State = wanted;
            Clip = _library.For(wanted);
            if (Clip != null) GameAudio.PlayMusic(Clip, FadeSeconds);
            else GameAudio.StopMusic(FadeSeconds);
        }
    }
}
