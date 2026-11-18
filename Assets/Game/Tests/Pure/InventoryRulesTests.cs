using System.Collections.Generic;
using System.Linq;
using Blondie.Core;
using Blondie.GridInventory;
using Blondie.Inventory;
using NUnit.Framework;

namespace Isekai.Tests
{
    public class InventoryRulesTests
    {
        private float _now;
        private ItemGrid _active, _storage;
        private InventoryRules _rules;
        private readonly List<ItemDefinition> _made = new();

        [SetUp]
        public void SetUp()
        {
            _now = 0f;
            _active = new ItemGrid(4, 4, "active") { IsActive = true };
            _storage = new ItemGrid(6, 4, "storage");
            _rules = new InventoryRules(_active, _storage, () => _now);
        }

        // ItemDefinition.Create registers in the global ItemDatabase, which outlives the test (domain reload is off), so
        // test items get their own ids and are unregistered: a test "satchel" once replaced the real one in Play Mode.
        [TearDown]
        public void TearDown()
        {
            foreach (ItemDefinition d in _made) ItemDatabase.Unregister(d);
            _made.Clear();
        }

        private Item Make(string id, ItemRemoval removal = ItemRemoval.Free, ItemLifetime lifetime = ItemLifetime.Run, float cooldown = 10f)
        {
            ItemDefinition d = ItemDefinition.Create("test." + id, id);
            _made.Add(d);
            d.Effects.Add(new ItemRules { Removal = removal, Lifetime = lifetime, SwapCooldown = cooldown });
            return new Item(d);
        }

        private GridItem InActive(Item item, int x = 0, int y = 0)
        {
            GridItem entry = _active.Place(item, new Int2(x, y));
            _rules.Sync();
            return entry;
        }

        private static DropKind Try(GridItem entry, ItemGrid target, int x, int y) => GridMoves.Preview(entry, int.MaxValue, target, new Int2(x, y), 0).Kind;

        [Test]
        public void Free_items_move_anywhere()
        {
            GridItem entry = InActive(Make("boots"));
            Assert.AreEqual(DropKind.Place, Try(entry, _storage, 0, 0));
            Assert.AreEqual(DropKind.Place, Try(entry, _active, 2, 2));
        }

        [Test]
        public void Locked_items_stay_active_until_the_Region_is_captured_but_can_be_rearranged()
        {
            GridItem entry = InActive(Make("banner", ItemRemoval.Locked));
            Assert.AreEqual(DropKind.Reject, Try(entry, _storage, 0, 0));
            Assert.AreEqual(DropKind.Place, Try(entry, _active, 2, 2), "moving within the Active inventory is fine");
            _rules.RegionCompleted();
            Assert.AreEqual(DropKind.Place, Try(entry, _storage, 0, 0));
        }

        [Test]
        public void Fixed_items_cannot_move_at_all_until_the_Region_is_captured()
        {
            GridItem entry = InActive(Make("satchel", ItemRemoval.Fixed));
            Assert.AreEqual(DropKind.Reject, Try(entry, _active, 2, 2));
            Assert.AreEqual(DropKind.Reject, Try(entry, _storage, 0, 0));
            _rules.RegionCompleted();
            Assert.AreEqual(DropKind.Place, Try(entry, _active, 2, 2));
        }

        [Test]
        public void Cooldown_items_leave_only_after_their_cooldown()
        {
            GridItem entry = InActive(Make("pick", ItemRemoval.Cooldown, cooldown: 10f));
            _now = 4f;
            Assert.AreEqual(6f, _rules.CooldownLeft(entry.Item), 1e-4f);
            Assert.AreEqual(DropKind.Reject, Try(entry, _storage, 0, 0));
            _now = 10f;
            Assert.AreEqual(DropKind.Place, Try(entry, _storage, 0, 0));
        }

        [Test]
        public void A_locked_active_item_cannot_be_swapped_out_from_Storage()
        {
            GridItem locked = InActive(Make("banner", ItemRemoval.Locked));
            GridItem other = _storage.Place(Make("boots"), new Int2(0, 0));
            Assert.AreEqual(DropKind.Reject, Try(other, _active, 0, 0), "the swap would push the banner into Storage");
            Assert.IsTrue(_active.Items.Contains(locked));
        }

        [Test]
        public void A_refused_drop_is_put_back_where_it_was_and_keeps_its_cooldown()
        {
            Item pick = Make("pick", ItemRemoval.Cooldown, cooldown: 10f);
            GridItem entry = InActive(pick, 1, 2);
            _now = 5f;
            Item taken = _active.Take(entry);
            _rules.Sync();
            Assert.IsTrue(_rules.PutBackIfNotAllowed(taken));
            Assert.AreEqual(new Int2(1, 2), _active.ItemAt(new Int2(1, 2)).Position);
            Assert.AreEqual(5f, _rules.CooldownLeft(pick), 1e-4f, "the clock wasn't restarted");
        }

        [Test]
        public void Region_lifetime_items_expire_when_the_Region_is_captured()
        {
            InActive(Make("coin", lifetime: ItemLifetime.Region));
            _storage.Place(Make("spare", lifetime: ItemLifetime.Region), new Int2(0, 0));
            InActive(Make("boots"), 2, 2);
            var expired = _rules.RegionCompleted();
            CollectionAssert.AreEquivalent(new[] { "coin", "spare" }, expired.ConvertAll(e => e.Item.Definition.DisplayName));
        }

        [Test]
        public void Rules_describe_themselves_for_tooltips()
        {
            StringAssert.Contains("captured", new ItemRules { Removal = ItemRemoval.Locked }.Describe());
            StringAssert.Contains("this Region only", new ItemRules { Lifetime = ItemLifetime.Region }.Describe());
            Assert.IsNull(new ItemRules().Describe());
        }
    }
}
