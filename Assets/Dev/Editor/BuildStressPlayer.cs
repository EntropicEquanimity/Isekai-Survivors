using UnityEditor;
using UnityEditor.Build.Reporting;

namespace Isekai.Dev.Editor
{
    /// <summary>
    /// Dev-only: a Windows player of the Region scene alone, for horde stress tests.
    /// <c>exec Isekai.Dev.Editor.BuildStressPlayer.Run C:/somewhere/Isekai.exe</c>, then run it with
    /// <c>-horde 3000 -quit-after 45 -logFile stress.log</c> and read the "[Perf]" lines (see FrameStats).
    /// Build outside the repo: Builds/ is not git-ignored.
    /// </summary>
    public static class BuildStressPlayer
    {
        public static string Run(string exePath)
        {
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Game/Scenes/Region.unity" },
                locationPathName = exePath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            });
            return $"{report.summary.result}, {report.summary.totalErrors} errors, {report.summary.totalTime.TotalSeconds:F0} s";
        }
    }
}
