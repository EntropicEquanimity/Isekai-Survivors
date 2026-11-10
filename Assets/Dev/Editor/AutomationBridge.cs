using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace Isekai.Dev.Editor
{
    /// <summary>
    /// Dev-only tool (Editor only, not in builds), copied from BlondieUtils, that lets humans and AI agents work with this project from outside the Editor.
    /// <list type="bullet">
    /// <item>Every test run (from the Test Runner window or here) is saved to <c>Logs/Dev/</c>:
    /// <c>test-&lt;mode&gt;.xml</c> (NUnit XML) and <c>test-summary.txt</c> (failures with messages).</item>
    /// <item>Every script compilation writes its errors and warnings to <c>Logs/Dev/compile.txt</c>.</item>
    /// <item>Writing a command into <c>Logs/Dev/request.txt</c> makes the Editor run it within a second, even when it
    /// is not focused (a request written while Unity is closed runs after the next launch): <c>refresh</c>, <c>resolve</c> (re-resolve packages, e.g. after adding one under Packages/), <c>menu &lt;Menu/Path&gt;</c>, <c>exec &lt;Type.Method&gt; [argument]</c>, <c>quit</c>, <c>test editmode</c>, <c>test playmode</c>, <c>test all</c>. Progress goes to
    /// <c>Logs/Dev/status.txt</c>.</item>
    /// <item>Console messages (warnings, errors, and info logs) are appended to <c>Logs/Dev/console.txt</c>, which is cleared on each Editor launch.</item>
    /// </list>
    /// Menu: Tools/Isekai/Dev/Run All Tests.
    /// </summary>
    [InitializeOnLoad]
    internal static class AutomationBridge
    {
        private static readonly string s_requestPath = Path.Combine("Logs", "Dev", "request.txt");
        private const string PendingKey = "Isekai.Dev.PendingTestModes";
        private static readonly string s_outDir = Path.Combine("Logs", "Dev");
        private static readonly List<string> s_compileMessages = new();
        private static double s_nextPoll;
        private static readonly string s_consolePath = Path.Combine("Logs", "Dev", "console.txt");
        private static readonly object s_consoleLock = new();
        private const string ConsoleClearedKey = "Isekai.Dev.ConsoleCleared";

        static AutomationBridge()
        {
            Directory.CreateDirectory(s_outDir);
            TestRunnerApi.RegisterTestCallback(new ResultRecorder());
            EditorApplication.update += Poll;
            CompilationPipeline.compilationStarted += _ => s_compileMessages.Clear();
            CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompiled;
            CompilationPipeline.compilationFinished += _ => WriteCompileReport();
            if (!SessionState.GetBool(ConsoleClearedKey, false))
            {
                SessionState.SetBool(ConsoleClearedKey, true);
                File.WriteAllText(s_consolePath, "");
            }
            Application.logMessageReceivedThreaded -= OnLog;
            Application.logMessageReceivedThreaded += OnLog;
        }

        private static void OnLog(string message, string stackTrace, LogType type)
        {
            var sb = new StringBuilder();
            sb.Append($"{DateTime.Now:HH:mm:ss} {type}: {message}\n");
            if (type is LogType.Error or LogType.Exception or LogType.Assert)
            {
                string[] lines = (stackTrace ?? "").Split('\n');
                for (int i = 0; i < Math.Min(3, lines.Length); i++)
                    if (lines[i].Trim().Length > 0) sb.Append("    " + lines[i].Trim() + "\n");
            }
            lock (s_consoleLock)
            {
                try
                {
                    if (File.Exists(s_consolePath) && new FileInfo(s_consolePath).Length > 512 * 1024) File.WriteAllText(s_consolePath, "(truncated)\n");
                    File.AppendAllText(s_consolePath, sb.ToString());
                }
                catch (IOException) { }
            }
        }

        [MenuItem("Tools/Isekai/Dev/Run All Tests")]
        private static void RunAllMenu() => RunTests("all");

        private static void Poll()
        {
            if (EditorApplication.timeSinceStartup < s_nextPoll) return;
            s_nextPoll = EditorApplication.timeSinceStartup + 1.0;
            if (!File.Exists(s_requestPath) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;

            string command, raw;
            try
            {
                raw = File.ReadAllText(s_requestPath).Trim();
                command = raw.ToLowerInvariant();
                File.Delete(s_requestPath);
            }
            catch (IOException)
            {
                return; // still being written; try again next poll
            }

            Status($"request: {command}");
            if (command == "refresh") AssetDatabase.Refresh();
            else if (command == "resolve") UnityEditor.PackageManager.Client.Resolve();
            else if (command == "quit")
            {
                Status("quitting the Editor");
                EditorApplication.delayCall += () => EditorApplication.Exit(0);
            }
            else if (command.StartsWith("exec ")) Exec(raw.Substring(5).Trim());
            else if (command.StartsWith("menu "))
            {
                string path = raw.Substring(5).Trim();
                Status(EditorApplication.ExecuteMenuItem(path) ? $"ran menu {path}" : $"menu not found: {path}");
            }
            else if (command.StartsWith("test ")) RunTests(command.Substring(5).Trim());
            else Status($"unknown request '{command}'");
        }

        /// <summary>
        /// <c>exec Namespace.Type.Method [argument]</c>: calls a public static method taking no parameters or one string
        /// (the rest of the line), on the main thread. Its return value, if any, goes to the status file.
        /// </summary>
        private static void Exec(string spec)
        {
            int space = spec.IndexOf(' ');
            string target = space < 0 ? spec : spec.Substring(0, space);
            string argument = space < 0 ? null : spec.Substring(space + 1);
            int dot = target.LastIndexOf('.');
            if (dot <= 0) { Status($"exec: expected Type.Method, got '{target}'"); return; }
            string typeName = target.Substring(0, dot), methodName = target.Substring(dot + 1);
            Type type = null;
            foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                if ((type = assembly.GetType(typeName)) != null) break;
            if (type == null) { Status($"exec: type {typeName} not found"); return; }
            System.Reflection.MethodInfo method = argument == null
                ? type.GetMethod(methodName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static, null, Type.EmptyTypes, null)
                : type.GetMethod(methodName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static, null, new[] { typeof(string) }, null);
            if (method == null) { Status($"exec: no public static {methodName}({(argument == null ? "" : "string")}) on {typeName}"); return; }
            try
            {
                object result = method.Invoke(null, argument == null ? null : new object[] { argument });
                Status($"exec {target} done{(result != null ? ": " + result.ToString().Replace("\n", " | ") : "")}");
            }
            catch (System.Reflection.TargetInvocationException e)
            {
                Status($"exec {target} failed: {e.InnerException?.Message}");
                Debug.LogException(e.InnerException ?? e);
            }
        }

        private static void RunTests(string which)
        {
            switch (which)
            {
                case "editmode": Execute(TestMode.EditMode); break;
                case "playmode": Execute(TestMode.PlayMode); break;
                case "all":
                    SessionState.SetString(PendingKey, "playmode");
                    Execute(TestMode.EditMode);
                    break;
                default: Status($"unknown test mode '{which}'"); break;
            }
        }

        private static void Execute(TestMode mode)
        {
            if (mode == TestMode.PlayMode) DiscardUntitledScenes();
            Status($"running {mode} tests");
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.Execute(new ExecutionSettings(new Filter { testMode = mode, assemblyNames = ProjectTestAssemblies(), groupNames = new[] { "^Isekai\\." } }));
        }

        // Play Mode tests ask to save modified scenes, a dialog that stalls an unattended run. Edit Mode tests leave the
        // Editor's untitled scene modified (they add and remove objects), and an untitled scene holds nothing to keep:
        // replace it with a fresh one. A saved scene is never touched.
        private static void DiscardUntitledScenes()
        {
            bool dirty = false;
            for (int i = 0; i < UnityEditor.SceneManagement.EditorSceneManager.sceneCount; i++)
            {
                UnityEngine.SceneManagement.Scene scene = UnityEditor.SceneManagement.EditorSceneManager.GetSceneAt(i);
                if (!string.IsNullOrEmpty(scene.path)) return;
                dirty |= scene.isDirty;
            }
            if (!dirty) return;
            UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects, UnityEditor.SceneManagement.NewSceneMode.Single);
            Status("replaced the modified untitled scene with a new one before Play Mode tests");
        }

        /// <summary>Only this project's test assemblies (Isekai.*.Tests.*), not tests of packages listed as testables.</summary>
        private static string[] ProjectTestAssemblies()
        {
            var names = new List<string>();
            foreach (UnityEditor.Compilation.Assembly assembly in CompilationPipeline.GetAssemblies())
                if (assembly.name.StartsWith("Isekai.") && assembly.name.Contains(".Tests.")) names.Add(assembly.name);
            return names.ToArray();
        }

        private static void OnRunFinished(ITestResultAdaptor result)
        {
            string mode = result.Test != null && (result.Test.TestMode & TestMode.PlayMode) != 0 ? "playmode" : "editmode";
            try
            {
                TestRunnerApi.SaveResultToFile(result, Path.GetFullPath(Path.Combine(s_outDir, $"test-{mode}.xml")));
                File.WriteAllText(Path.Combine(s_outDir, $"test-{mode}-summary.txt"), Summarize(mode, result));
            }
            catch (Exception e)
            {
                Status("could not write results: " + e.Message);
            }
            Status($"{mode} finished: {result.PassCount} passed, {result.FailCount} failed, {result.SkipCount} skipped");

            string pending = SessionState.GetString(PendingKey, "");
            if (pending.Length > 0)
            {
                SessionState.EraseString(PendingKey);
                EditorApplication.delayCall += () => RunTests(pending);
            }
        }

        private static string Summarize(string mode, ITestResultAdaptor root)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{mode} {DateTime.Now:yyyy-MM-dd HH:mm:ss}: {root.PassCount} passed, {root.FailCount} failed, {root.SkipCount} skipped, {root.InconclusiveCount} inconclusive");
            AppendFailures(root, sb);
            return sb.ToString();
        }

        private static void AppendFailures(ITestResultAdaptor node, StringBuilder sb)
        {
            if (node.HasChildren)
            {
                foreach (ITestResultAdaptor child in node.Children) AppendFailures(child, sb);
                return;
            }
            if (node.TestStatus != TestStatus.Failed) return;
            sb.AppendLine();
            sb.AppendLine("FAILED " + node.FullName);
            sb.AppendLine("  " + (node.Message ?? "").Trim().Replace("\n", "\n  "));
            string stack = (node.StackTrace ?? "").Trim();
            if (stack.Length > 0) sb.AppendLine("  at " + stack.Split('\n')[0].Trim());
            string output = (node.Output ?? "").Trim();
            if (output.Length > 0) sb.AppendLine("  output: " + (output.Length > 600 ? output.Substring(0, 600) + "…" : output).Replace("\n", "\n    "));
        }

        private static void OnAssemblyCompiled(string assembly, CompilerMessage[] messages)
        {
            foreach (CompilerMessage m in messages)
                s_compileMessages.Add($"{m.type}: {m.file}({m.line},{m.column}): {m.message}");
        }

        private static void WriteCompileReport()
        {
            int errors = s_compileMessages.FindAll(m => m.StartsWith("Error")).Count;
            var sb = new StringBuilder();
            sb.AppendLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss}: {errors} errors, {s_compileMessages.Count - errors} warnings");
            foreach (string m in s_compileMessages) sb.AppendLine(m);
            File.WriteAllText(Path.Combine(s_outDir, "compile.txt"), sb.ToString());
        }

        private static void Status(string message)
        {
            File.AppendAllText(Path.Combine(s_outDir, "status.txt"), $"{DateTime.Now:HH:mm:ss} {message}\n");
        }

        private sealed class ResultRecorder : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) { }
            public void RunFinished(ITestResultAdaptor result) => OnRunFinished(result);
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }
        }
    }
}
