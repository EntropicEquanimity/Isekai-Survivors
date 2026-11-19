using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Isekai.Dev.Editor
{
    /// <summary>
    /// Dev-only performance run: <c>exec Isekai.Dev.Editor.PerfProbe.Run 30</c> plays the Region for that many seconds
    /// with the Character walking a wide loop (so it crosses cells and wakes roamers, as in real play), brings the
    /// first Wave in a third of the way through, then stops. <see cref="FrameStats"/> logs fps windows and spike
    /// breakdowns; this probe also records the Profiler and, for each slow frame, logs which scripts' updates took the
    /// time ("[Perf] who ..."), so costs inside packages show up by name.
    /// </summary>
    [InitializeOnLoad]
    public static class PerfProbe
    {
        private const string PendingKey = "Isekai.Dev.PerfProbe.Seconds";
        private const float SlowFrameMs = 20f;
        private const int MaxWhoLines = 25;
        private static readonly string[] s_phases = { "BehaviourUpdate", "FixedBehaviourUpdate", "LateBehaviourUpdate" };

        static PerfProbe() => EditorApplication.playModeStateChanged += OnPlayModeChanged;

        public static string Run(string seconds)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return "already in Play Mode";
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) return "an open scene has unsaved changes; save it first";
            float duration = float.TryParse(seconds, out float s) ? s : 30f;
            EditorSceneManager.OpenScene("Assets/Game/Scenes/Region.unity", OpenSceneMode.Single);
            SessionState.SetFloat(PendingKey, duration);
            EditorApplication.EnterPlaymode();
            return $"perf run for {duration:0} s";
        }

        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredPlayMode) return;
            float duration = SessionState.GetFloat(PendingKey, 0f);
            if (duration <= 0f) return;
            SessionState.EraseFloat(PendingKey);
            Application.runInBackground = true;
            ProfilerDriver.ClearAllFrames();
            ProfilerDriver.profileEditor = false;
            ProfilerDriver.enabled = true;
            double start = EditorApplication.timeSinceStartup, nextScan = start + 0.5;
            int scanned = ProfilerDriver.lastFrameIndex, whoLines = 0;
            bool waveSent = false;
            Vector3 centre = Vector3.zero;
            bool centred = false;
            Debug.Log($"[Perf] probe start: {duration:0} s, walking a loop");
            void Tick()
            {
                float t = (float)(EditorApplication.timeSinceStartup - start);
                CharacterControl body = Player.Character;
                if (body != null)
                {
                    if (!centred) { centre = body.transform.position; centred = true; }
                    float angle = t * 0.25f;   // about 4 units/s along a 16-unit-radius loop
                    body.transform.position = centre + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * 16f;
                }
                if (!waveSent && t > duration / 3f)
                {
                    waveSent = true;
                    var waves = Object.FindAnyObjectByType<WaveDirector>();
                    if (waves != null) waves.Skip(waves.SecondsUntilNext + 0.01f);
                    Debug.Log("[Perf] probe: first Wave sent");
                }
                if (EditorApplication.timeSinceStartup >= nextScan)
                {
                    nextScan = EditorApplication.timeSinceStartup + 0.5;
                    int last = ProfilerDriver.lastFrameIndex;
                    for (int f = Mathf.Max(scanned + 1, ProfilerDriver.firstFrameIndex); f <= last && whoLines < MaxWhoLines; f++)
                        if (Describe(f, out string line)) { Debug.Log(line); whoLines++; }
                    scanned = last;
                }
                if (t < duration) return;
                EditorApplication.update -= Tick;
                ProfilerDriver.enabled = false;
                Debug.Log($"[Perf] probe end: horde {HordeDirector.Current?.Count ?? 0}, buildings {Building.All.Count}");
                EditorApplication.ExitPlaymode();
            }
            EditorApplication.update += Tick;
        }

        // A slow frame's heaviest script updates, by name, under each script phase.
        private static bool Describe(int frame, out string line)
        {
            line = null;
            using HierarchyFrameDataView view = ProfilerDriver.GetHierarchyFrameDataView(frame, 0,
                HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName, HierarchyFrameDataView.columnTotalTime, false);
            if (view == null || !view.valid || view.frameTimeMs < SlowFrameMs) return false;
            var costs = new List<(int id, string name, float ms)>();
            var children = new List<int>();
            foreach (int phase in FindAll(view, view.GetRootItemID(), s_phases))
            {
                children.Clear();
                view.GetItemChildren(phase, children);
                foreach (int child in children)
                    costs.Add((child, Short(view.GetItemName(child)), view.GetItemColumnDataAsFloat(child, HierarchyFrameDataView.columnTotalTime)));
            }
            // The heaviest script updates, each with what inside it took the time.
            var top = costs.Where(c => c.ms >= 1f).OrderByDescending(c => c.ms).Take(5).Select(c => $"{c.name} {c.ms:F1}{Inside(view, c.id)}");
            line = $"[Perf] who {view.frameTimeMs:F1} ms (frame {frame}): {string.Join(", ", top)}";
            return true;
        }

        private static string Inside(HierarchyFrameDataView view, int item)
        {
            var children = new List<int>();
            view.GetItemChildren(item, children);
            var parts = children
                .Select(c => (name: Short(view.GetItemName(c)), ms: view.GetItemColumnDataAsFloat(c, HierarchyFrameDataView.columnTotalTime)))
                .Where(c => c.ms >= 0.5f).OrderByDescending(c => c.ms).Take(3).Select(c => $"{c.name} {c.ms:F1}").ToArray();
            return parts.Length == 0 ? "" : " [" + string.Join(", ", parts) + "]";
        }

        // "Isekai.Game.dll!Isekai::HordeDirector.Update() [Invoke]" -> "HordeDirector.Update()"
        private static string Short(string name)
        {
            int bang = name.IndexOf('!');
            if (bang >= 0) name = name.Substring(bang + 1);
            int colons = name.LastIndexOf("::", System.StringComparison.Ordinal);
            if (colons >= 0) name = name.Substring(colons + 2);
            return name.Replace(" [Invoke]", "");
        }

        private static IEnumerable<int> FindAll(HierarchyFrameDataView view, int item, string[] names, int depth = 0)
        {
            if (depth > 6) yield break;
            var children = new List<int>();
            view.GetItemChildren(item, children);
            foreach (int child in children)
            {
                if (names.Contains(view.GetItemName(child))) { yield return child; continue; }
                foreach (int found in FindAll(view, child, names, depth + 1)) yield return found;
            }
        }
    }
}
