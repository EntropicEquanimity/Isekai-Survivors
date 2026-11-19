using System;
using System.Collections;
using System.Linq;
using Blondie.Combat;
using Blondie.Core;
using Blondie.FlowFields;
using Blondie.Pooling;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Isekai.Tests
{
    /// <summary>Phase 4 in the real Region scene, without the horde, at 4x speed.</summary>
    public class BuildingPlayModeTests
    {
        private RegionBuildings _buildings;
        private Building _mainBase;
        private BuildMode _mode;

        [UnitySetUp]
        public IEnumerator LoadRegion()
        {
            yield return RegionTestScene.Load();
            RegionTestScene.ClearHorde();
            yield return null;   // RegionBuildings claims the Main Base's ground after the grid bakes
            _buildings = RegionBuildings.Current;
            _mainBase = Building.All.First(b => b.IsMainBase);
            _mode = Object.FindAnyObjectByType<BuildMode>();
            Time.timeScale = 4f;
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = 1f;
            yield return null;
        }

        private BuildingAsset Catalogue(string name) => _mode.Catalogue.First(a => a.DisplayName == name);

        private Int2 BesideMainBase(int dx) => new(_mainBase.Origin.x + dx, _mainBase.Origin.y);

        [UnityTest]
        public IEnumerator A_Portal_costs_stone_blocks_the_horde_builds_and_brings_Imps()
        {
            FlowFieldGrid field = HordeDirector.Current.Grid;
            Assert.AreEqual(FlowFieldGrid.Blocked, field.GetCost(_mainBase.Origin), "the Main Base blocks its ground");

            Stockpile pile = RegionEconomy.Current.Stockpile;
            int stone = pile.Get(ResourceType.Stone);
            BuildingAsset barracks = Catalogue("Portal");
            Building placed = _buildings.Place(barracks, BesideMainBase(4));
            Assert.IsNotNull(placed, "placed inside the Main Base's area");
            Assert.AreEqual(stone - 20, pile.Get(ResourceType.Stone));
            Assert.AreEqual(FlowFieldGrid.Blocked, field.GetCost(BesideMainBase(4)));
            Assert.IsFalse(placed.IsBuilt);

            yield return TestWait.Until(() => placed.IsBuilt, "the Portal finishes", 15f);
            var producer = placed.GetComponent<UnitProducer>();
            yield return TestWait.Until(() => producer.Units.Count > 0, "the Portal brings an Imp", 15f);
            Assert.AreEqual(Stance.Guard, producer.Units[0].Stance, "Units follow their Building's stance");
            producer.Stance = Stance.Hold;
            Assert.AreEqual(Stance.Hold, producer.Units[0].Stance);
        }

        [UnityTest]
        public IEnumerator Buildings_fit_side_by_side_and_their_art_and_ghost_cover_the_footprint()
        {
            RegionEconomy.Current.Stockpile.Add(ResourceType.Stone, 100);
            BuildingAsset barracks = Catalogue("Portal");
            Building left = _buildings.Place(barracks, BesideMainBase(4));
            Building right = _buildings.Place(barracks, BesideMainBase(6));
            Assert.IsNotNull(left);
            Assert.IsNotNull(right, "the very next cells over");
            float cell = _buildings.CellSize;
            Bounds art = left.GetComponentsInChildren<SpriteRenderer>().First(r => r.name == "Art").bounds;
            Assert.AreEqual(2f * cell, art.size.x, 1e-3f, "the art is as wide as the footprint");

            _mode.Pick(barracks);
            var ghost = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Exclude).First(r => r.name == "Ghost");
            Assert.AreEqual(2f * cell, ghost.bounds.size.x, 1e-3f, "the ghost too");
            _mode.Cancel();
            yield return null;
        }

        [UnityTest]
        public IEnumerator Placement_is_refused_outside_a_Base_and_when_too_expensive()
        {
            BuildingAsset tower = Catalogue("Spire");
            Assert.AreEqual(Verdict.OutsideBase, _buildings.Check(tower, BesideMainBase(30)).Verdict);
            Assert.AreEqual(Verdict.Blocked, _buildings.Check(tower, _mainBase.Origin).Verdict, "on the Main Base");
            RegionEconomy.Current.Stockpile.TrySpend(ResourceType.Stone, RegionEconomy.Current.Stockpile.Get(ResourceType.Stone));
            Assert.AreEqual(Verdict.TooExpensive, _buildings.Check(tower, BesideMainBase(4)).Verdict);
            Assert.IsNull(_buildings.Place(tower, BesideMainBase(4)));
            yield return null;
        }

        [UnityTest]
        public IEnumerator A_Cloning_Pod_brings_the_Character_back_once()
        {
            var character = Object.FindAnyObjectByType<CharacterControl>();
            var damageable = character.GetComponent<Damageable>();
            character.transform.position = _mainBase.transform.position + new Vector3(10f, 0f, 0f);

            damageable.Unit.TakeDamage(10000f, DamageRules.TrueType);
            Assert.IsTrue(character.IsDead);
            yield return TestWait.Until(() => !character.IsDead, "the clone is ready", 15f);
            Assert.Less(Vector3.Distance(character.transform.position, _mainBase.transform.position), 3f, "cloned at the Main Base");
            Assert.AreEqual(0, _mainBase.GetComponent<CloningPods>().Bay.Pods);

            damageable.Unit.TakeDamage(10000f, DamageRules.TrueType);
            yield return new WaitForSeconds(10f);
            Assert.IsTrue(character.IsDead, "no pods left: stays dead");
            Assert.IsFalse(Object.FindAnyObjectByType<RegionDefeat>().IsDefeated, "losing the Character alone doesn't lose the Run");
        }

        [UnityTest]
        public IEnumerator Losing_the_Main_Base_loses_the_Run()
        {
            var defeat = Object.FindAnyObjectByType<RegionDefeat>();
            _mainBase.Damageable.Unit.TakeDamage(100000f, DamageRules.TrueType);
            yield return TestWait.Until(() => defeat.IsDefeated, "the Run is lost", 3f);
            Assert.IsFalse(Object.FindAnyObjectByType<HordeSpawner>().enabled);
            Assert.IsTrue(GamePause.IsPausedBy(defeat), "the game freezes behind the dialog");
            Assert.AreEqual(0f, Time.timeScale);
            Assert.IsFalse(Object.FindAnyObjectByType<CharacterControl>().enabled, "the Character takes no more orders");
            Assert.IsFalse(_mode.enabled, "no more building");
        }

    }
}
