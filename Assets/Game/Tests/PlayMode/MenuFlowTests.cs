using System;
using System.Collections;
using System.Linq;
using Blondie.Core;
using Blondie.GameSetup;
using Blondie.Inventory;
using Blondie.Juice;
using Blondie.Save;
using Blondie.Settings;
using Blondie.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Isekai.Tests
{
    /// <summary>
    /// The main menu, the pause menu, the settings rows and continuing a saved Run, through the game's real scenes
    /// (Boot, Menu, Planet, Region). Player settings and saves stay in memory.
    /// </summary>
    public class MenuFlowTests
    {
        private const float Timeout = 20f;
        private IKeyValueStore _previousStore;
        private ISettingsApplier _previousApplier;
        private Action _previousTutorial;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _previousStore = KeyValueStorage.Current;
            KeyValueStorage.Current = new MemoryKeyValueStore();
            _previousApplier = GameSettings.Applier;
            GameSettings.Applier = null;
            _previousTutorial = MenuHooks.StartTutorial;
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
                ConfirmNewGame = false,
                UseSaveSlots = true,
                MainMenuItems = real.MainMenuItems,
                PauseMenuItems = real.PauseMenuItems,
                SettingsTabs = real.SettingsTabs,
                PanelMotionSeconds = 0f,
                StaggerSeconds = 0f,
            });
            PackageSettings<GameSetupUnitySettings>.Set(new GameSetupUnitySettings());   // no splash screens or music
            SaveSystem.MaxSlots = 1;
            SaveSystem.NewGame();
            RunSave.Delete();
            RunSession.Clear();
            TutorialRun.ResetForTests();
            TutorialProgressStore.ResetForTests();
            TutorialProgressStore.MarkSkipped();
            SideOutlineSettingsStore.ResetForTests();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
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
            TutorialRun.ResetForTests();
            TutorialProgressStore.ResetForTests();
            RunSave.Delete();
            RunSession.Clear();
            SideOutlineSettingsStore.SetPreset(OutlinePreset.Default);
            SideOutlineSettingsStore.ClearAllOverrides();
            SideOutlineSettingsStore.ResetForTests();
            GameSetup.SetState(GameSetupState.Gameplay);
        }

        private static string ActiveScene => SceneManager.GetActiveScene().name;

        private static string Normal(string text) => text.ToLowerInvariant();

        private static IEnumerator BootToMenu()
        {
            SceneManager.LoadScene(GameSetupSettings.Current.BootScene);
            yield return TestWait.Until(() => GameSetup.State == GameSetupState.MainMenu && ActiveScene == GameSetupSettings.Current.MainMenuScene, "Boot reaches the Menu scene", Timeout);
            yield return null;
        }

        private static IEnumerator PlanetShows(string what) =>
            TestWait.Until(() => ActiveScene == RunSession.PlanetScene && Object.FindAnyObjectByType<PlanetScreen>()?.Map != null, what, Timeout);

        private static IEnumerator RegionPopulated() =>
            TestWait.Until(() => RegionBootstrap.Current != null && RegionBootstrap.Current.IsPopulated, "the Region is populated", Timeout);

        private static string[] MainMenuButtons() => ((MainMenuPresenter)MenuStack.Top).Buttons.Select(b => b.Text).ToArray();

        private static void PressMain(string text)
        {
            var menu = (MainMenuPresenter)MenuStack.Top;
            int index = menu.Buttons.ToList().FindIndex(b => Normal(b.Text) == Normal(text));
            Assert.GreaterOrEqual(index, 0, $"the main menu has '{text}'");
            menu.Press(index);
        }

        // New Run from the menu, then land on the first Region.
        private static IEnumerator InRegion()
        {
            yield return BootToMenu();
            PressMain(GameMenus.NewRunText);
            yield return PlanetShows("the Planet map shows");
            Assert.IsTrue(RunSession.GoToRegion(RunSession.Run.Available().First()));
            yield return RegionPopulated();
            RegionTestScene.DisableDrafts();
            yield return TestWait.Until(() => RunSession.Run.RunSeconds > 0f, "the Run clock runs", 5f);
        }

        // A Run saved on the Planet screen after capturing one Region, with an Item, an Anchor, a soul level and deaths.
        private static IEnumerator SaveARun(Action<RegionInfo> captured)
        {
            RunSession.StartNew(7, new[] { 2, 1 });
            PlanetRun run = RunSession.Run;
            SceneManager.LoadScene(RunSession.RegionScene);   // the Run lands on its first Region
            yield return null;
            yield return null;
            CharacterInventory inventory = CharacterInventory.Current;
            Assert.IsTrue(inventory.Give(ItemDatabase.Find("heart_charm")));
            RunSession.StoreInventory(inventory.Unit.Capture());   // what capturing a Region keeps for the next one
            RegionInfo region = run.Current;
            RunSession.Anchor = AnchorKind.WarThrone;
            SoulRun.Ledger.TryRaiseLevel(_ => true);
            SoulRun.Ledger.RecordDeath();
            SoulRun.Ledger.RecordDeath();
            run.CaptureCurrent();
            captured(region);

            SceneManager.LoadScene(RunSession.PlanetScene);   // the Planet screen saves the Run
            yield return PlanetShows("the Planet map shows");
        }

        [UnityTest]
        public IEnumerator Saving_on_the_Planet_screen_and_continuing_restores_the_Run()
        {
            RegionInfo first = null;
            yield return SaveARun(r => first = r);
            Assert.IsTrue(RunSave.Exists, "the Planet screen saved the Run");
            int seed = RunSession.Seed;
            float seconds = RunSession.Run.RunSeconds;
            string next = RunSession.Run.Available().First().Id;

            // Quit and come back: nothing of the Run is left but the save.
            RunSession.Clear();
            Assert.IsFalse(RunSession.HasRun);
            EventCenter.Publish(new PlayRequested(RunSave.Slot));   // what Continue does before the Planet loads
            SceneManager.LoadScene(RunSession.PlanetScene);
            yield return PlanetShows("the Planet map shows again");

            PlanetRun run = RunSession.Run;
            Assert.AreEqual(seed, RunSession.Seed, "the same Planet");
            CollectionAssert.AreEqual(new[] { first.Id }, run.CapturedInOrder, "the captured Regions");
            Assert.AreEqual(first.BonusStone, run.BonusStone, "the bonus Stone");
            Assert.AreEqual(seconds, run.RunSeconds, 0.01f, "the Run clock");
            Assert.AreEqual(PlanetRunState.Choosing, run.State);
            Assert.AreEqual(AnchorKind.WarThrone, RunSession.Anchor, "the Anchor");
            Assert.AreEqual(SoulTuning.StartingLevel + 1, SoulRun.Ledger.Level, "the soul level");
            Assert.AreEqual(2, SoulRun.Ledger.Deaths, "the deaths");
            Assert.IsTrue(run.Available().Any(r => r.Id == next), "the same Regions are open");
            CollectionAssert.AreEqual(new[] { first.BuffId }.Where(b => !string.IsNullOrEmpty(b)), run.Buffs, "the buffs follow the captures");

            Assert.IsTrue(RunSession.GoToRegion(run.Available().First()));
            yield return TestWait.Until(() => CharacterInventory.Current != null && CharacterInventory.Current.Rules != null, "the next Region starts", Timeout);
            CharacterInventory inventory = CharacterInventory.Current;
            Assert.IsTrue(inventory.Storage.Items.Concat(inventory.Active.Items).Any(e => e.Item.Definition.Id == "heart_charm"), "the Items");
        }

        [UnityTest]
        public IEnumerator A_Region_never_saves_and_a_lost_Run_deletes_the_save()
        {
            yield return SaveARun(_ => { });
            Assert.IsTrue(RunSave.Exists);

            RunSave.Delete();
            Assert.IsTrue(RunSession.GoToRegion(RunSession.Run.Available().First()));
            yield return RegionPopulated();
            Assert.IsNull(RunSave.Capture(), "mid-Region there is nothing to save");
            Assert.IsFalse(RunSave.Save());
            Assert.IsFalse(RunSave.Exists, "nothing is saved mid-Region");

            RunSession.Run.CaptureCurrent();
            SceneManager.LoadScene(RunSession.PlanetScene);   // back on the map after the Region
            yield return PlanetShows("the Planet map shows");
            Assert.IsTrue(RunSave.Exists, "the Planet screen saves again");
            RunSession.End(won: false);
            Assert.IsFalse(RunSave.Exists, "a lost Run can't be continued");
        }

        [UnityTest]
        public IEnumerator The_main_menu_offers_Continue_only_when_a_Run_is_saved()
        {
            yield return BootToMenu();
            CollectionAssert.AreEqual(new[] { GameMenus.NewRunText, GameMenus.TutorialText, "Settings", GameMenus.CreditsText, "Quit" }.Select(Normal),
                MainMenuButtons().Select(Normal));

            RunSession.StartNew(7, new[] { 2, 1 });
            Assert.IsTrue(RunSave.Save());
            ((MainMenuPresenter)MenuStack.Top).Refresh();
            Assert.AreEqual("continue", Normal(MainMenuButtons()[0]), "Continue comes first when a Run is saved");
        }

        [UnityTest]
        public IEnumerator Continue_from_the_main_menu_opens_the_Planet_screen_with_the_saved_Run()
        {
            yield return SaveARun(_ => { });
            string captured = RunSession.Run.CapturedInOrder.Single();
            RunSession.Clear();
            yield return BootToMenu();

            PressMain("Continue");
            yield return PlanetShows("Continue opens the Planet screen");
            Assert.AreEqual(captured, RunSession.Run.CapturedInOrder.Single());
            Assert.AreEqual(AnchorKind.WarThrone, RunSession.Anchor);
        }

        [UnityTest]
        public IEnumerator New_Run_starts_a_fresh_Run_and_forgets_the_save()
        {
            yield return SaveARun(_ => { });
            PlanetRun old = RunSession.Run;
            yield return BootToMenu();

            PressMain(GameMenus.NewRunText);
            yield return PlanetShows("New Run opens the Planet screen");
            Assert.AreNotSame(old, RunSession.Run);
            Assert.AreEqual(0, RunSession.Run.CapturedInOrder.Count, "a fresh Run");
            Assert.AreEqual(0, RunSave.Capture().captured.Length, "and its own save");
        }

        [UnityTest]
        public IEnumerator Tutorial_and_Credits_go_through_hooks()
        {
            yield return BootToMenu();
            int tutorials = 0;
            MenuHooks.StartTutorial = () => tutorials++;
            PressMain(GameMenus.TutorialText);
            Assert.AreEqual(1, tutorials, "Tutorial runs its hook (ticket #30 replaces it)");

            PressMain(GameMenus.CreditsText);
            Assert.IsTrue(MenuStack.Contains("GameCredits"), "Credits shows its placeholder screen (ticket #34 replaces it)");
            MenuStack.CloseTop();
            Assert.IsFalse(MenuStack.Contains("GameCredits"));
        }

        [UnityTest]
        public IEnumerator Pause_menu_pauses_and_resumes_a_Region()
        {
            yield return InRegion();
            Assert.AreEqual(1f, Time.timeScale);
            GameSetup.Pause();
            Assert.IsTrue(GameSetup.IsPaused);
            Assert.AreEqual(0f, Time.timeScale, "the game stops");
            Assert.IsInstanceOf<PauseMenuPresenter>(MenuStack.Top);

            yield return null;
            MenuPanel view = MenuViews.Of(MenuStack.Top);
            string[] buttons = view.Query<Button>().ToList().Select(b => b.text).ToArray();
            CollectionAssert.AreEqual(new[] { "Resume", "Settings", GameMenus.AbandonText, GameMenus.QuitToMenuText }.Select(Normal), buttons.Select(Normal));

            float seconds = RunSession.Run.RunSeconds;
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.AreEqual(seconds, RunSession.Run.RunSeconds, "the Run clock stands still while paused");

            ((PauseMenuPresenter)MenuStack.Top).Press(0);   // Resume
            Assert.IsFalse(GameSetup.IsPaused);
            Assert.AreEqual(1f, Time.timeScale, "the game runs again");
            Assert.IsNull(MenuStack.Top, "the pause menu closed");
        }

        [UnityTest]
        public IEnumerator Abandon_Run_ends_the_Run_as_lost_and_goes_to_the_main_menu()
        {
            yield return InRegion();
            PlanetRun run = RunSession.Run;
            GameSetup.Pause();
            yield return null;

            GameMenus.AskAbandonRun();
            var confirm = (ConfirmPresenter)MenuStack.Top;
            yield return TestWait.Until(() => !confirm.IsInputBlocked, "the question takes answers", 5f);
            Assert.IsTrue(confirm.Answer(false), "Keep playing");
            Assert.IsFalse(run.IsOver, "No keeps the Run");
            Assert.IsTrue(GameSetup.IsPaused);

            GameMenus.AskAbandonRun();
            confirm = (ConfirmPresenter)MenuStack.Top;
            Assert.IsFalse(run.IsOver, "asking does not end the Run");
            yield return TestWait.Until(() => !confirm.IsInputBlocked, "the question takes answers", 5f);
            Assert.IsTrue(confirm.Answer(true));
            Assert.AreEqual(PlanetRunState.Lost, run.State, "the Run is lost");
            Assert.IsFalse(RunSave.Exists, "and its save is gone");
            yield return TestWait.Until(() => GameSetup.State == GameSetupState.MainMenu && ActiveScene == GameSetupSettings.Current.MainMenuScene, "back at the main menu", Timeout);
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsFalse(MainMenuButtons().Any(b => Normal(b) == "continue"), "a lost Run can't be continued");
        }

        [UnityTest]
        public IEnumerator Quit_to_menu_keeps_the_saved_Run_for_Continue()
        {
            yield return InRegion();
            Assert.IsTrue(RunSave.Exists, "the Planet screen saved the new Run");
            GameSetup.GoToMainMenu();
            yield return TestWait.Until(() => GameSetup.State == GameSetupState.MainMenu && ActiveScene == GameSetupSettings.Current.MainMenuScene, "back at the main menu", Timeout);
            Assert.IsTrue(RunSave.Exists, "leaving a Region does not delete the Run");
            Assert.AreEqual("continue", Normal(MainMenuButtons()[0]));
        }

        [UnityTest]
        public IEnumerator Settings_has_audio_video_controls_and_outline_colours_that_apply_at_once()
        {
            yield return BootToMenu();
            PressMain("Settings");
            var settings = (SettingsPresenter)MenuStack.Top;
            CollectionAssert.AreEqual(new[] { SettingsTab.Audio, SettingsTab.Graphics, SettingsTab.Accessibility, SettingsTab.Controls }, settings.Tabs);

            SelectTab(settings, SettingsTab.Audio);
            int[] sliders = Enumerable.Range(0, settings.Rows.Count).Where(i => settings.Rows[i].Kind == SettingsRowKind.Slider).ToArray();
            Assert.GreaterOrEqual(sliders.Length, 3, "master, music and SFX volume sliders");
            settings.SetValue(sliders[1], 0.25f);
            Assert.AreEqual(0.25f, GameSettings.Audio.MusicVolume, 0.001f, "volumes apply at once");
            settings.SetValue(sliders[2], 0.4f);
            Assert.AreEqual(0.4f, GameSettings.Audio.SfxVolume, 0.001f, "the SFX volume is kept");

            SelectTab(settings, SettingsTab.Graphics);
            Assert.IsTrue(settings.Rows.Any(r => r.Kind == SettingsRowKind.Choice), "window mode and resolution rows");
            Assert.IsTrue(settings.Rows.Any(r => r.Kind == SettingsRowKind.Toggle), "vsync");

            SelectTab(settings, SettingsTab.Accessibility);
            int preset = settings.Rows.ToList().FindIndex(r => r.Label == "Colour preset");
            Assert.GreaterOrEqual(preset, 0, "the outline preset row");
            settings.PickChoice(preset, (int)OutlinePreset.Deuteranopia);
            Assert.AreEqual(OutlinePreset.Deuteranopia, SideOutlineSettingsStore.Preset, "presets apply at once");
            int mortals = settings.Rows.ToList().FindIndex(r => r.Label == "Mortals outline");
            Assert.GreaterOrEqual(mortals, 0, "a colour row per side");
            settings.PickChoice(mortals, 3);
            Assert.IsTrue(SideOutlineSettingsStore.TryGetOverride(Side.Mortals, out _), "a side colour applies at once");
            settings.PickChoice(mortals, 0);
            Assert.IsFalse(SideOutlineSettingsStore.TryGetOverride(Side.Mortals, out _), "back to the preset colour");

            SelectTab(settings, SettingsTab.Controls);
            Assert.IsTrue(settings.Rows.Any(r => r.Label == "Pause"), "the game's keys are listed");
            Assert.IsTrue(settings.Rows.Any(r => r.Label == "Map"));
        }

        [UnityTest]
        public IEnumerator Settings_open_from_the_pause_menu()
        {
            yield return InRegion();
            GameSetup.Pause();
            yield return null;
            ((PauseMenuPresenter)MenuStack.Top).Press(1);   // Settings
            Assert.IsInstanceOf<SettingsPresenter>(MenuStack.Top);
            MenuStack.CloseTop();
            Assert.IsInstanceOf<PauseMenuPresenter>(MenuStack.Top, "back to the pause menu");
        }

        private static void SelectTab(SettingsPresenter settings, SettingsTab tab)
        {
            settings.ShowTab(tab);
            Assert.AreEqual(tab, settings.CurrentTab);
        }
    }
}
