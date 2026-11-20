using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Isekai.Dev.Editor
{
    /// <summary>
    /// Builds the Windows player of the whole game from the scenes in the Build Settings (Boot, Menu, Planet, Region) into
    /// <c>Builds/Windows</c> in the project. From the command line:
    /// <c>Unity.exe -batchmode -projectPath . -executeMethod Isekai.Dev.Editor.BuildPlayer.Windows -quit</c>
    /// (the process exits 0 when the build succeeded, 1 when it did not). See docs/playtest-build.md.
    /// </summary>
    public static class BuildPlayer
    {
        public const string OutputFolder = "Builds/Windows";
        public const string ExecutableName = "IsekaiConquerors.exe";

        [MenuItem("Tools/Isekai/Dev/Build/Windows player")]
        public static void WindowsFromMenu() => Build();

        /// <summary>The command-line entry: builds and exits with the result.</summary>
        public static void Windows()
        {
            bool ok = Build();
            EditorApplication.Exit(ok ? 0 : 1);
        }

        /// <summary>The scenes the player is built from: the enabled ones in the Build Settings, in order.</summary>
        public static string[] BuildScenes() => EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();

        private static bool Build()
        {
            string[] scenes = BuildScenes();
            if (scenes.Length == 0)
            {
                Debug.LogError("BuildPlayer: no enabled scenes in the Build Settings.");
                return false;
            }
            string project = Directory.GetParent(Application.dataPath).FullName;
            string folder = Path.GetFullPath(Path.Combine(project, OutputFolder));
            Directory.CreateDirectory(folder);
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = Path.Combine(folder, ExecutableName),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            Debug.Log($"BuildPlayer: {summary.result}, {summary.totalErrors} errors, {summary.totalWarnings} warnings, {summary.totalSize / (1024 * 1024)} MB, {summary.totalTime.TotalSeconds:0} s -> {options.locationPathName}");
            return summary.result == BuildResult.Succeeded;
        }
    }
}
