using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Item rewards: destroying an enemy Base offers a pick of Items (Blondie Roguelike's draft window). Starts a Run if
    /// none is going (phase 7 makes the Run proper), and pauses the game while a draft is open.
    /// </summary>
    public sealed class ItemDrafts : MonoBehaviour
    {
        [SerializeField] private ItemAsset[] _pool = Array.Empty<ItemAsset>();
        [SerializeField, Min(1)] private int _choices = 3;
        [SerializeField] private string _title = "Spoils of the Nest";

        private List<Offerable> _offers;
        private bool _paused;

        public IReadOnlyList<Offerable> Offers => _offers ??= BuildOffers();

        private void OnEnable() => EnemyNest.Destroyed += OnNestDestroyed;

        private void OnDisable()
        {
            EnemyNest.Destroyed -= OnNestDestroyed;
            if (_paused) { Time.timeScale = 1f; _paused = false; }
        }

        private void Start()
        {
            if (!RunManager.HasActiveRun) RunManager.StartRun();
        }

        /// <summary>Offers a pick of Items now (also used by tests and debugging).</summary>
        public DraftOffer Present() => RunDrafts.Present(new DraftRequest { Pool = Offers.ToList(), Choices = _choices, Title = _title });

        private void OnNestDestroyed(EnemyNest nest) => Present();

        // Gameplay waits while a draft is open.
        private void Update()
        {
            bool open = RunDrafts.Current != null;
            if (open == _paused) return;
            _paused = open;
            Time.timeScale = open ? 0f : 1f;
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
