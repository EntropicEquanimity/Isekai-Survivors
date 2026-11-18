using System;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// Logs frame rate every few seconds as "[Perf] ..." lines (average fps, slowest frame, horde size), for profiling
    /// builds from their Player.log. A <c>-quit-after N</c> command-line argument quits after N seconds with a summary.
    /// </summary>
    public sealed class FrameStats : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float _reportSeconds = 5f;
        [Tooltip("Seconds ignored at start while the horde spawns and shaders warm up.")]
        [SerializeField, Min(0f)] private float _warmupSeconds = 5f;

        private float _quitAfter = -1f;
        private float _windowStart, _windowWorst;
        private int _windowFrames;
        private double _totalTime;
        private int _totalFrames;
        private float _totalWorst;

        private void Start()
        {
            string[] args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-quit-after");
            if (i >= 0 && i + 1 < args.Length && float.TryParse(args[i + 1], out float s)) _quitAfter = s;
            if (Application.isEditor || _quitAfter > 0f) Application.runInBackground = true;   // keep measuring while unfocused
            if (_quitAfter > 0f)
            {
                QualitySettings.vSyncCount = 0;   // measure headroom, not the monitor's refresh rate
                Application.targetFrameRate = -1;
            }
            _windowStart = Time.realtimeSinceStartup;
        }

        private void Update()
        {
            float now = Time.realtimeSinceStartup;
            float dt = Time.unscaledDeltaTime;
            if (Time.timeSinceLevelLoad >= _warmupSeconds)
            {
                _windowFrames++;
                _windowWorst = Mathf.Max(_windowWorst, dt);
                _totalTime += dt;
                _totalFrames++;
                _totalWorst = Mathf.Max(_totalWorst, dt);
            }
            if (now - _windowStart >= _reportSeconds)
            {
                if (_windowFrames > 0)
                    Debug.Log($"[Perf] {_windowFrames / (now - _windowStart):F1} fps, slowest {_windowWorst * 1000f:F1} ms, horde {HordeDirector.Current?.Count ?? 0}, kills {HordeDirector.Current?.Kills ?? 0}, character {CharacterState()}");
                _windowStart = now;
                _windowFrames = 0;
                _windowWorst = 0f;
            }
            if (_quitAfter > 0f && Time.timeSinceLevelLoad >= _quitAfter)
            {
                Debug.Log($"[Perf] summary: {(_totalFrames / Math.Max(_totalTime, 1e-6)):F1} fps average over {_totalFrames} frames, slowest {_totalWorst * 1000f:F1} ms, horde {HordeDirector.Current?.Count ?? 0}, kills {HordeDirector.Current?.Kills ?? 0}, character {CharacterState()}");
                _quitAfter = -1f;
                Application.Quit();
            }
        }

        private static string CharacterState()
        {
            var control = FindAnyObjectByType<CharacterControl>();
            var damageable = control != null ? control.GetComponent<Blondie.Combat.Damageable>() : null;
            return damageable == null ? "none" : damageable.Unit.IsDead ? "dead" : $"{damageable.Unit.Health:F0} hp";
        }
    }
}
