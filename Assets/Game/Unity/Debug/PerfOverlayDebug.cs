using Blondie.FlowFields;
using UnityEngine;

namespace Isekai
{
    /// <summary>Small on-screen performance overlay with frame and unit stats.</summary>
    [RegionSystem]
    public sealed class PerfOverlayDebug : MonoBehaviour
    {
        private bool _show;
        private GUIStyle _title;
        private float _windowAt;
        private int _windowFrames;
        private float _windowMs;
        private float _windowMaxMs;
        private float _avgMs;
        private float _maxMs;

        private void OnEnable()
        {
            DebugActions.Toggle(this, "Perf", "Perf overlay", () => _show, on => _show = on);
            DebugActions.Info(this, "Perf", () => $"Frame {_avgMs:0.00}/{_maxMs:0.00} ms (avg/max, 1s)");
        }

        private void OnDisable() => DebugActions.Remove(this);

        private void Update()
        {
            float ms = Time.unscaledDeltaTime * 1000f;
            _windowFrames++;
            _windowMs += ms;
            if (ms > _windowMaxMs) _windowMaxMs = ms;
            if (Time.unscaledTime - _windowAt < 1f) return;
            _windowAt = Time.unscaledTime;
            _avgMs = _windowFrames > 0 ? _windowMs / _windowFrames : 0f;
            _maxMs = _windowMaxMs;
            _windowFrames = 0;
            _windowMs = 0f;
            _windowMaxMs = 0f;
        }

        private void OnGUI()
        {
            if (!_show) return;
            _title ??= new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            GUILayout.BeginArea(new Rect(Screen.width - 320f, 320f, 310f, 150f), GUI.skin.box);
            GUILayout.Label("Perf", _title);
            GUILayout.Label($"Frame {_avgMs:0.00}/{_maxMs:0.00} ms (avg/max, 1s)");
            GUILayout.Label($"Units: Yours {RegionEntities.Count(Side.Yours)} · Mortals {RegionEntities.Count(Side.Mortals)} · Neutral {RegionEntities.Count(Side.Neutral)} · Hostile {RegionEntities.Count(Side.Hostile)} · Rogue {RegionEntities.Count(Side.Rogue)}");
            GUILayout.Label($"Horde members {HordeDirector.Current?.Count ?? 0} ({HordeDirector.Current?.Attacking ?? 0} attacking)");
            GUILayout.Label($"Flow fields cached {FlowFields.CachedCount} · recycled {FlowFields.RecycledCount} · mortal fields held {RegionMortals.Current?.HeldFields ?? 0}");
            GUILayout.EndArea();
        }
    }
}
