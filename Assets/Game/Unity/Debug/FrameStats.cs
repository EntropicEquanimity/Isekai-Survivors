using System;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// Logs frame rate every few seconds as "[Perf] ..." lines (average fps, slowest frame, horde size), for profiling
    /// builds from their Player.log. Every slow frame (over <see cref="_spikeMs"/> or three times the running average)
    /// also logs a "[Perf] spike" line saying where its time went (<see cref="SpikeReport"/>). A <c>-quit-after N</c>
    /// command-line argument quits after N seconds with a summary.
    /// </summary>
    public sealed class FrameStats : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float _reportSeconds = 5f;
        [Tooltip("Seconds ignored at start while the horde spawns and shaders warm up.")]
        [SerializeField, Min(0f)] private float _warmupSeconds = 5f;
        [Tooltip("Frames slower than this (milliseconds) are reported with a breakdown.")]
        [SerializeField, Min(1f)] private float _spikeMs = 12f;
        [Tooltip("At most this many spike lines per report window, so a bad stretch doesn't flood the log.")]
        [SerializeField, Min(0)] private int _spikesPerWindow = 3;

        private float _quitAfter = -1f;
        private float _windowStart, _windowWorst;
        private int _windowFrames;
        private double _totalTime;
        private int _totalFrames;
        private float _totalWorst;
        private SpikeReport _spikes;
        private int _windowSpikes;
        private float _average = 1f / 60f;

        private void OnEnable() => _spikes = new SpikeReport();

        private void OnDisable()
        {
            _spikes?.Dispose();
            _spikes = null;
        }

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
            ReportSpike(dt);
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
                    Log($"[Perf] {_windowFrames / (now - _windowStart):F1} fps, slowest {_windowWorst * 1000f:F1} ms, horde {HordeDirector.Current?.Count ?? 0}, kills {HordeDirector.Current?.Kills ?? 0}, character {CharacterState()}");
                _windowStart = now;
                _windowFrames = 0;
                _windowWorst = 0f;
                _windowSpikes = 0;
            }
            if (_quitAfter > 0f && Time.timeSinceLevelLoad >= _quitAfter)
            {
                Log($"[Perf] summary: {(_totalFrames / Math.Max(_totalTime, 1e-6)):F1} fps average over {_totalFrames} frames, slowest {_totalWorst * 1000f:F1} ms, horde {HordeDirector.Current?.Count ?? 0}, kills {HordeDirector.Current?.Kills ?? 0}, character {CharacterState()}");
                _quitAfter = -1f;
                Application.Quit();
            }
        }

        // The frame that just ended (dt) against the threshold; the recorders hold that same frame's numbers.
        private void ReportSpike(float dt)
        {
            bool slow = Time.timeSinceLevelLoad > 2f && (dt * 1000f > _spikeMs || dt > _average * 3f) && dt * 1000f > 8f;
            _average = Mathf.Lerp(_average, dt, 0.05f);
            if (!slow || _spikes == null || _windowSpikes >= _spikesPerWindow) return;
            _windowSpikes++;
            Log($"[Perf] spike {dt * 1000f:F1} ms (avg {_average * 1000f:F1}): {_spikes.Describe()}");
        }

        // No stack trace: in the Editor capturing one costs several milliseconds, which made the report cause spikes.
        private static void Log(string line) => Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "{0}", line);

        private static string CharacterState()
        {
            CharacterControl control = Player.Character;
            var damageable = control != null ? control.GetComponent<Blondie.Combat.Damageable>() : null;
            return damageable == null ? "none" : damageable.Unit.IsDead ? "dead" : $"{damageable.Unit.Health:F0} hp";
        }
    }
}
