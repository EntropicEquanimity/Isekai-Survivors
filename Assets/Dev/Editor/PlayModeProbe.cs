using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Isekai.Dev.Editor
{
    /// <summary>
    /// Dev-only: plays a scene for a moment so agents can see it. <c>exec Isekai.Dev.Editor.PlayModeProbe.Capture
    /// Assets/Game/Scenes/Region.unity</c> opens the scene, enters Play Mode, waits two seconds, saves the Game view to
    /// <c>Logs/Dev/probe.png</c>, then stops. <c>scene|Namespace.Type.Method</c>
    /// also calls that method half a second in (on the scene's instance of a MonoBehaviour type, else a static one), e.g.
    /// <c>Assets/Game/Scenes/Planet.unity|Isekai.PlanetScreen.OpenLegacy</c> to see a window that opens on a click.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayModeProbe
    {
        private const string PendingKey = "Isekai.Dev.PlayModeProbe.Pending";
        private const string CallKey = "Isekai.Dev.PlayModeProbe.Call";

        static PlayModeProbe() => EditorApplication.playModeStateChanged += OnPlayModeChanged;

        public static string Capture(string scenePath)
        {
            string call = null;
            int bar = scenePath.IndexOf('|');
            if (bar >= 0)
            {
                call = scenePath.Substring(bar + 1).Trim();
                scenePath = scenePath.Substring(0, bar).Trim();
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode) return "already in Play Mode";
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) return "an open scene has unsaved changes; save it first";
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            SessionState.SetBool(PendingKey, true);
            SessionState.SetString(CallKey, call ?? "");
            EditorApplication.EnterPlaymode();
            return "entering Play Mode";
        }

        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(PendingKey, false)) return;
            SessionState.EraseBool(PendingKey);
            Application.runInBackground = true; // otherwise Play Mode pauses while the Editor is unfocused
            string call = SessionState.GetString(CallKey, "");
            SessionState.EraseString(CallKey);
            double callAt = EditorApplication.timeSinceStartup + 0.5;
            double captureAt = EditorApplication.timeSinceStartup + 2.0, stopAt = captureAt + 1.0;
            bool captured = false;
            void Tick()
            {
                double now = EditorApplication.timeSinceStartup;
                if (call.Length > 0 && now >= callAt)
                {
                    Call(call);
                    call = "";
                }
                if (!captured && now >= captureAt)
                {
                    captured = true;
                    ScreenCapture.CaptureScreenshot(Path.GetFullPath("Logs/Dev/probe.png"));
                }
                if (now < stopAt) return;
                EditorApplication.update -= Tick;
                EditorApplication.ExitPlaymode();
            }
            EditorApplication.update += Tick;
        }

        // "Namespace.Type.Method" (lowercase is fine: the bridge lowercases requests), case-insensitive.
        private static void Call(string target)
        {
            int dot = target.LastIndexOf('.');
            string typeName = target.Substring(0, dot), methodName = target.Substring(dot + 1);
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.IgnoreCase;
            foreach (System.Type type in TypeCache.GetTypesDerivedFrom<object>())
            {
                if (!string.Equals(type.FullName, typeName, System.StringComparison.OrdinalIgnoreCase)) continue;
                MethodInfo method = type.GetMethod(methodName, flags, null, System.Type.EmptyTypes, null);
                if (method == null) break;
                object instance = method.IsStatic ? null : Object.FindAnyObjectByType(type);
                if (!method.IsStatic && instance == null) break;
                method.Invoke(instance, null);
                return;
            }
            Debug.LogWarning($"PlayModeProbe: couldn't call {target}");
        }
    }
}
