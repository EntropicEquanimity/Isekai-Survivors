using System.Linq;
using Blondie.SkillTrees;
using NUnit.Framework;

namespace Isekai.Tests
{
    public class MetaTests
    {
        [Test]
        public void Runs_pay_Shards_for_each_Region_and_more_for_deeper_ones_and_winning()
        {
            Assert.AreEqual(0, MetaRewards.ShardsFor(new int[0], won: false));
            Assert.AreEqual(10 + 10 + 15, MetaRewards.ShardsFor(new[] { 0, 0, 1 }, won: false));
            Assert.AreEqual(10 + 25 + 50, MetaRewards.ShardsFor(new[] { 0, 3 }, won: true));
        }

        [Test]
        public void A_finished_PlanetRun_pays_for_what_it_captured()
        {
            var run = new PlanetRun(Planet.Generate(3, new[] { 3, 2 }, null));
            run.Enter(run.Available().First());
            run.CaptureCurrent();
            run.Lose();
            Assert.AreEqual(10, MetaRewards.ShardsFor(run));
        }

        [Test]
        public void The_Legacy_tree_is_bought_with_Shards_and_grants_the_upgrades()
        {
            SkillTreeDefinition tree = LegacyTree.Create();
            var unit = new SkillTreeUnit(startingPoints: 110);
            unit.AddTree(tree);

            Assert.AreNotEqual(SpendFailReason.None, unit.CanSpend(tree, "satchel"), "needs the Supply Cache first");
            Assert.AreEqual(SpendFailReason.None, unit.Spend(tree, "supplies"));
            Assert.AreEqual(SpendFailReason.None, unit.Spend(tree, "supplies"));
            Assert.AreEqual(30, LegacyTree.StartingStone(unit));
            Assert.AreEqual(SpendFailReason.None, unit.Spend(tree, "satchel"));
            CollectionAssert.AreEqual(new[] { "satchel" }, LegacyTree.StartingItems(tree, unit));

            Assert.IsFalse(LegacyTree.RebuildsPods(unit));
            Assert.AreEqual(SpendFailReason.None, unit.Spend(tree, "pods"));
            Assert.AreEqual(1, LegacyTree.ExtraPods(unit));
            Assert.AreEqual(SpendFailReason.None, unit.Spend(tree, "rebuild"));
            Assert.IsTrue(LegacyTree.RebuildsPods(unit));
            Assert.AreEqual(110 - 10 - 10 - 25 - 20 - 40, unit.SharedPoints, "each rank costs its Shards");
            Assert.AreNotEqual(SpendFailReason.None, unit.Spend(tree, "hardy"), "5 Shards left: too few");
        }

        [Test]
        public void Used_Cloning_Pods_rebuild_one_at_a_time_when_upgraded()
        {
            var bay = new CloningBay(pods: 2, cloneSeconds: 1f, rebuildSeconds: 10f);
            Assert.IsTrue(bay.CharacterDied());
            Assert.IsTrue(bay.Tick(1f), "cloned");
            Assert.IsTrue(bay.CharacterDied());
            Assert.IsTrue(bay.Tick(1f));
            Assert.AreEqual(0, bay.Pods);
            bay.Tick(7.5f);
            Assert.AreEqual(0, bay.Pods, "9.5 s since the first pod was used");
            bay.Tick(1f);
            Assert.AreEqual(1, bay.Pods, "the first pod is back");
            bay.Tick(10f);
            Assert.AreEqual(2, bay.Pods);
            bay.Tick(100f);
            Assert.AreEqual(2, bay.Pods, "never above capacity");
        }
    }
}
