using System;
using System.Collections;
using System.Linq;
using Blondie.GridInventory;
using Blondie.SkillTrees;
using Blondie.Stats;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Isekai.Tests
{
    /// <summary>Phase 8: Shards from Runs and the Legacy tree's upgrades in the real scenes.</summary>
    public class MetaPlayModeTests
    {
        [UnitySetUp]
        public IEnumerator Clean()
        {
            RunSession.Clear();
            MetaProgress.Wipe();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            RunSession.Clear();
            MetaProgress.Wipe();
            Time.timeScale = 1f;
            yield return null;
        }

        [Test]
        public void Ending_a_Run_pays_Shards_once()
        {
            PlanetRun run = RunSession.StartNew(5, new[] { 3, 2 });
            run.Enter(run.Available().First());
            run.CaptureCurrent();
            run.Enter(run.Available().First(r => r.Depth == 1));

            RunSession.End(won: false);
            Assert.AreEqual(10, MetaProgress.Shards, "one edge Region captured");
            Assert.AreEqual(10, MetaProgress.LastEarned);
            RunSession.End(won: false);
            Assert.AreEqual(10, MetaProgress.Shards, "an ended Run pays only once");
        }

        [Test]
        public void Shards_and_upgrades_are_saved()
        {
            MetaProgress.Earn(50);
            Assert.AreEqual(SpendFailReason.None, MetaProgress.Unit.Spend(MetaProgress.Tree, "supplies"));
            string saved = MetaProgress.Store.GetString(MetaProgress.SaveKey);
            StringAssert.Contains("supplies", saved);
            StringAssert.Contains("\"shared\":40", saved);
        }

        [UnityTest]
        public IEnumerator Legacy_upgrades_shape_the_next_Region()
        {
            yield return LoadRegion();
            int stone = RegionEconomy.Current.Stockpile.Get(ResourceType.Stone);
            float health = Character().GetComponent<StatBlock>().GetValue(nameof(StatId.HealthMax));
            CloningBay bay = Object.FindAnyObjectByType<CloningPods>().Bay;
            int pods = bay.Pods;
            int satchels = Count(CharacterInventory.Current.Storage, "satchel");

            MetaProgress.Earn(500);
            foreach (string node in new[] { "pods", "rebuild", "supplies", "supplies", "satchel", "hardy" })
                Assert.AreEqual(SpendFailReason.None, MetaProgress.Unit.Spend(MetaProgress.Tree, node), node);
            RunSession.Clear();
            yield return LoadRegion();

            Assert.AreEqual(stone + 30, RegionEconomy.Current.Stockpile.Get(ResourceType.Stone), "Supply Cache x2");
            Assert.AreEqual(health + 10f, Character().GetComponent<StatBlock>().GetValue(nameof(StatId.HealthMax)), 1e-3f, "Hardy");
            bay = Object.FindAnyObjectByType<CloningPods>().Bay;
            Assert.AreEqual(pods + 1, bay.Pods, "Spare Pod");
            Assert.AreEqual(LegacyTree.PodRebuildSeconds, bay.RebuildSeconds, "Pod Rebuilding");
            Assert.AreEqual(satchels + 1, Count(CharacterInventory.Current.Storage, "satchel"), "Packed Satchel");
        }

        [UnityTest]
        public IEnumerator The_Planet_screen_opens_the_Legacy_tree_only_between_Runs()
        {
            MetaProgress.Earn(20);
            SceneManager.LoadScene(RunSession.PlanetScene);
            yield return TestWait.Until(() => Object.FindAnyObjectByType<PlanetScreen>()?.Map != null, "the Planet map shows");
            var screen = Object.FindAnyObjectByType<PlanetScreen>();
            Assert.IsTrue(PlanetScreen.CanSpendShards(RunSession.Run), "before landing");

            screen.OpenLegacy();
            yield return null;
            Assert.IsTrue(screen.LegacyWindow.IsOpen);
            Assert.AreEqual(SpendFailReason.None, screen.LegacyWindow.Presenter.Learn("supplies"));
            Assert.AreEqual(1, MetaProgress.Rank("supplies"));
            Assert.AreEqual(10, MetaProgress.Shards);
            screen.LegacyWindow.Close();
            yield return null;

            RunSession.Run.Enter(RunSession.Run.Available().First());
            Assert.IsFalse(PlanetScreen.CanSpendShards(RunSession.Run), "not mid-Run");
        }

        private static IEnumerator LoadRegion()
        {
            SceneManager.LoadScene(RunSession.RegionScene);
            yield return null;
            yield return null;
        }

        private static CharacterControl Character() => Object.FindAnyObjectByType<CharacterControl>();

        private static int Count(ItemGrid grid, string id) => grid.Items.Count(e => e.Item.Definition.Id == id);

    }
}
