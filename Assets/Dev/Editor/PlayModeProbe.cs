using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Isekai.Dev.Editor
{
    /// <summary>
    /// Dev-only: plays a scene for a moment so agents can see it. <c>exec Isekai.Dev.Editor.PlayModeProbe.Capture
    /// Assets/Scenes/MainMenu.unity</c> opens the scene, enters Play Mode, waits two seconds, saves the Game view to
    /// <c>Logs/Dev/probe.png</c> and every screen-space UI image's size to <c>Logs/Dev/probe.txt</c>, then stops.
    /// Inactive menu panels named in <see cref="s_panelsToShow"/> are switched on first.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayModeProbe
    {
        private const string PendingKey = "Isekai.Dev.PlayModeProbe.Pending";
        private static readonly string[] s_panelsToShow = { "CharacterSelection" };

        static PlayModeProbe() => EditorApplication.playModeStateChanged += OnPlayModeChanged;

        public static string Capture(string scenePath)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return "already in Play Mode";
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) return "an open scene has unsaved changes; save it first";
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            SessionState.SetBool(PendingKey, true);
            EditorApplication.EnterPlaymode();
            return "entering Play Mode";
        }

        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(PendingKey, false)) return;
            SessionState.EraseBool(PendingKey);
            Application.runInBackground = true; // otherwise Play Mode pauses while the Editor is unfocused
            ShowPanels();
            double captureAt = EditorApplication.timeSinceStartup + 2.0, stopAt = captureAt + 1.0;
            bool captured = false;
            void Tick()
            {
                double now = EditorApplication.timeSinceStartup;
                if (!captured && now >= captureAt)
                {
                    captured = true;
                    ScreenCapture.CaptureScreenshot(Path.GetFullPath("Logs/Dev/probe.png"));
                    WriteReport();
                }
                if (now < stopAt) return;
                EditorApplication.update -= Tick;
                EditorApplication.ExitPlaymode();
            }
            EditorApplication.update += Tick;
        }

        private static void ShowPanels()
        {
            foreach (MonoBehaviour behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
            {
                if (behaviour == null || System.Array.IndexOf(s_panelsToShow, behaviour.GetType().Name) < 0) continue;
                for (Transform t = behaviour.transform; t != null; t = t.parent) t.gameObject.SetActive(true);
            }
        }

        private static void WriteReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Screen {Screen.width}x{Screen.height}");
            foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude))
                if (canvas.isRootCanvas)
                    sb.AppendLine($"Canvas '{canvas.name}' {canvas.renderMode} scale {canvas.scaleFactor} refPPU {canvas.referencePixelsPerUnit}");
            foreach (Image image in Object.FindObjectsByType<Image>(FindObjectsInactive.Exclude))
            {
                if (image.sprite == null || image.canvas == null || image.canvas.rootCanvas.renderMode == RenderMode.WorldSpace) continue;
                Rect r = image.rectTransform.rect;
                sb.AppendLine($"{HierarchyPath(image.transform)}: {r.width}x{r.height} scale {image.transform.lossyScale.x / image.canvas.rootCanvas.scaleFactor:0.###} sprite '{image.sprite.name}' {image.sprite.rect.width}x{image.sprite.rect.height} @{image.sprite.pixelsPerUnit}ppu type {image.type}");
            }
            File.WriteAllText("Logs/Dev/probe.txt", sb.ToString());
        }

        private static string HierarchyPath(Transform t) => t.parent == null ? t.name : HierarchyPath(t.parent) + "/" + t.name;
    }
}
