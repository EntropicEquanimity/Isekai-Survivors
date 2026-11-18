using System;
using System.Collections.Generic;

namespace Isekai
{
    /// <summary>The Resources the player holds in a Region. Amounts never go below zero.</summary>
    public sealed class Stockpile
    {
        private readonly Dictionary<ResourceType, int> _amounts = new();

        /// <summary>A Resource's amount changed: the type and its new amount.</summary>
        public event Action<ResourceType, int> Changed;

        public int Get(ResourceType type) => _amounts.TryGetValue(type, out int amount) ? amount : 0;

        public void Add(ResourceType type, int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "Use TrySpend to take Resources away.");
            if (amount == 0) return;
            _amounts[type] = Get(type) + amount;
            Changed?.Invoke(type, _amounts[type]);
        }

        public bool CanAfford(ResourceType type, int amount) => amount <= Get(type);

        /// <summary>Takes <paramref name="amount"/> if there is enough, else takes nothing and returns false.</summary>
        public bool TrySpend(ResourceType type, int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            if (!CanAfford(type, amount)) return false;
            if (amount == 0) return true;
            _amounts[type] = Get(type) - amount;
            Changed?.Invoke(type, _amounts[type]);
            return true;
        }
    }
}
