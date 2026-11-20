using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Isekai.Tests
{
    /// <summary>The Credits button on the main menu opens the credits screen listing every third-party asset.</summary>
    public class CreditsPlayModeTests
    {
        private const float Timeout = 20f;
        private IKeyValueStore _previousStore;
        private ISettingsApplier _previousApplier;
        private Action _previousCredits;
        private Action<string> _previousOpenUrl;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _previousStore = KeyValueStorage.Current;
            KeyValueStorage.Current = new MemoryKeyValueStore();
            _previousApplier = GameSettings.Applier;
            GameSettings.Applier = null;
            _previousCredits = MenuHooks.OpenCredits;
            MenuHooks.OpenCredits = CreditsScreen.Open;
            _previousOpenUrl = CreditsScreen.OpenUrl;
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
            PackageSettings<GameSetupUnitySettings>.Set(new GameSetupUnitySettings());
            SaveSystem.MaxSlots = 1;
            SaveSystem.NewGame();
            RunSave.Delete();
            RunSession.Clear();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            MenuStack.CloseAll();
            foreach (MonoBehaviour b in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
                if (b != null && b.GetType().Name == "GameSetupRunner") Object.Destroy(b.gameObject);   // internal to Game Setup
            if (UIRoot.HasInstance) Object.Destroy(UIRoot.Instance.gameObject);
            yield return null;
            PackageSettings<GameSetupSettings>.Set(null);
            PackageSettings<GameSetupUnitySettings>.Set(null);
            KeyValueStorage.Current = _previousStore;
            GameSettings.Applier = _previousApplier;
            MenuHooks.OpenCredits = _previousCredits;
            CreditsScreen.OpenUrl = _previousOpenUrl;
            RunSave.Delete();
            RunSession.Clear();
            GameSetup.SetState(GameSetupState.Gameplay);
        }

        private static IEnumerator BootToMenu()
        {
            SceneManager.LoadScene(GameSetupSettings.Current.BootScene);
            yield return TestWait.Until(() => GameSetup.State == GameSetupState.MainMenu
                && SceneManager.GetActiveScene().name == GameSetupSettings.Current.MainMenuScene, "Boot reaches the Menu scene", Timeout);
            yield return null;
        }

        private static void PressCredits()
        {
            var menu = (MainMenuPresenter)MenuStack.Top;
            int index = menu.Buttons.ToList().FindIndex(b => b.Text.ToLowerInvariant() == GameMenus.CreditsText.ToLowerInvariant());
            Assert.GreaterOrEqual(index, 0, "the main menu has a Credits button");
            menu.Press(index);
        }

        private static string TextOf(VisualElement row, string cssClass) => (row.Q(className: cssClass) as TextElement)?.text;

        [UnityTest]
        public IEnumerator The_Credits_button_opens_a_screen_where_every_entry_has_a_title_author_and_licence()
        {
            yield return BootToMenu();
            PressCredits();
            Assert.IsTrue(MenuStack.Contains(CreditsScreen.MenuName), "Credits opens the credits screen");

            List<VisualElement> rows = CreditsScreen.Current.Query<VisualElement>(className: CreditsScreen.EntryClass).ToList();
            Assert.AreEqual(CreditsList.Entries.Count, rows.Count, "one row per credited asset");
            for (int i = 0; i < rows.Count; i++)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(TextOf(rows[i], CreditsScreen.TitleClass)), $"row {i} has a title");
                Assert.IsFalse(string.IsNullOrWhiteSpace(TextOf(rows[i], CreditsScreen.AuthorClass)), $"row {i} has an author");
                Assert.IsFalse(string.IsNullOrWhiteSpace(TextOf(rows[i], CreditsScreen.LicenceClass)), $"row {i} has a licence");
            }

            MenuStack.CloseTop();
            Assert.IsFalse(MenuStack.Contains(CreditsScreen.MenuName), "Back closes it");
        }

        [UnityTest]
        public IEnumerator Clicking_a_title_with_a_link_opens_its_url()
        {
            var opened = new List<string>();
            CreditsScreen.OpenUrl = opened.Add;
            yield return BootToMenu();
            PressCredits();

            VisualElement row = CreditsScreen.Current.Query<VisualElement>(className: CreditsScreen.EntryClass)
                .ToList().First(r => r.Q<Button>(className: CreditsScreen.TitleClass) != null);
            Button link = row.Q<Button>(className: CreditsScreen.TitleClass);
            using (NavigationSubmitEvent click = NavigationSubmitEvent.GetPooled())
            {
                click.target = link;
                link.SendEvent(click);
            }
            Assert.AreEqual(1, opened.Count, "the click opens one link");
            Assert.IsTrue(CreditsScreen.IsWebLink(opened[0]));
        }
    }
}
