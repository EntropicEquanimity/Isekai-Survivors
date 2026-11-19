using System.Collections;
using System.Linq;
using Blondie.Abilities;
using Blondie.Combat;
using Blondie.Core;
using Blondie.FlowFields;
using Blondie.Pooling;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Isekai.Tests
{
    /// <summary>The seams new mechanics plug into: ordered Region startup, placement rules, team relations, Region end, debug entries.</summary>
    public class FoundationTests
    {
        [UnitySetUp]
        public IEnumerator Load()
        {
            yield return RegionTestScene.Load();
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Teams.ClearRelations();
            Time.timeScale = 1f;
            yield return null;
        }

        [UnityTest]
        public IEnumerator The_Region_sets_up_then_populates_with_a_stable_seed()
        {
            RegionBootstrap boot = RegionBootstrap.Current;
            Assert.IsNotNull(boot, "the Region scene has a bootstrap");
            yield return TestWait.Until(() => boot.IsPopulated, "the Region fills once its walls are baked");
            Assert.IsTrue(RegionGrid.Current.IsBaked);
            Assert.IsNotNull(boot.Context.Threat, "the Waves registered as the Region's threat during setup");
            Assert.AreEqual(boot.Context.Region.Depth, Object.FindAnyObjectByType<WaveDirector>().Depth, "the Waves were set up from the context");

            var again = new RegionContext(boot.Context.Region, boot.Context.Run, RunSession.Seed);
            Assert.AreEqual(boot.Context.Seed, again.Seed, "the same Run gives the Region the same seed");
            Assert.AreEqual(boot.Context.Random("wilds").Next(), again.Random("wilds").Next());
            Assert.AreNotEqual(boot.Context.Random("wilds").Next(), boot.Context.Random("waves").Next(), "each system has its own stream");
            Assert.AreEqual(Player.Character, Object.FindAnyObjectByType<CharacterControl>(), "the player's body is known without searching");
        }

        [UnityTest]
        public IEnumerator A_new_placement_rule_can_forbid_building_and_say_why()
        {
            yield return TestWait.Until(() => Building.All.Any(b => b.IsMainBase), "the Main Base");
            RegionBuildings buildings = RegionBuildings.Current;
            BuildingAsset tower = Object.FindAnyObjectByType<BuildMode>().Catalogue.First(a => a.DisplayName == "Spire");
            RegionEconomy.Current.Stockpile.Add(ResourceType.Stone, 100);
            Building home = Building.All.First(b => b.IsMainBase && b.IsPlayers);
            var beside = new Int2(home.Origin.x + 4, home.Origin.y);
            Assert.IsTrue(buildings.Check(tower, beside).IsOk);

            buildings.Rules.Add(new NoBuildingRule());
            Placement result = buildings.Check(tower, beside);
            Assert.AreEqual(Verdict.NotAllowed, result.Verdict);
            Assert.AreEqual("Not on hallowed ground", result.Reason);
            Assert.IsNull(buildings.Place(tower, beside));
        }

        private sealed class NoBuildingRule : IPlacementRule
        {
            public Placement Check(in PlacementQuery query) => Placement.No(Verdict.NotAllowed, "Not on hallowed ground");
        }

        [UnityTest]
        public IEnumerator Who_the_horde_attacks_comes_from_team_relations()
        {
            RegionTestScene.ClearHorde();
            Object.FindAnyObjectByType<FlowFieldCrowd>().Speed = 0f;
            CharacterControl character = Player.Character;
            character.GetComponent<AutoCaster>().enabled = false;
            var health = character.GetComponent<Damageable>();
            GameObject prefab = Object.FindAnyObjectByType<HordeSpawner>().Prefab;

            Teams.SetRelation(TeamIds.Enemy, TeamIds.Player, TeamRelation.Neutral);
            Pools.Spawn(prefab, character.transform.position + new Vector3(0.3f, 0f, 0f), Quaternion.identity);
            yield return new WaitForSeconds(1.5f);
            Assert.AreEqual(health.MaxHealth, health.Health, 1e-3f, "a neutral horde leaves the Character alone");

            Teams.ClearRelations();
            yield return TestWait.Until(() => health.Health < health.MaxHealth, "a hostile horde hits", 3f);
        }

        [UnityTest]
        public IEnumerator Systems_add_their_own_debug_entries()
        {
            yield return null;
            var waves = Object.FindAnyObjectByType<WaveDirector>();
            Assert.IsTrue(DebugActions.All.Any(e => e.Owner == (object)waves && e.Label == "Next Wave now"));
            float before = waves.SecondsUntilNext;
            DebugActions.All.First(e => e.Owner == (object)waves && e.Label == "Next Wave now").Run();
            Assert.Less(waves.SecondsUntilNext, before, "the entry runs the Waves' own action");
            waves.enabled = false;
            Assert.IsFalse(DebugActions.All.Any(e => e.Owner == (object)waves), "a stopped system takes its entries away");
        }
    }
}
