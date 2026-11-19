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
            yield return WaitFor(() => Object.FindAnyObjectByType<PlanetScreen>()?.Map != null, "the Planet map shows");
            PlanetRun run = RunSession.Run;
            Assert.AreEqual(6, run.Available().Count(), "land on any edge Region");

            RegionInfo first = run.Available().First();
            Assert.IsTrue(RunSession.GoToRegion(first));
            yield return InRegion(first);
            Assert.AreEqual(first.Depth, Object.FindAnyObjectByType<WaveDirector>().Depth);
            CharacterInventory.Current.Give(ItemDatabase.Find("heart_charm"));
            int stone = RegionEconomy.Current.Stockpile.Get(ResourceType.Stone);

            yield return CaptureRegion();
            yield return WaitFor(() => SceneManager.GetActiveScene().name == RunSession.PlanetScene, "back on the Planet map");
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
            yield return WaitFor(() => SceneManager.GetActiveScene().name == RunSession.PlanetScene, "back on the Planet map");

            Assert.IsTrue(RunSession.GoToRegion(run.Planet.Capital));
            yield return InRegion(run.Planet.Capital);
            yield return CaptureRegion();
            yield return null;
            Assert.AreEqual(PlanetRunState.Won, run.State);
            Assert.IsFalse(RunManager.HasActiveRun, "Blondie's Run ended too");
        }

        [UnityTest]
        public IEnumerator The_Planet_map_opens_over_the_Region_and_zooming_out_never_leaves_the_Region()
        {
            SceneManager.LoadScene(RunSession.RegionScene);
            yield return WaitFor(() => Object.FindAnyObjectByType<PlanetOverlay>() != null, "the Region loads");
            yield return null;
            var overlay = Object.FindAnyObjectByType<PlanetOverlay>();
            var zoom = Object.FindAnyObjectByType<ScrollZoom>();
            for (int i = 0; i < 10; i++) zoom.Zoom(-1);
            Assert.IsFalse(overlay.IsOpen, "zooming out stays on the Region");
            Assert.AreEqual(4, zoom.Levels.PixelDivisor, "the farthest view: 8 pixels per unit");
            overlay.Open();
            Assert.IsTrue(overlay.IsOpen);
            Assert.IsTrue(zoom.Blocked, "the wheel no longer zooms the Region");
            overlay.Close();
            Assert.IsFalse(zoom.Blocked);
        }

        private static IEnumerator InRegion(RegionInfo region) =>
            WaitFor(() => Object.FindAnyObjectByType<RegionRun>()?.Region == region && CharacterInventory.Current != null && RegionEconomy.Current != null,
                $"in {region.Name}");

        // Every nest destroyed, the final Wave sent and killed. Drafts are off: they'd pause the game.
        private static IEnumerator CaptureRegion()
        {
            yield return null;
            ItemDrafts drafts = Object.FindAnyObjectByType<ItemDrafts>();
            if (drafts != null) drafts.enabled = false;
            var character = Object.FindAnyObjectByType<CharacterControl>();
            character.GetComponent<AutoCaster>().enabled = false;
            yield return null;
            foreach (EnemyNest nest in EnemyNest.All.ToArray()) nest.GetComponent<Damageable>().Unit.TakeDamage(100000f, DamageRules.TrueType);
            var waves = Object.FindAnyObjectByType<WaveDirector>();
            waves.Skip(1000f);
            yield return WaitFor(() => RegionGoal.Current.Capture.FinalWaveReleased && waves.PendingSpawns == 0, "every Wave has spawned");
            foreach (HordeMember m in Object.FindObjectsByType<HordeMember>(FindObjectsInactive.Exclude))
                if (m.IsAlive && !m.IsRoaming) m.Unit.TakeDamage(100000f, DamageRules.TrueType);
            yield return WaitFor(() => RegionGoal.Current.Capture.IsCaptured, "captured");
            yield return null;
        }

        private static IEnumerator WaitFor(Func<bool> condition, string what, float seconds = 15f)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (!condition())
            {
                if (Time.realtimeSinceStartup > end) Assert.Fail("Timed out: " + what);
                yield return null;
            }
        }
    }
}
