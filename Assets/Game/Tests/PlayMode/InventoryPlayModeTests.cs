using System.Collections;
using System.Linq;
using Blondie.Abilities;
using Blondie.Combat;
using Blondie.Core;
using Blondie.GridInventory;
using Blondie.Inventory;
using Blondie.Roguelike.Drafts;
using Blondie.Stats;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Isekai.Tests
{
    /// <summary>Phase 6 in the real Region scene: the Character's Active inventory and Storage, item rules and drafts.</summary>
    public class InventoryPlayModeTests
    {
        private CharacterInventory _inventory;
        private AbilityCaster _caster;

        [UnitySetUp]
        public IEnumerator LoadRegion()
        {
            RunSession.Clear();   // a fresh Run: no inventory carried over from an earlier test
            SceneManager.LoadScene("Region");
            yield return null;
            yield return null;
            _inventory = CharacterInventory.Current;
            _caster = _inventory.GetComponent<AbilityCaster>();
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            RunDrafts.ClearQueue();
            Time.timeScale = 1f;
            yield return null;
        }

        private static ItemDefinition Def(string id) => ItemDatabase.Find(id);

        private GridItem Entry(ItemGrid grid, string id) => grid.Items.FirstOrDefault(e => e.Item.Definition.Id == id);

        private bool Knows(string abilityId) => _caster.Slots.Any(s => s.Ability.Id == abilityId);

        private GridItem PlaceActive(string id)
        {
            Assert.AreEqual(0, _inventory.Active.Add(Def(id)), $"{id} fits in the Active inventory");
            return Entry(_inventory.Active, id);
        }

        [UnityTest]
        public IEnumerator Weapons_come_from_the_Active_inventory()
        {
            yield return null;
            Assert.IsNotNull(Entry(_inventory.Active, "shortbow"), "starts with the Shortbow active");
            Assert.IsTrue(Knows("shortbow"));
            GridItem blade = Entry(_inventory.Storage, "whirling_blade");
            Assert.IsNotNull(blade, "the Whirling Blade starts in Storage");
            Assert.IsFalse(Knows("debug_cleave"), "Storage Items do nothing");

            Assert.AreEqual(DropKind.Place, GridMoves.Drop(blade, int.MaxValue, _inventory.Active, new Int2(1, 0), 0));
            yield return null;
            Assert.IsTrue(Knows("debug_cleave"), "active: its weapon is learned");

            GridItem active = Entry(_inventory.Active, "whirling_blade");
            Assert.Greater(_inventory.Rules.CooldownLeft(active.Item), 25f);
            Assert.AreEqual(DropKind.Reject, GridMoves.Preview(active, int.MaxValue, _inventory.Storage, new Int2(0, 0), 0).Kind,
                "its 30 s swap cooldown keeps it active");
        }

        [UnityTest]
        public IEnumerator Active_stat_items_change_the_Characters_stats()
        {
            var stats = _inventory.GetComponent<StatBlock>();
            float speed = stats.GetValue(nameof(StatId.Speed));
            PlaceActive("swift_boots");
            yield return null;
            Assert.AreEqual(speed * 1.15f, stats.GetValue(nameof(StatId.Speed)), 1e-3f);
        }

        [UnityTest]
        public IEnumerator Bag_space_items_grow_the_Active_inventory()
        {
            Assert.AreEqual(3, _inventory.Active.Width);
            PlaceActive("satchel");
            yield return null;
            Assert.AreEqual(4, _inventory.Active.Width);
            Assert.AreEqual(4, _inventory.Active.Height);
        }

        [UnityTest]
        public IEnumerator A_locked_item_dropped_outside_the_window_comes_back()
        {
            _inventory.Active.Take(Entry(_inventory.Active, "shortbow"));   // make room for the 2x2 backpack
            GridItem backpack = PlaceActive("big_backpack");
            yield return null;
            Int2 at = backpack.Position;
            _inventory.Unit.Drop(backpack);
            yield return null;
            GridItem back = Entry(_inventory.Active, "big_backpack");
            Assert.IsNotNull(back, "still active");
            Assert.AreEqual(at, back.Position, "in the same spot");
        }

        [UnityTest]
        public IEnumerator The_debug_entry_opens_an_Item_draft_that_pauses_the_game()
        {
            DebugMenu.OpenItemDraft();
            yield return null;
            DraftOffer offer = RunDrafts.Current;
            Assert.IsNotNull(offer, "a draft is open");
            Assert.AreEqual(0f, Time.timeScale, "the game waits");
            var pick = (ItemOfferable)offer.Choices[0];
            int before = _inventory.Storage.Items.Count + _inventory.Active.Items.Count;
            Assert.IsTrue(RunDrafts.Take(0));
            yield return null;
            Assert.AreEqual(before + 1, _inventory.Storage.Items.Count + _inventory.Active.Items.Count, $"{pick.ItemId} was given");
            Assert.AreEqual(1f, Time.timeScale, "the game goes on");
        }

        [UnityTest]
        public IEnumerator Capturing_the_Region_removes_Region_items_and_unlocks_locked_ones()
        {
            _inventory.Active.Take(Entry(_inventory.Active, "shortbow"));
            PlaceActive("war_banner");
            PlaceActive("lucky_coin");
            yield return null;
            GridItem banner = Entry(_inventory.Active, "war_banner");
            Assert.IsFalse(_inventory.Rules.CanLeave(banner.Item));

            _inventory.OnRegionCaptured();
            yield return null;
            Assert.IsNull(Entry(_inventory.Active, "war_banner"), "Region-only: gone");
            Assert.IsNull(Entry(_inventory.Active, "lucky_coin"));
            Assert.IsTrue(_inventory.Rules.RegionDone);
        }
    }
}
