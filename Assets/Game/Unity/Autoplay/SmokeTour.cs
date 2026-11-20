using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Blondie.Core;
using Blondie.GameSetup;
using Blondie.Save;
using Blondie.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Isekai
{
    /// <summary>
    /// A scripted tour of the real front end for a built player, started with <c>-smoke</c> (see <see cref="SmokeOptions"/>): from
    /// the main menu it presses Tutorial, lets the tutorial Region run for a while, leaves through the pause menu's Main Menu
    /// button (and its question), presses Credits, closes them with Back, and quits. Unlike <c>-autoplay</c>, which skips the
    /// menus, every step goes through the buttons a player presses. It quits with <see cref="SmokeExit"/>'s code: 0 for a
    /// clean tour, 2 when errors were logged, 3 when a step never came (and says which). Saves go to memory, so the player's own
    /// Run is never touched.
    /// </summary>
    public sealed class SmokeTour : MonoBehaviour
    {
        private const float StepTimeout = 60f;

        private readonly List<string> _steps = new();
        private readonly List<string> _errors = new();
        private SmokeOptions _options;
        private ISaveStorage _savedStorage;
        private bool _savedRunInBackground;
        private string _stalledOn;
        private bool _restored;

        /// <summary>The tour in progress or last run (null when none).</summary>
        public static SmokeTour Current { get; private set; }

        public bool IsFinished { get; private set; }

        /// <summary>The steps reached so far, in order.</summary>
        public IReadOnlyList<string> Steps => _steps;

        /// <summary>The errors the game logged during the tour (the first few messages).</summary>
        public IReadOnlyList<string> Errors => _errors;

        public int ErrorsLogged { get; private set; }

        /// <summary>The step that never came, or null.</summary>
        public string StalledOn => _stalledOn;

        public int ExitCode => SmokeExit.Code(_stalledOn != null, ErrorsLogged);

        /// <summary>Starts a tour. Null (and a warning) when one is going.</summary>
        public static SmokeTour Begin(SmokeOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (Current != null && !Current.IsFinished)
            {
                BLog.Warning("Smoke", "A tour is already running.");
                return null;
            }
            if (Current != null) Destroy(Current.gameObject);
            var go = new GameObject("Smoke Tour");
            DontDestroyOnLoad(go);
            var tour = go.AddComponent<SmokeTour>();
            tour._options = options;
            Current = tour;
            tour.Setup();
            tour.StartCoroutine(tour.Tour());
            return tour;
        }

        /// <summary>Gives a running tour up and puts the game's own settings back (nothing is quit).</summary>
        public static void Stop()
        {
            if (Current == null) return;
            Current.Restore();
            Destroy(Current.gameObject);
            Current = null;
        }

        private void Setup()
        {
            _savedStorage = SaveSystem.Storage;
            SaveSystem.Storage = new MemorySaveStorage();
            _savedRunInBackground = Application.runInBackground;
            Application.runInBackground = true;
            Application.logMessageReceived += OnLog;
        }

        private void OnDestroy() => Restore();

        private void Restore()
        {
            if (_restored) return;
            _restored = true;
            Application.logMessageReceived -= OnLog;
            if (_savedStorage != null) SaveSystem.Storage = _savedStorage;
            Application.runInBackground = _savedRunInBackground;
            Time.timeScale = 1f;
        }

        private void OnLog(string message, string stack, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            ErrorsLogged++;
            string first = message.Split('\n')[0];
            if (_errors.Count < 10 && !_errors.Contains(first)) _errors.Add(first);
        }

        private void Reached(string step)
        {
            _steps.Add(step);
            BLog.Info("Smoke", $"Step: {step}.");
        }

        // ---------------------------------------------------------------- the tour

        private IEnumerator Tour()
        {
            BLog.Info("Smoke", $"Tour: tutorial Region for {_options.Seconds:0} s at speed {_options.Speed:0.#}.");
            yield return UntilMainMenu("the main menu");
            if (_stalledOn != null) { Finish(); yield break; }
            Reached("main menu");

            PressMain(GameMenus.TutorialText);
            yield return Until(() => SceneManager.GetActiveScene().name == RunSession.RegionScene && RegionBootstrap.Current != null
                && RegionBootstrap.Current.IsPopulated && TutorialRun.IsActive && FindAnyObjectByType<RegionTutorial>() != null, "the tutorial Region");
            if (_stalledOn != null) { Finish(); yield break; }
            Reached("tutorial Region");

            // Let it run a while at speed, as a player would.
            float until = Time.realtimeSinceStartup + _options.Seconds;
            while (Time.realtimeSinceStartup < until && SceneManager.GetActiveScene().name == RunSession.RegionScene)
            {
                Time.timeScale = GamePause.IsPaused || GameSetup.IsPaused ? 0f : _options.Speed;
                yield return null;
            }
            Time.timeScale = 1f;
            if (SceneManager.GetActiveScene().name != RunSession.RegionScene) { Stall("the tutorial Region to last"); Finish(); yield break; }
            Reached($"tutorial Region ran {_options.Seconds:0} s");

            GameSetup.Pause();
            yield return Until(() => MenuStack.Top is PauseMenuPresenter, "the pause menu");
            if (_stalledOn != null) { Finish(); yield break; }
            yield return null;
            Reached("pause menu");
            var pause = (PauseMenuPresenter)MenuStack.Top;
            int leave = pause.Buttons.ToList().FindIndex(b => b.Text.ToLowerInvariant().Contains("main menu"));
            if (leave < 0) { Stall("a Main Menu button on the pause menu"); Finish(); yield break; }
            pause.Press(leave);
            for (float waited = 0f; waited < 3f; waited += Time.unscaledDeltaTime)
            {
                if (MenuStack.Top is ConfirmPresenter confirm && !confirm.IsInputBlocked) { confirm.Answer(true); break; }
                yield return null;
            }
            yield return UntilMainMenu("the main menu again");
            if (_stalledOn != null) { Finish(); yield break; }
            Reached("back at the main menu");

            PressMain(GameMenus.CreditsText);
            yield return Until(() => MenuStack.Contains(CreditsScreen.MenuName), "the credits");
            if (_stalledOn != null) { Finish(); yield break; }
            Reached("credits");
            yield return new WaitForSecondsRealtime(1f);
            MenuStack.CloseTop();
            yield return Until(() => !MenuStack.Contains(CreditsScreen.MenuName) && MenuStack.Top is MainMenuPresenter, "the main menu after the credits");
            if (_stalledOn != null) { Finish(); yield break; }
            Reached("credits closed");
            Finish();
        }

        private IEnumerator UntilMainMenu(string what) =>
            Until(() => GameSetup.State == GameSetupState.MainMenu && SceneManager.GetActiveScene().name == GameSetupSettings.Current.MainMenuScene
                && !GameScenes.IsLoading && MenuStack.Top is MainMenuPresenter, what);

        private IEnumerator Until(Func<bool> done, string what)
        {
            float start = Time.realtimeSinceStartup;
            while (!done())
            {
                if (Time.realtimeSinceStartup - start > StepTimeout) { Stall(what); yield break; }
                yield return null;
            }
        }

        private void Stall(string what)
        {
            _stalledOn = what;
            BLog.Error("Smoke", $"Stalled: {what} never came (after steps: {string.Join(", ", _steps)}).");
        }

        private static void PressMain(string text)
        {
            var menu = (MainMenuPresenter)MenuStack.Top;
            int index = menu.Buttons.ToList().FindIndex(b => string.Equals(b.Text, text, StringComparison.OrdinalIgnoreCase));
            if (index < 0) throw new InvalidOperationException($"The main menu has no '{text}' button.");
            menu.Press(index);
        }

        private void Finish()
        {
            Restore();
            IsFinished = true;
            BLog.Info("Smoke", $"Tour done: {_steps.Count} steps, {ErrorsLogged} errors logged, " + (_stalledOn != null ? $"stalled on {_stalledOn}" : "no stall") + $", exit {ExitCode}.");
            if (_options.QuitWhenDone) Quit(ExitCode);
        }

        private static void Quit(int code)
        {
#if UNITY_EDITOR
            if (Application.isBatchMode) UnityEditor.EditorApplication.Exit(code);
            else UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit(code);
#endif
        }

        // A build (or the Editor in a batch) started with -smoke: take the tour as soon as the game is up.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartFromArguments()
        {
            string[] args = Environment.GetCommandLineArgs();
            if (Array.FindIndex(args, a => string.Equals(a, SmokeOptions.Flag, StringComparison.OrdinalIgnoreCase)) < 0) return;
            if (!SmokeOptions.TryParse(args, out SmokeOptions options, out string error))
            {
                Debug.LogError(error);
                Quit(SmokeExit.Stalled);
                return;
            }
            if (options != null) Begin(options);
        }
    }
}
