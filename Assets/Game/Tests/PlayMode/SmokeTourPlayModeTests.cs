using System;
using System.Collections;
using Blondie.Core;
using Blondie.GameSetup;
using Blondie.Juice;
using Blondie.Save;
using Blondie.Settings;
using Blondie.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Isekai.Tests
{
    /// <summary>
    /// The scripted tour a built player runs with <c>-smoke</c>: main menu, the Tutorial button, the tutorial Region, the pause
    /// menu's way back to the main menu (with its question), the Credits button and Back. Settings and saves stay in memory.
    /// </summary>
    public class SmokeTourPlayModeTests
    {
        private IKeyValueStore _previousStore;
        private ISettingsApplier _previousApplier;
        private Action _previousTutorial;
        private Action _previousCredits;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _previousStore = KeyValueStorage.Current;
            KeyValueStorage.Current = new MemoryKeyValueStore();
            _previousApplier = GameSettings.Applier;
            GameSettings.Applier = null;
            _previousTutorial = MenuHooks.StartTutorial;
            _previousCredits = MenuHooks.OpenCredits;
            MenuHooks.StartTutorial = TutorialRun.StartFromMenu;
            MenuHooks.OpenCredits = CreditsScreen.Open;
            GameSetupSettings real = GameSetupSettings.Current;
            PackageSettings<GameSetupSettings>.Set(new GameSetupSettings
            {
                BootScene = real.BootScene,
                MainMenuScene = real.MainMenuScene,
                GameScene = real.GameScene,
                SkipMenusInEditor = false,
                FirstLaunchLanguage = false,
                FirstLaunchSettings = false,
                ShowLoadingScreen = false,
                MinLoadingSeconds = 0f,
                Transition = new ScreenTransition { Kind = TransitionKind.Cut },
                ConfirmLeave = true,   // leaving a Region asks first, as in the real settings
                ConfirmNewGame = false,
                UseSaveSlots = true,
                MainMenuItems = real.MainMenuItems,
                PauseMenuItems = real.PauseMenuItems,
                SettingsTabs = real.SettingsTabs,
                PanelMotionSeconds = 0f,
                StaggerSeconds = 0f,
            });
            PackageSettings<GameSetupUnitySettings>.Set(new GameSetupUnitySettings());
            SaveSystem.MaxSlots = 1;
            SaveSystem.NewGame();
            RunSave.Delete();
            RunSession.Clear();
            TutorialRun.ResetForTests();
            TutorialProgressStore.ResetForTests();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SmokeTour.Stop();
            Time.timeScale = 1f;
            GameSetup.Resume();
            MenuStack.CloseAll();
            foreach (MonoBehaviour b in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
                if (b != null && b.GetType().Name == "GameSetupRunner") Object.Destroy(b.gameObject);   // internal to Game Setup
            if (UIRoot.HasInstance) Object.Destroy(UIRoot.Instance.gameObject);
            yield return null;
            PackageSettings<GameSetupSettings>.Set(null);
            PackageSettings<GameSetupUnitySettings>.Set(null);
            KeyValueStorage.Current = _previousStore;
            GameSettings.Applier = _previousApplier;
            MenuHooks.StartTutorial = _previousTutorial;
            MenuHooks.OpenCredits = _previousCredits;
            TutorialRun.ResetForTests();
            TutorialProgressStore.ResetForTests();
            RunSave.Delete();
            RunSession.Clear();
            GameSetup.SetState(GameSetupState.Gameplay);
        }

        [UnityTest]
        public IEnumerator The_tour_goes_from_the_main_menu_through_the_tutorial_and_the_credits_and_back_and_ends_clean()
        {
            SceneManager.LoadScene(GameSetupSettings.Current.BootScene);
            SmokeTour tour = SmokeTour.Begin(new SmokeOptions { Seconds = 2f, Speed = 4f, QuitWhenDone = false });
            yield return TestWait.Until(() => tour.IsFinished, "the tour finishes", 150f);

            Assert.IsNull(tour.StalledOn, "no step stalled");
            CollectionAssert.AreEqual(new[]
            {
                "main menu", "tutorial Region", "tutorial Region ran 2 s", "pause menu", "back at the main menu", "credits", "credits closed",
            }, tour.Steps);
            Assert.AreEqual(0, tour.ErrorsLogged, string.Join("; ", tour.Errors));
            Assert.AreEqual(SmokeExit.Clean, tour.ExitCode);
            Assert.IsTrue(MenuStack.Top is MainMenuPresenter, "the game is left at the main menu");
            Assert.AreEqual(1f, Time.timeScale, "the game's own speed is back");
            Assert.IsFalse(RunSave.Exists, "the tour touches no save");
        }
    }
}
