using System;
using System.Collections.Generic;
using Blondie.GridInventory;
using Blondie.Inventory;
using Blondie.UI;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// The Character's run inventory on a Blondie <see cref="GridInventoryHolder"/>: an Active grid (Items there take
    /// effect) and Storage. <see cref="InventoryRules"/> enforces each Item's removal rule on every drag; Items
    /// dragged out of the window go to Storage (or back where they were, when they may not leave). Active
    /// <see cref="BagSpace"/> Items grow the Active grid. Capturing the Region unlocks Locked and Fixed Items and
    /// removes Region-only ones.
    /// </summary>
    [RequireComponent(typeof(GridInventoryHolder))]
    public sealed class CharacterInventory : MonoBehaviour
    {
        public const string ActiveId = "active";
        public const string StorageId = "storage";

        [SerializeField] private Vector2Int _baseActiveSize = new(3, 3);
        [SerializeField] private ItemAsset[] _startingActive = Array.Empty<ItemAsset>();
        [SerializeField] private ItemAsset[] _startingStorage = Array.Empty<ItemAsset>();

        private GridInventoryHolder _holder;
        private bool _resizing;

        public static CharacterInventory Current { get; private set; }

        public InventoryRules Rules { get; private set; }
        public GridInventoryUnit Unit => _holder.Unit;
        public ItemGrid Active => Unit.GetGrid(ActiveId);
        public ItemGrid Storage => Unit.GetGrid(StorageId);

        private void Awake() => _holder = GetComponent<GridInventoryHolder>();

        private void OnEnable() => Current = this;

        private void OnDisable()
        {
            if (Current == this) Current = null;
        }

        private void Start()
        {
            ItemPickups.SpawnOnDrop = false;   // Items never land in the world: a drop goes to Storage
            // Restore first: it may rebuild the grids, and the rules must sit on the ones in use.
            GridInventorySaveData carried = RunSession.TakeInventory();
            if (carried != null) Unit.Restore(carried);   // the Run's Items from earlier Regions
            else
            {
                foreach (ItemAsset a in _startingActive) if (a != null) Active.Add(a.Definition);
                foreach (ItemAsset a in _startingStorage) if (a != null) Storage.Add(a.Definition);
                foreach (string id in MetaProgress.StartingItems())   // the Legacy tree's starting Items
                {
                    ItemDefinition item = ItemDatabase.Find(id);
                    if (item != null) Storage.Add(item);
                }
            }
            Rules = new InventoryRules(Active, Storage, () => Time.time);
            Unit.Changed += OnChanged;
            Unit.Dropped += OnDropped;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_holder == null || _holder.Unit == null) return;
            _holder.Unit.Changed -= OnChanged;
            _holder.Unit.Dropped -= OnDropped;
        }

        /// <summary>Adds a new Item: to Storage, else the Active grid. False if neither has room.</summary>
        public bool Give(ItemDefinition item)
        {
            if (item == null) return false;
            bool given = Storage.Add(item) == 0 || Active.Add(item) == 0;
            Say(given ? $"+ {item.DisplayName}" : $"No room for {item.DisplayName}", given ? Color.white : Color.red);
            return given;
        }

        private void OnChanged(ItemGrid grid)
        {
            if (!_resizing) Refresh();
        }

        private void Refresh()
        {
            Rules.Sync();
            ResizeForBagSpace();
        }

        private void OnDropped(Item item)
        {
            if (Rules.PutBackIfNotAllowed(item))
            {
                float left = Rules.CooldownLeft(item);
                Say(left > 0f ? $"{item.DisplayName}: {left:0} s until it can be swapped" : $"{item.DisplayName} can't be removed yet", Color.yellow);
                return;
            }
            if (Storage.Add(item) > 0) Say($"Storage full: {item.DisplayName} was lost", Color.red);
        }

        /// <summary>The Region is captured: unlock Locked and Fixed Items and remove Region-only ones (called by <see cref="RegionRun"/>).</summary>
        internal void OnRegionCaptured()
        {
            List<GridItem> expired = Rules.RegionCompleted();
            foreach (GridItem entry in expired)
            {
                Say($"{entry.Item.DisplayName} fades away", new Color(0.7f, 0.7f, 1f));
                entry.Grid.Take(entry);
            }
            Refresh();
        }

        // The Active grid is its base size plus every active BagSpace Item; shrinking pushes what no longer fits to Storage.
        private void ResizeForBagSpace()
        {
            int columns = 0, rows = 0;
            foreach (GridItem entry in Active.Items)
                foreach (ItemEffect effect in entry.Item.Definition.Effects)
                    if (effect is BagSpace bag) { columns += bag.Columns; rows += bag.Rows; }
            int width = _baseActiveSize.x + columns, height = _baseActiveSize.y + rows;
            if (Active.Width == width && Active.Height == height) return;
            _resizing = true;
            try
            {
                foreach (Item lost in Unit.ResizeGrid(ActiveId, width, height, dropLeftovers: false))
                    if (Storage.Add(lost) > 0) Say($"Storage full: {lost.DisplayName} was lost", Color.red);
            }
            finally
            {
                _resizing = false;
            }
            Rules.Sync();
        }

        private void Say(string text, Color color) => FloatingText.Spawn(transform.position + Vector3.up * 1.2f, text, color);
    }
}
