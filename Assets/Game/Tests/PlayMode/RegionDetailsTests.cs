using System;
using System.Collections;
using System.Linq;
using Blondie.Combat;
using Blondie.Pooling;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Isekai.Tests
{
    /// <summary>The playtest round after phase 8: health bars, a bigger Region with wanderers, Wave distance, Y sorting and the debug menu.</summary>
    public class RegionDetailsTests
    {
        [UnitySetUp]
        public IEnumerator LoadRegion()
        {
            SceneManager.LoadScene(RunSession.RegionScene);
            yield return null;
            ItemDrafts drafts = Object.FindAnyObjectByType<ItemDrafts>();
            if (drafts != null) drafts.enabled = false;   // a draft (after a nest falls) pauses the game
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = 1f;
            if (DebugMenu.Instance != null) DebugMenu.Instance.GodMode = false;
            yield return null;
        }

        [UnityTest]
        public IEnumerator A_hurt_enemy_shows_its_health_bar_and_an_unhurt_one_doesnt()
        {
            var spawner = Object.FindAnyObjectByType<HordeSpawner>();
            var control = Object.FindAnyObjectByType<CharacterControl>();
            GameObject go = Pools.Spawn(spawner.Prefab, control.transform.position + new Vector3(30f, 0f, 0f), Quaternion.identity);
            go.GetComponent<HordeMember>().SetRoaming(true);
            var bar = go.GetComponent<HealthBar>();
            var damageable = go.GetComponent<Damageable>();
            Assert.IsNotNull(bar, "enemies carry a health bar");
            Assert.IsFalse(bar.IsShown, "full health: hidden");

            damageable.Unit.TakeDamage(damageable.MaxHealth * 0.5f, DamageRules.TrueType);
            yield return null;
            Assert.IsTrue(bar.IsShown);
            Assert.Less(bar.FilledPixels, 12, "half full");
            Assert.Greater(bar.FilledPixels, 0);
            Assert.IsTrue(control.GetComponent<HealthBar>().IsShown, "the Character's bar always shows");
        }

        [UnityTest]
        public IEnumerator The_Region_is_big_and_has_wandering_groups_away_from_the_Main_Base()
        {
            Assert.GreaterOrEqual(HordeDirector.Current.Grid.Width, 250);
            var wilds = Object.FindAnyObjectByType<RegionWilds>();
            yield return WaitFor(() => wilds.GroupCount > 0, 3f, "the wilds fill in");
            Assert.Greater(wilds.Roaming, wilds.GroupCount, "groups of several roaming enemies");
            Building home = Building.All.First(b => b.IsMainBase);
            foreach (HordeMember m in Object.FindObjectsByType<HordeMember>(FindObjectsInactive.Exclude).Where(m => m.IsRoaming))
                Assert.Greater(Vector3.Distance(m.transform.position, home.transform.position), 15f, "nothing roams next to the Base");
        }

        [UnityTest]
        public IEnumerator Waves_appear_within_reach_of_the_Main_Base()
        {
            var waves = Object.FindAnyObjectByType<WaveDirector>();
            Building home = Building.All.First(b => b.IsMainBase);
            int before = HordeDirector.Current.Attacking;
            waves.Skip(waves.SecondsUntilNext + 0.01f);
            yield return WaitFor(() => HordeDirector.Current.Attacking > before + 3, 5f, "a Wave spawns");
            int near = Object.FindObjectsByType<HordeMember>(FindObjectsInactive.Exclude)
                .Count(m => !m.IsRoaming && Vector3.Distance(m.transform.position, home.transform.position) < 60f);
            Assert.Greater(near, 3, "the Wave starts within 60 units, not at the far edge");
        }

        [Test]
        public void Sprites_sort_by_their_height_on_screen()
        {
            var character = Object.FindAnyObjectByType<CharacterControl>().GetComponentInChildren<SpriteRenderer>();
            var enemy = Object.FindAnyObjectByType<HordeSpawner>().Prefab.GetComponentInChildren<SpriteRenderer>();
            Assert.AreEqual(character.sortingOrder, enemy.sortingOrder, "units share an order, so height decides who's in front");
        }

        [UnityTest]
        public IEnumerator The_debug_menu_is_there_and_its_buttons_work()
        {
            Assert.IsNotNull(DebugMenu.Instance, "created on scene load");
            Stockpile pile = RegionEconomy.Current.Stockpile;
            int stone = pile.Get(ResourceType.Stone);
            DebugMenu.AddResources(100);
            Assert.AreEqual(stone + 100, pile.Get(ResourceType.Stone));

            var character = Object.FindAnyObjectByType<CharacterControl>().GetComponent<Damageable>();
            DebugMenu.Instance.GodMode = true;
            character.Unit.TakeDamage(10000f, DamageRules.PhysicalType);
            Assert.IsFalse(character.IsDead, "god mode");
            DebugMenu.Instance.GodMode = false;

            Assert.Greater(EnemyNest.All.Count, 0);
            DebugMenu.DestroyNests();
            yield return null;
            Assert.AreEqual(0, EnemyNest.All.Count, "destroyed nests leave the list");
        }

        private static IEnumerator WaitFor(Func<bool> condition, float seconds, string what)
        {
            float end = Time.time + seconds;
            while (!condition())
            {
                if (Time.time > end) Assert.Fail("Timed out: " + what);
                yield return null;
            }
        }
    }
}
