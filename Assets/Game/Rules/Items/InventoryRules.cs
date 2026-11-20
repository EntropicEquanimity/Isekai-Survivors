using System;
using System.Collections.Generic;
using Blondie.Core;
using Blondie.GridInventory;
using Blondie.Inventory;

namespace Isekai
{
    /// <summary>
    /// Enforces <see cref="ItemRules"/> on the Character's Active inventory and Storage. It installs a placement rule on
    /// both grids, so every drag, drop and swap through Blondie Grid Inventory is checked: an Item that may not leave the
    /// Active inventory can't be dropped into Storage or swapped out, and a Fixed Item can't move at all. Call
    /// <see cref="Sync"/> whenever the grids change, so it knows how long each Item has been active.
    /// </summary>
    public sealed class InventoryRules
    {
        private readonly ItemGrid _active;
        private readonly ItemGrid _storage;
        private readonly Func<float> _clock;
        private readonly Dictionary<Item, float> _activeSince = new();
        private readonly Dictionary<Item, (Int2 position, int rotation)> _lastSpot = new();
        private readonly HashSet<Item> _inActive = new();

        public InventoryRules(ItemGrid active, ItemGrid storage, Func<float> clock)
        {
            _active = active ?? throw new ArgumentNullException(nameof(active));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _active.PlacementRule = (item, position, shape, moving, swapped) => Allows(_active, moving, swapped);
            _storage.PlacementRule = (item, position, shape, moving, swapped) => Allows(_storage, moving, swapped);
            Sync();
        }

        /// <summary>True once the current Region is captured: Locked and Fixed Items are free again.</summary>
        public bool RegionDone { get; private set; }

        public ItemGrid Active => _active;
        public ItemGrid Storage => _storage;

        /// <summary>Whether <paramref name="item"/> may leave the Active inventory now.</summary>
        public bool CanLeave(Item item)
        {
            ItemRules rules = ItemRules.Of(item);
            return rules.Removal switch
            {
                ItemRemoval.Cooldown => CooldownLeft(item) <= 0f,
                ItemRemoval.Locked or ItemRemoval.Fixed => RegionDone,
                _ => true,
            };
        }

        /// <summary>Whether <paramref name="item"/> may move to another spot inside the Active inventory.</summary>
        public bool CanMoveWithin(Item item) => ItemRules.Of(item).Removal != ItemRemoval.Fixed || RegionDone;

        /// <summary>Seconds until a Cooldown Item may leave (0 for the rest, or when it isn't active).</summary>
        public float CooldownLeft(Item item)
        {
            ItemRules rules = ItemRules.Of(item);
            if (rules.Removal != ItemRemoval.Cooldown || !_activeSince.TryGetValue(item, out float since)) return 0f;
            return Math.Max(0f, rules.SwapCooldown - (_clock() - since));
        }

        /// <summary>Notes which Items are active (and since when) and where they sit. Call after every change.</summary>
        public void Sync()
        {
            float now = _clock();
            _inActive.Clear();
            foreach (GridItem entry in _active.Items)
            {
                _inActive.Add(entry.Item);
                if (!_activeSince.ContainsKey(entry.Item)) _activeSince[entry.Item] = now;
                _lastSpot[entry.Item] = (entry.Position, entry.Rotation);
            }
            // Items in Storage start their clock afresh when they come back. Items in neither grid keep theirs, so one
            // put back after a refused drop keeps its cooldown.
            foreach (GridItem entry in _storage.Items)
            {
                _activeSince.Remove(entry.Item);
                _lastSpot.Remove(entry.Item);
            }
        }

        /// <summary>
        /// An Item left the grids by being dropped. Returns true if the rules forbid that and it was put back where it
        /// was (or anywhere in the Active inventory); false if it may go.
        /// </summary>
        public bool PutBackIfNotAllowed(Item item)
        {
            if (item == null || !_lastSpot.TryGetValue(item, out (Int2 position, int rotation) spot)) return false;
            if (CanLeave(item) && CanMoveWithin(item)) return false;
            GridPlacementRule rule = _active.PlacementRule;
            _active.PlacementRule = null;   // putting it back is always allowed
            try
            {
                if (_active.Place(item, spot.position, spot.rotation) == null) _active.Add(item);
            }
            finally
            {
                _active.PlacementRule = rule;
            }
            Sync();
            return true;
        }

        /// <summary>The Region is captured: Locked and Fixed Items unlock, and Region-lifetime Items are returned for removal.</summary>
        public List<GridItem> RegionCompleted()
        {
            RegionDone = true;
            var expired = new List<GridItem>();
            foreach (ItemGrid grid in new[] { _active, _storage })
                foreach (GridItem entry in grid.Items)
                    if (ItemRules.Of(entry.Item).Lifetime == ItemLifetime.Region) expired.Add(entry);
            return expired;
        }

        /// <summary>A new Region starts: Locked and Fixed Items lock again.</summary>
        public void RegionStarted() => RegionDone = false;

        // moving: the entry being moved (null for a new Item). swapped: the entry it would swap with, if any.
        private bool Allows(ItemGrid target, GridItem moving, GridItem swapped)
        {
            if (moving != null && moving.Grid == _active)
            {
                if (!CanMoveWithin(moving.Item)) return false;
                if (target != _active && !CanLeave(moving.Item)) return false;
            }
            if (swapped != null && swapped.Grid == _active)
            {
                if (!CanMoveWithin(swapped.Item)) return false;
                bool leaves = moving == null || moving.Grid != _active;   // it would go where the moving Item came from
                if (leaves && !CanLeave(swapped.Item)) return false;
            }
            return true;
        }
    }
}
