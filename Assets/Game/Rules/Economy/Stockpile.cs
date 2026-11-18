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

        /// <summary>True if every line of <paramref name="costs"/> is affordable at once (repeated types add up).</summary>
        public bool CanAfford(IReadOnlyList<ResourceCost> costs)
        {
            var totals = new Dictionary<ResourceType, int>();
            foreach (ResourceCost c in costs)
            {
                if (c.Amount < 0) throw new ArgumentOutOfRangeException(nameof(costs));
                totals[c.Type] = (totals.TryGetValue(c.Type, out int t) ? t : 0) + c.Amount;
            }
            foreach (KeyValuePair<ResourceType, int> total in totals)
                if (!CanAfford(total.Key, total.Value)) return false;
            return true;
        }

        /// <summary>Takes every line of <paramref name="costs"/>, or nothing at all if any can't be afforded.</summary>
        public bool TrySpend(IReadOnlyList<ResourceCost> costs)
        {
            if (!CanAfford(costs)) return false;
            foreach (ResourceCost c in costs) TrySpend(c.Type, c.Amount);
            return true;
        }

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
