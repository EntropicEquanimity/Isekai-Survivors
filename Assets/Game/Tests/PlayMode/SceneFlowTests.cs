using System;
using System.Collections;
using Blondie.Core;
using Blondie.GameSetup;
using Blondie.Juice;
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
    /// The front end through the game's real scenes: Boot → Menu → Play → Region → Pause → Main Menu.
    /// Player settings stay in memory and are not applied, so nothing outside the test changes.
    /// </summary>
    public class SceneFlowTests
    {
        private const float TimeoutSeconds = 20f;
        private IKeyValueStore _previousStore;
        private ISettingsApplier _previousApplier;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _previousStore = KeyValueStorage.Current;
            KeyValueStorage.Current = new MemoryKeyValueStore();
            _previousApplier = GameSettings.Applier;
            GameSettings.Applier = null;
            TutorialRun.ResetForTests();
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
                ConfirmLeave = false,
                UseSaveSlots = false,
                PanelMotionSeconds = 0f,
                StaggerSeconds = 0f,
            });
            PackageSettings<GameSetupUnitySettings>.Set(new GameSetupUnitySettings());   // no splash screens or music
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
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
            TutorialRun.ResetForTests();
            GameSetup.SetState(GameSetupState.Gameplay);
        }

        [UnityTest]
        public IEnumerator Boot_ReachesMenu_PlayLoadsRegion_PauseAndBackToMenu()
        {
            GameSetupSettings s = GameSetupSettings.Current;
            SceneManager.LoadScene(s.BootScene);

            yield return TestWait.Until(() => GameSetup.State == GameSetupState.MainMenu && ActiveScene == s.MainMenuScene, "Boot reaches the Menu scene", TimeoutSeconds);

            GameSetup.Play();
            yield return TestWait.Until(() => GameSetup.State == GameSetupState.Gameplay && ActiveScene == s.GameScene, "Play loads the Region scene", TimeoutSeconds);

            GameSetup.Pause();
            Assert.IsTrue(GameSetup.IsPaused, "Pause pauses");
            Assert.IsNotNull(MenuStack.Top, "the pause menu is open");

            GameSetup.GoToMainMenu();
            yield return TestWait.Until(() => GameSetup.State == GameSetupState.MainMenu && ActiveScene == s.MainMenuScene, "Main Menu goes back to the Menu scene", TimeoutSeconds);
            Assert.IsFalse(GameSetup.IsPaused, "leaving gameplay unpauses");
        }

        private static string ActiveScene => SceneManager.GetActiveScene().name;

    }
}
