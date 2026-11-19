using UnityEngine;

namespace Isekai
{
    /// <summary>The game-speed buttons and the frame rate readout; they work on any screen, so they register at startup, not with a Region.</summary>
    public static class SpeedDebug
    {
        private static readonly float[] Speeds = { 0.25f, 1f, 2f, 4f, 8f };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Register()
        {
            if (!Application.isEditor && !Debug.isDebugBuild) return;
            DebugActions.Remove(typeof(SpeedDebug));
            DebugActions.Info(typeof(SpeedDebug), "Speed", () => $"Game speed ×{Time.timeScale:0.##}");
            foreach (float s in Speeds)
            {
                float speed = s;
                DebugActions.Button(typeof(SpeedDebug), "Speed", $"×{speed:0.##}", () => Time.timeScale = speed, "speed");
            }
            DebugActions.Info(typeof(SpeedDebug), "Info", () => $"{1f / Mathf.Max(Time.smoothDeltaTime, 1e-4f):0} fps");
        }
    }
}
