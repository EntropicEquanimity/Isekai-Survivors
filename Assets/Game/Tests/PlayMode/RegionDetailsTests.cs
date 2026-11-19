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
            yield return RegionTestScene.Load();
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = 1f;
            DebugActions.Find("God mode")?.Set(false);
            yield return null;
        }

        [UnityTest]
        public IEnumerator A_hurt_enemy_shows_its_health_bar_and_an_unhurt_one_doesnt()
        {
            var spawner = Object.FindAnyObjectByType<HordeSpawner>();
            var control = Object.FindAnyObjectByType<CharacterControl>();
            GameObject go = Pools.Spawn(spawner.Prefab, control.transform.position + new Vector3(3f, 0f, 0f), Quaternion.identity);
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
            Assert.GreaterOrEqual(HordeDirector.Current.Grid.Width, 150);
            var wilds = Object.FindAnyObjectByType<RegionWilds>();
            yield return TestWait.Until(() => wilds.GroupCount > 0, "the wilds fill in", 3f);
            Assert.Greater(wilds.Roaming, wilds.GroupCount, "groups of several roaming enemies");
            Building home = Building.All.First(b => b.IsMainBase);
            foreach (HordeMember m in Object.FindObjectsByType<HordeMember>(FindObjectsInactive.Exclude).Where(m => m.IsRoaming))
                Assert.Greater(Vector3.Distance(m.transform.position, home.transform.position), 15f, "nothing roams next to the Base");
        }

        [Test]
        public void Sprites_sort_by_their_height_on_screen()
        {
            var character = Object.FindAnyObjectByType<CharacterControl>().GetComponentInChildren<SpriteRenderer>();
            var enemy = Object.FindAnyObjectByType<HordeSpawner>().Prefab.GetComponentInChildren<SpriteRenderer>();
            Assert.AreEqual(character.sortingOrder, enemy.sortingOrder, "units share an order, so height decides who's in front");
            Building home = Building.All.First(b => b.IsMainBase);
            int building = home.GetComponentsInChildren<SpriteRenderer>().First(r => r.name == "Art").sortingOrder;
            int stone = ResourceNode.All.First().GetComponentInChildren<SpriteRenderer>().sortingOrder;
            Assert.Less(building, character.sortingOrder, "Buildings draw under units: the Character never hides behind one");
            Assert.LessOrEqual(stone, building, "Stone deposits stand like Buildings, under units");
        }

        [UnityTest]
        public IEnumerator Rocks_are_walls_until_mined_out()
        {
            yield return TestWait.Until(() => RegionBootstrap.Current.IsPopulated, "the Region is ready");
            RegionGrid grid = RegionGrid.Current;
            Obstacle[] walls = Object.FindObjectsByType<Obstacle>(FindObjectsInactive.Exclude);
            Assert.Greater(walls.Length, 50, "the Region has rocks");
            foreach (Obstacle wall in walls)
                Assert.IsFalse(grid.IsWalkable(wall.transform.position), $"{wall.name} blocks its cell");

            ResourceNode rock = walls.Select(w => w.GetComponent<ResourceNode>()).First(n => n != null && n.name.StartsWith("Rock"));
            Vector3 at = rock.transform.position;
            rock.Take(rock.Remaining);
            yield return null;
            Assert.IsFalse(rock.gameObject.activeSelf, "mined out");
            Assert.IsTrue(grid.IsWalkable(at), "its cell is open again");
        }

        [UnityTest]
        public IEnumerator Walking_up_to_Stone_mines_it()
        {
            var character = Object.FindAnyObjectByType<CharacterControl>();
            ResourceNode node = ResourceNode.All.First();
            var hud = Object.FindAnyObjectByType<StockpileHud>();
            character.transform.position = node.transform.position + new Vector3(0.5f, 0f, 0f);
            yield return null;
            yield return null;
            StringAssert.StartsWith("Mining ", hud.Prompt, "whatever is nearest (Stone, a rock, a dead tree) gets mined");
            int before = Total();
            yield return TestWait.Until(() => Total() > before, "no key needed: Resources come in", 3f);
            character.transform.position = FarFromAnyNode(node.transform.position);
            yield return null;   // the Miner lets go this frame; the HUD may read it before or after
            yield return null;
            Assert.AreEqual(string.Empty, hud.Prompt);
        }

        private static int Total()
        {
            Stockpile pile = RegionEconomy.Current.Stockpile;
            return pile.Get(ResourceType.Stone) + pile.Get(ResourceType.Wood);
        }

        // A walkable spot with nothing minable within reach (rocks are everywhere now).
        private static Vector3 FarFromAnyNode(Vector3 from)
        {
            for (int r = 3; r < 40; r++)
            for (int k = 0; k < 16; k++)
            {
                float a = k * Mathf.PI / 8f;
                Vector3 p = from + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * r;
                if (RegionGrid.Current.IsWalkable(p) && ResourceNode.NearestTo(p, 3f) == null) return p;
            }
            Assert.Fail("no open ground away from Resource nodes");
            return from;
        }

        [UnityTest]
        public IEnumerator The_debug_menu_is_there_and_its_buttons_work()
        {
            Assert.IsNotNull(DebugMenu.Instance, "created on scene load");
            Stockpile pile = RegionEconomy.Current.Stockpile;
            int stone = pile.Get(ResourceType.Stone);
            TestDebug.Press("+100 each");
            Assert.AreEqual(stone + 100, pile.Get(ResourceType.Stone));

            var character = Object.FindAnyObjectByType<CharacterControl>().GetComponent<Damageable>();
            TestDebug.Toggle("God mode", true);
            character.Unit.TakeDamage(10000f, DamageRules.PhysicalType);
            Assert.IsFalse(character.IsDead, "god mode");
            TestDebug.Toggle("God mode", false);

            Assert.IsFalse(RegionEvents.HasEnded);
            TestDebug.Press("Destroy the Seat");
            yield return null;
            Assert.IsTrue(RegionEvents.HasEnded);
            Assert.AreEqual(RegionEnd.Captured, RegionEvents.Outcome, "destroying the Seat ends the Region as captured");
        }

    }
}
