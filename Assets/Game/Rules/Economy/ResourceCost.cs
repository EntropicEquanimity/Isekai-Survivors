using System;

namespace Isekai
{
    /// <summary>An amount of one Resource, e.g. a line of a Building's price.</summary>
    [Serializable]
    public struct ResourceCost
    {
        public ResourceType Type;
        public int Amount;

        public ResourceCost(ResourceType type, int amount)
        {
            Type = type;
            Amount = amount;
        }

        public override string ToString() => $"{Amount} {Type}";
    }
}
