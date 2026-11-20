using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Isekai.Dev.Editor
{
    /// <summary>
    /// Starts the autoplay bot from the Editor. The menu items under Tools/Isekai/Dev/Autoplay enter Play Mode and play a
    /// batch (the Editor stays open and the game keeps running when it is done); <see cref="RunBatch"/> starts one from the
    /// command line and quits with the batch's exit code:
    /// <c>Unity.exe -batchmode -projectPath . -executeMethod Isekai.Dev.Editor.AutoplayMenu.RunBatch -autoplay runs=3 seeds=1,2,3 anchor=Shrine strategy=balanced speed=8</c>
    /// (no -quit: the batch quits when it is done). Reports are written under <c>Logs/Autoplay</c> in the project.
    /// </summary>
    public static class AutoplayMenu
    {
        private const string Root = "Tools/Isekai/Dev/Autoplay/";

        [MenuItem(Root + "Play one balanced Run")]
        private static void Balanced() => Start("-autoplay runs=1 strategy=balanced speed=8");

        [MenuItem(Root + "Play one stealth Run")]
        private static void Stealth() => Start("-autoplay runs=1 strategy=stealth speed=8");

        [MenuItem(Root + "Play one rush Run")]
        private static void Rush() => Start("-autoplay runs=1 strategy=rush speed=8");

        [MenuItem(Root + "Play three balanced Runs")]
        private static void Three() => Start("-autoplay runs=3 seeds=101,102,103 strategy=balanced speed=8");

        [MenuItem(Root + "Open the reports folder")]
        private static void OpenReports()
        {
            string folder = AutoplayReportFiles.DefaultDirectory;
            System.IO.Directory.CreateDirectory(folder);
            EditorUtility.RevealInFinder(folder);
        }

        /// <summary>The command-line entry: plays the batch named by the arguments after <c>-autoplay</c>, then the game quits with its exit code.</summary>
        public static void RunBatch()
        {
            string[] args = Environment.GetCommandLineArgs();
            int flag = Array.FindIndex(args, a => string.Equals(a, AutoplayOptions.Flag, StringComparison.OrdinalIgnoreCase));
            if (flag < 0)
            {
                Debug.LogError("AutoplayMenu.RunBatch needs -autoplay and its options on the command line.");
                EditorApplication.Exit(3);
                return;
            }
            if (!AutoplayOptions.TryParse(args, out _, out string error))
            {
                Debug.LogError(error);
                EditorApplication.Exit(3);
                return;
            }
            Start(string.Join(" ", args.Skip(flag)));
        }

        private static void Start(string arguments)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Autoplay: already in Play Mode.");
                return;
            }
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (!SceneManager.GetSceneAt(i).isDirty) continue;
                Debug.LogWarning("Autoplay: an open scene has unsaved changes; save it first.");
                return;
            }
            EditorSceneManager.OpenScene("Assets/Game/Scenes/Planet.unity", OpenSceneMode.Single);
            AutoplayRunner.SetEditorArgs(arguments);
            EditorApplication.EnterPlaymode();
        }
    }
}
