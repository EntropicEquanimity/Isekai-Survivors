using UnityEngine;

namespace Isekai
{
    /// <summary>A debug row showing what the music is doing in the Region.</summary>
    [RegionSystem]
    public sealed class MusicDebug : MonoBehaviour
    {
        private void OnEnable() => DebugActions.Info(this, "Info", () =>
        {
            MusicPlayer music = MusicPlayer.Current;
            return music == null ? "Music: none" : $"Music: {music.State} ({(music.Clip != null ? music.Clip.name : "no track")})";
        });

        private void OnDisable() => DebugActions.Remove(this);
    }
}
