using System;
using System.Collections.Generic;
using System.Linq;
using Blondie.Core;
using Blondie.Inventory;
using Blondie.Roguelike;
using Blondie.Roguelike.Drafts;
using UnityEngine;

namespace Isekai
{
    /// <summary>A draft choice that gives the Character an Item.</summary>
    public sealed class ItemOfferable : Offerable
    {
        public string ItemId;

        protected override void OnTaken(Run run, int count)
        {
            ItemDefinition item = ItemDatabase.Find(ItemId);
            for (int i = 0; i < Math.Max(1, count); i++) CharacterInventory.Current?.Give(item);
        }
    }

    /// <summary>
    /// Item rewards: a debug entry offers (razing a settlement will) a pick of Items (Blondie Roguelike's draft window). Starts a Run if
    /// none is going (phase 7 makes the Run proper), and pauses the game while a draft is open.
    /// </summary>
    public sealed class ItemDrafts : MonoBehaviour
    {
        [SerializeField] private ItemAsset[] _pool = Array.Empty<ItemAsset>();
        [SerializeField, Min(1)] private int _choices = 3;
        [SerializeField] private string _title = "Spoils";

        private List<Offerable> _offers;
        private bool _paused;

        public IReadOnlyList<Offerable> Offers => _offers ??= BuildOffers();

        private void OnDisable()
        {
            if (_paused) { GamePause.Resume(this); _paused = false; }
        }

        private void Start()
        {
            if (!RunManager.HasActiveRun) RunManager.StartRun();
        }

        /// <summary>Offers a pick of Items now (also used by tests and debugging).</summary>
        public DraftOffer Present() => RunDrafts.Present(new DraftRequest { Pool = Offers.ToList(), Choices = _choices, Title = _title });

        // Gameplay waits while a draft is open. Through GamePause, so closing a draft never undoes another pause (a defeat).
        private void Update()
        {
            bool open = RunDrafts.Current != null;
            if (open == _paused) return;
            _paused = open;
            if (open) GamePause.Pause(this);
            else GamePause.Resume(this);
        }

        private List<Offerable> BuildOffers()
        {
            var offers = new List<Offerable>();
            foreach (ItemAsset asset in _pool)
            {
                if (asset == null) continue;
                ItemDefinition d = asset.Definition;
                var offer = Offerable.Create<ItemOfferable>("item." + d.Id, d.DisplayName);
                offer.ItemId = d.Id;
                string rules = string.Join(". ", d.Effects.Select(e => e.Describe()).Where(s => !string.IsNullOrEmpty(s)));
                offer.Description = string.IsNullOrEmpty(rules) ? d.Description : $"{d.Description} {rules}.".Trim();
                offers.Add(offer);
            }
            return offers;
        }
    }
}
