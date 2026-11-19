using System;
using System.Collections;
using System.Linq;
using Blondie.Abilities;
using Blondie.Combat;
using Blondie.Inventory;
using Blondie.Roguelike;
using Blondie.Roguelike.Boons;
using Blondie.Roguelike.Drafts;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Isekai.Tests
{
    /// <summary>Phase 7: a Run across the Planet, through the real Planet and Region scenes.</summary>
    public class RunFlowTests
    {
        private const float Timeout = 15f;

        [UnitySetUp]
        public IEnumerator Clean()
        {
            RunSession.Clear();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            RunDrafts.ClearQueue();
            Time.timeScale = 1f;
            RunSession.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator Land_capture_and_return_to_the_Planet_with_the_Runs_rewards()
        {
            SceneManager.LoadScene(RunSession.PlanetScene);
            yield return TestWait.Until(() => Object.FindAnyObjectByType<PlanetScreen>()?.Map != null, "the Planet map shows", Timeout);
            PlanetRun run = RunSession.Run;
            Assert.AreEqual(6, run.Available().Count(), "land on any edge Region");

            RegionInfo first = run.Available().First();
            Assert.IsTrue(RunSession.GoToRegion(first));
            yield return InRegion(first);
            CharacterInventory.Current.Give(ItemDatabase.Find("heart_charm"));
            int stone = RegionEconomy.Current.Stockpile.Get(ResourceType.Stone);

            yield return CaptureRegion();
            yield return TestWait.Until(() => SceneManager.GetActiveScene().name == RunSession.PlanetScene, "back on the Planet map", Timeout);
            Assert.IsTrue(run.IsCaptured(first));
            Assert.AreEqual(PlanetRunState.Choosing, run.State);
            CollectionAssert.Contains(run.Buffs, first.BuffId);
            Assert.IsTrue(RunBoons.Has(RegionBuffs.Get(first.BuffId)), "the Region's buff is on the Run");
            Assert.Greater(run.RunSeconds, 0f);

            RegionInfo next = run.Available().First();
            Assert.IsTrue(first.Neighbours.Contains(next.Id), "next door to a captured Region");
            RunSession.GoToRegion(next);
            yield return InRegion(next);
            Assert.IsTrue(CharacterInventory.Current.Storage.Items.Any(e => e.Item.Definition.Id == "heart_charm")
                          || CharacterInventory.Current.Active.Items.Any(e => e.Item.Definition.Id == "heart_charm"), "Run Items carry over");
            Assert.AreEqual(stone + first.BonusStone, RegionEconomy.Current.Stockpile.Get(ResourceType.Stone), "captures add starting Stone");
        }

        [UnityTest]
        public IEnumerator Capturing_the_Capital_wins_the_Run()
        {
            RunSession.StartNew(5, new[] { 1 });   // one edge Region next to the Capital
            PlanetRun run = RunSession.Run;
            RunSession.GoToRegion(run.Available().Single());
            yield return InRegion(run.Current);
            yield return CaptureRegion();
            yield return TestWait.Until(() => SceneManager.GetActiveScene().name == RunSession.PlanetScene, "back on the Planet map", Timeout);

            Assert.IsTrue(RunSession.GoToRegion(run.Planet.Capital));
            yield return InRegion(run.Planet.Capital);
            yield return CaptureRegion();
            yield return null;
            Assert.AreEqual(PlanetRunState.Won, run.State);
            Assert.IsFalse(RunManager.HasActiveRun, "Blondie's Run ended too");
        }

        [UnityTest]
        public IEnumerator Zooming_out_past_the_widest_view_opens_the_map_screen_on_the_Region()
        {
            RunSession.StartNew(3);
            SceneManager.LoadScene(RunSession.RegionScene);
            yield return TestWait.Until(() => Object.FindAnyObjectByType<MapScreen>() != null, "the Region loads", Timeout);
            yield return null;
            yield return null;
            var map = Object.FindAnyObjectByType<MapScreen>();
            var zoom = Object.FindAnyObjectByType<ScrollZoom>();
            for (int i = 0; i < 10 && !map.IsOpen; i++) zoom.Zoom(-1);
            Assert.IsTrue(map.IsOpen);
            Assert.AreEqual(MapScreen.Tab.Region, map.Current);
            Assert.IsTrue(zoom.Blocked, "the wheel no longer zooms the Region");

            var character = Object.FindAnyObjectByType<CharacterControl>();
            Color32 you = map.RegionMap.PixelAt(character.transform.position);
            Assert.AreEqual(RegionMapView.You, you, "the Character is a white square on the map");

            map.Show(MapScreen.Tab.Planet);
            Assert.AreEqual(MapScreen.Tab.Planet, map.Current);
            map.Close();
            Assert.IsFalse(zoom.Blocked);
            RunSession.Clear();
        }

        private static IEnumerator InRegion(RegionInfo region) =>
            TestWait.Until(() => Object.FindAnyObjectByType<RegionRun>()?.Region == region && CharacterInventory.Current != null && RegionEconomy.Current != null,
                $"in {region.Name}", Timeout);

        // The debug entry that stands in for destroying the Seat. Drafts are off: they'd pause the game.
        private static IEnumerator CaptureRegion()
        {
            yield return null;
            RegionTestScene.DisableDrafts();
            TestDebug.Press("Capture Region");
            yield return TestWait.Until(() => RegionEvents.HasEnded && RegionEvents.Outcome == RegionEnd.Captured, "captured", Timeout);
            yield return null;
        }

    }
}
