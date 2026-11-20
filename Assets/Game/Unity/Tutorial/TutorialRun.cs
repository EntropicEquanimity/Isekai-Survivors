using System.Linq;
using Blondie.Core;
using Blondie.GameSetup;
using Blondie.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Isekai
{
    /// <summary>
    /// Starts and tracks the guided first Region tutorial run: fixed seed, fixed small recipe, Shrine preset, and direct
    /// landing in the Region.
    /// </summary>
    public static class TutorialRun
    {
        private const string OfferMenuName = "TutorialOffer";
        private const int TutorialSeed = 20300030;
        private const int TutorialWidth = 96;
        private const int TutorialHeight = 72;
        private static bool s_enteredRegion;

        static TutorialRun()
        {
            StaticReset.Register(() =>
            {
                IsActive = false;
                IsResolved = false;
                s_enteredRegion = false;
            });
        }

        public static bool IsActive { get; private set; }
        public static bool IsResolved { get; private set; }
        public static bool CanSkip => IsActive && !IsResolved;

        /// <summary>The fixed small recipe used by the tutorial Region.</summary>
        public static RegionRecipe Recipe => new(TutorialSeed, TutorialWidth, TutorialHeight);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            MenuHooks.StartTutorial = StartFromMenu;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        /// <summary>Main menu New Run entrypoint: offer the tutorial once, before the first Run.</summary>
        public static void StartNewRunFromMenu()
        {
            if (!TutorialProgressStore.NeedsOffer)
            {
                GameSetup.Play();
                return;
            }

            if (MenuStack.Contains(OfferMenuName)) return;
            var panel = new MenuPanel(OfferMenuName, "Guided first Region");
            panel.Body.Add(GameStyle.Label("Play the guided first Region now?", TextRole.Body));
            panel.Body.Add(GameStyle.Label("You can skip it at any time and replay it from Tutorial on the main menu.", TextRole.Muted));
            panel.Footer.Add(new MenuButton("Play tutorial", () =>
            {
                panel.Close();
                StartFromMenu();
            }, primary: true));
            panel.Footer.Add(new MenuButton("Skip", () =>
            {
                panel.Close();
                TutorialProgressStore.MarkSkipped();
                GameSetup.Play();
            }));
            panel.Open();
        }

        /// <summary>Main menu Tutorial button: starts a fresh tutorial run and lands in the tutorial Region.</summary>
        public static void StartFromMenu()
        {
            RunSave.Delete();
            RunSession.StartNew(TutorialSeed, new[] { 2, 1 });
            RunSession.Anchor = AnchorKind.Shrine;
            RegionRecipes.Pending = Recipe;
            IsActive = true;
            IsResolved = false;
            s_enteredRegion = false;
            RunSession.GoToRegion(RunSession.Run.Available().First());
        }

        /// <summary>Marks this tutorial run as skipped and turns tutorial guidance off in this Region.</summary>
        public static bool Skip()
        {
            if (!CanSkip) return false;
            TutorialProgressStore.MarkSkipped();
            IsResolved = true;
            IsActive = false;
            s_enteredRegion = false;
            return true;
        }

        /// <summary>Marks this tutorial run as finished and turns tutorial guidance off in this Region.</summary>
        public static bool Complete()
        {
            if (!CanSkip) return false;
            TutorialProgressStore.MarkFinished();
            IsResolved = true;
            IsActive = false;
            s_enteredRegion = false;
            return true;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!IsActive) return;
            if (scene.name == RunSession.RegionScene)
            {
                s_enteredRegion = true;
                return;
            }
            if (!s_enteredRegion) return;
            if (!IsResolved) IsActive = false;
            s_enteredRegion = false;
        }

        internal static void ResetForTests()
        {
            IsActive = false;
            IsResolved = false;
            s_enteredRegion = false;
        }
    }
}
