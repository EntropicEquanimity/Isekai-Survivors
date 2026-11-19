using Blondie.Core;
using Blondie.GameSetup;
using Blondie.Save;
using Blondie.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Isekai
{
    /// <summary>
    /// Sets Blondie Game Setup's front end up for this game when the game starts: the main menu (New Run, Continue,
    /// Tutorial, Settings, Credits, Quit; the items come from the Game Setup settings asset), the pause menu with Abandon
    /// Run, one save slot for the Run, the game's colours, and the settings rows of its own (<see cref="GameSettingsRows"/>).
    /// Continue loads the Run saved on the Planet screen (<see cref="RunSave"/>) and opens the Planet screen.
    /// </summary>
    public static class GameMenus
    {
        public const string NewRunText = "New Run";
        public const string TutorialText = "Tutorial";
        public const string CreditsText = "Credits";
        public const string AbandonText = "Abandon Run";
        public const string QuitToMenuText = "Quit to menu";

        private const string StyleSheetPath = "IsekaiMenus";
        private static bool s_listening;

        // After StaticReset (SubsystemRegistration) cleared the events, rows and buttons Game Setup keeps.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            SaveSystem.MaxSlots = 1;   // one Run at a time
            MainMenuButtons.Add(NewRunText, GameSetup.Play, MainMenuItem.Continue);
            MainMenuButtons.Add(TutorialText, () => MenuHooks.StartTutorial?.Invoke(), MainMenuItem.Continue);
            MainMenuButtons.Add(CreditsText, () => MenuHooks.OpenCredits?.Invoke(), MainMenuItem.Settings);
            MenuViews.Register<PauseMenuPresenter>(menu => new GamePauseScreen(menu));
            EventCenter.Subscribe<PlayRequested>(OnPlayRequested);
            GameSettingsRows.Register();
            if (!s_listening)
            {
                s_listening = true;
                SceneManager.sceneLoaded += (_, _) => ApplyStyle();
            }
            ApplyStyle();
        }

        /// <summary>Puts the game's colours on the UI (the stylesheet is added to the UI Root once it exists).</summary>
        public static void ApplyStyle()
        {
            if (!UIRoot.HasInstance) return;
            StyleSheet sheet = Resources.Load<StyleSheet>(StyleSheetPath);
            if (sheet != null) UIRoot.Instance.AddStyleSheet(sheet);
        }

        // New Run forgets the Run that was in progress and its save; Continue brings the saved Run back before the Planet
        // screen starts, so the screen finds it.
        private static void OnPlayRequested(PlayRequested request)
        {
            if (request.Slot < 0)
            {
                RunSession.Clear();
                RunSave.Delete();
                return;
            }
            if (SaveSystem.Load(request.Slot)) RunSave.Restore();
        }

        /// <summary>Asks whether to give the Run up; on yes it ends as lost and the game goes to the main menu.</summary>
        public static void AskAbandonRun() =>
            ConfirmPresenter.Ask(AbandonText + "?", "The Run ends as lost and its save is deleted.", AbandonRun, yesKey: AbandonText, noKey: "Keep playing");

        /// <summary>Ends the Run as lost and goes to the main menu.</summary>
        public static void AbandonRun()
        {
            RunSession.Abandon();
            GameSetup.GoToMainMenu();
        }
    }
}
