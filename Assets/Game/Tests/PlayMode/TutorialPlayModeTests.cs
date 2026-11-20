using System.Collections;
using System.Linq;
using Blondie.Combat;
using Blondie.Core;
using Blondie.GameSetup;
using Blondie.Juice;
using Blondie.Pooling;
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
    public class TutorialPlayModeTests
    {
        private const float Timeout = 25f;
        private IKeyValueStore _previousStore;
        private ISettingsApplier _previousApplier;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _previousStore = KeyValueStorage.Current;
            KeyValueStorage.Current = new MemoryKeyValueStore();
            _previousApplier = GameSettings.Applier;
            GameSettings.Applier = null;
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
            RunSession.Clear();
            RunSave.Delete();
            TutorialRun.ResetForTests();
            TutorialProgressStore.ResetForTests();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            GameSetup.Resume();
            MenuStack.CloseAll();
            foreach (MonoBehaviour b in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
                if (b != null && b.GetType().Name == "GameSetupRunner") Object.Destroy(b.gameObject);
            if (UIRoot.HasInstance) Object.Destroy(UIRoot.Instance.gameObject);
            yield return null;
            PackageSettings<GameSetupSettings>.Set(null);
            PackageSettings<GameSetupUnitySettings>.Set(null);
            KeyValueStorage.Current = _previousStore;
            GameSettings.Applier = _previousApplier;
            TutorialRun.ResetForTests();
            TutorialProgressStore.ResetForTests();
            RunSave.Delete();
            RunSession.Clear();
            GameSetup.SetState(GameSetupState.Gameplay);
        }

        [UnityTest]
        public IEnumerator First_New_Run_offers_Play_tutorial_or_Skip()
        {
            yield return BootToMenu();
            PressMain(GameMenus.NewRunText);
            Assert.IsTrue(MenuStack.Contains("TutorialOffer"));
        }

        [UnityTest]
        public IEnumerator Tutorial_button_starts_the_guided_Region()
        {
            yield return BootToMenu();
            PressMain(GameMenus.TutorialText);
            yield return RegionReady("Tutorial opens the Region");
            Assert.IsTrue(TutorialRun.IsActive);
            Assert.AreEqual(AnchorKind.Shrine, RunSession.Anchor);
            RegionRecipe recipe = RegionBootstrap.Current.Context.Recipe;
            Assert.AreEqual(TutorialRun.Recipe.Width, recipe.Width);
            Assert.AreEqual(TutorialRun.Recipe.Height, recipe.Height);
        }

        [UnityTest]
        public IEnumerator Skipping_the_tutorial_ends_it_and_remembers_it()
        {
            TutorialRun.StartFromMenu();
            yield return RegionReady("the tutorial Region loads");
            TestDebug.Press("Skip tutorial");
            yield return TestWait.Until(() => !TutorialRun.IsActive, "tutorial guidance stops", 5f);
            Assert.AreEqual(TutorialState.Skipped, TutorialProgressStore.State);
        }

        [UnityTest]
        public IEnumerator Pause_menu_shows_a_Skip_tutorial_entry_while_the_tutorial_is_active()
        {
            TutorialRun.StartFromMenu();
            yield return RegionReady("the tutorial Region loads");
            GameSetup.Pause();
            yield return null;
            MenuPanel view = MenuViews.Of(MenuStack.Top);
            string[] buttons = view.Query<Button>().ToList().Select(b => b.text).ToArray();
            Assert.IsTrue(buttons.Any(b => Normal(b) == "skip tutorial"));
        }

        [UnityTest]
        public IEnumerator Tutorial_advances_through_every_step_from_gameplay_actions()
        {
            TutorialRun.StartFromMenu();
            yield return RegionReady("the tutorial Region loads");
            RegionTestScene.ClearHorde();
            EconomyDebug.AddResources(1000);

            RegionTutorial tutorial = Object.FindAnyObjectByType<RegionTutorial>();
            Assert.IsNotNull(tutorial);
            Assert.AreEqual(TutorialStepId.MoveAndFight, tutorial.CurrentStep);

            CharacterControl jack = Player.Character;
            jack.Teleport(jack.transform.position + new Vector3(7f, 0f, 0f));
            HordeMember mortal = Pools.Spawn(Object.FindAnyObjectByType<HordeSpawner>().Prefab, jack.transform.position + new Vector3(5f, 0f, 0f), Quaternion.identity).GetComponent<HordeMember>();
            yield return null;
            mortal.Unit.TakeDamage(99999f, DamageRules.PhysicalType, jack.GetComponent<Damageable>().Unit);
            yield return WaitForAtLeastStep(tutorial, TutorialStepId.MineStoneAndWood, "move and fight step");

            foreach (ResourceNode stone in ResourceNode.All.Where(n => n.Type == ResourceType.Stone && !n.IsEmpty).Take(3)) stone.Take(1);
            foreach (ResourceNode wood in ResourceNode.All.Where(n => n.Type == ResourceType.Wood && !n.IsEmpty).Take(3)) wood.Take(1);
            yield return null;
            Assert.IsTrue(tutorial.HasFact(TutorialFact.MinedStone), "tutorial saw Stone mining");
            Assert.IsTrue(tutorial.HasFact(TutorialFact.MinedWood), "tutorial saw Wood mining");
            Assert.GreaterOrEqual(tutorial.StepIndex, StepIndexOf(TutorialStepId.BuildPortalOnCorruption), "mining advances to the portal build step");

            BuildMode mode = Object.FindAnyObjectByType<BuildMode>();
            RegionBuildings buildings = RegionBuildings.Current;
            Building anchor = Building.All.First(b => b.IsMainBase);
            BuildingAsset portalAsset = mode.Catalogue.First(a => a.DisplayName == "Portal");
            UnitProducer portal = PlacePortal(buildings, anchor, portalAsset).GetComponent<UnitProducer>();
            yield return null;
            Assert.IsTrue(tutorial.HasFact(TutorialFact.PortalBuilt), "tutorial saw Portal placement");
            yield return WaitForAtLeastStep(tutorial, TutorialStepId.SetPortalOrderAndRally, "portal build step");

            portal.Order = PortalOrder.Follow;
            portal.SetRally(portal.transform.position + new Vector3(2f, 0f, 0f));
            foreach (Building b in Building.All)
            {
                if (b == null || !b.IsPlayers || !b.TryGetComponent(out UnitProducer p)) continue;
                p.Order = PortalOrder.Follow;
                p.SetRally(p.transform.position + Vector3.right * 2f);
            }
            yield return null;
            Assert.IsTrue(tutorial.HasFact(TutorialFact.PortalOrderSet), "tutorial saw portal order");
            Assert.IsTrue(tutorial.HasFact(TutorialFact.PortalRallySet), "tutorial saw rally point");
            Assert.GreaterOrEqual(tutorial.StepIndex, StepIndexOf(TutorialStepId.WatchCorruptionSpread), "order and rally advance to the corruption step");

            Int2 corruptionCell = RegionGrid.Current.CellAt(jack.transform.position);
            RegionCorruption.Current.Field.Set(corruptionCell, 0);
            RegionCorruption.Current.Field.Raise(corruptionCell);
            yield return WaitForAtLeastStep(tutorial, TutorialStepId.LearnVigilance, "corruption step");

            VigilanceDirector.Current.RaiseStage();
            yield return WaitForAtLeastStep(tutorial, TutorialStepId.StopWitnessRider, "vigilance step");

            RegionAlarms alarms = RegionAlarms.Current;
            AlarmRider rider = alarms.SendRider(jack.transform.position + new Vector3(10f, 0f, 0f));
            Assert.IsNotNull(rider);
            yield return TestWait.Until(() => alarms.RidersSent > 0, "a rider is sent", 5f);
            rider.Member.Unit.TakeDamage(99999f, DamageRules.PhysicalType, jack.GetComponent<Damageable>().Unit);
            yield return WaitForAtLeastStep(tutorial, TutorialStepId.RecoverFromDeath, "rider step");

            RegionSouls souls = RegionSouls.Current;
            jack.GetComponent<Damageable>().Unit.TakeDamage(99999f, DamageRules.TrueType);
            yield return TestWait.Until(() => souls.IsBodiless && souls.Wisp != null, "Jack becomes a wisp", 6f);
            Building liveAnchor = Building.All.First(b => b.IsMainBase && b.IsPlayers && b.IsAlive);
            souls.Wisp.transform.position = liveAnchor.transform.position;
            yield return TestWait.Until(() => !souls.IsBodiless, "the wisp returns to a body", 8f);
            yield return WaitForAtLeastStep(tutorial, TutorialStepId.DestroySeat, "wisp step");

            TestDebug.Press("Destroy the Seat");
            yield return TestWait.Until(() => TutorialProgressStore.State == TutorialState.Finished, "tutorial is remembered as finished", 10f);
            Assert.IsFalse(TutorialRun.IsActive);
        }

        private static Building PlacePortal(RegionBuildings buildings, Building anchor, BuildingAsset portal)
        {
            Int2 origin = anchor.Origin;
            foreach (int d in new[] { 4, -4, 6, -6, 8, -8 })
            foreach (int e in new[] { 0, 4, -4 })
            {
                var cell = new Int2(origin.x + d, origin.y + e);
                if (!buildings.Check(portal, cell).IsOk) continue;
                Building placed = buildings.Place(portal, cell);
                if (placed != null) return placed;
            }
            Assert.Fail("no free corrupted ground for a tutorial Portal");
            return null;
        }

        private static IEnumerator WaitForAtLeastStep(RegionTutorial tutorial, TutorialStepId step, string what)
        {
            int want = StepIndexOf(step);
            return TestWait.Until(() =>
                tutorial != null && (tutorial.CurrentStep == null || tutorial.StepIndex >= want), "tutorial reaches " + what, 8f);
        }

        private static int StepIndexOf(TutorialStepId step)
        {
            for (int i = 0; i < TutorialSteps.Count; i++)
                if (TutorialSteps.At(i).Id == step) return i;
            return int.MaxValue;
        }

        private static IEnumerator RegionReady(string what) =>
            TestWait.Until(() => RegionBootstrap.Current != null && RegionBootstrap.Current.IsPopulated, what, Timeout);

        private static IEnumerator BootToMenu()
        {
            SceneManager.LoadScene(GameSetupSettings.Current.BootScene);
            yield return TestWait.Until(() => GameSetup.State == GameSetupState.MainMenu && SceneManager.GetActiveScene().name == GameSetupSettings.Current.MainMenuScene, "Boot reaches the menu", Timeout);
            yield return null;
        }

        private static string Normal(string text) => text.ToLowerInvariant();

        private static void PressMain(string text)
        {
            var menu = (MainMenuPresenter)MenuStack.Top;
            int index = menu.Buttons.ToList().FindIndex(b => Normal(b.Text) == Normal(text));
            Assert.GreaterOrEqual(index, 0, $"the main menu has '{text}'");
            menu.Press(index);
        }
    }
}
