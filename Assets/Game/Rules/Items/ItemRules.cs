using System;
using System.Linq;
using Blondie.Inventory;

namespace Isekai
{
    /// <summary>How long an Item lasts.</summary>
    public enum ItemLifetime
    {
        /// <summary>Kept until the Run ends.</summary>
        Run,

        /// <summary>Gone when the Region it was found in is captured.</summary>
        Region,
    }

    /// <summary>When an Item may leave the Active inventory.</summary>
    public enum ItemRemoval
    {
        /// <summary>Any time.</summary>
        Free,

        /// <summary>Only after it has been active for its swap cooldown.</summary>
        Cooldown,

        /// <summary>Not until the Region is captured (it may still be moved around inside the Active inventory).</summary>
        Locked,

        /// <summary>Can't be moved at all once placed, until the Region is captured.</summary>
        Fixed,
    }

    /// <summary>
    /// An Item's Lifetime and removal rule, added to the Item as an effect (Inspector: <i>Add Effect</i>). Items without
    /// it last the Run and move freely. <see cref="InventoryRules"/> enforces it.
    /// </summary>
    [Serializable]
    public sealed class ItemRules : ItemEffect
    {
        public ItemLifetime Lifetime = ItemLifetime.Run;
        public ItemRemoval Removal = ItemRemoval.Free;
        public float SwapCooldown = 30f;

        public static readonly ItemRules Default = new();

        /// <summary>The rules on <paramref name="item"/>, or <see cref="Default"/>.</summary>
        public static ItemRules Of(Item item) =>
            item?.Definition?.Effects.OfType<ItemRules>().FirstOrDefault() ?? Default;

        public override string Describe()
        {
            string removal = Removal switch
            {
                ItemRemoval.Cooldown => $"Can be swapped out after {SwapCooldown:0} s active",
                ItemRemoval.Locked => "Can't be removed until the Region is captured",
                ItemRemoval.Fixed => "Can't be moved once placed, until the Region is captured",
                _ => null,
            };
            string lifetime = Lifetime == ItemLifetime.Region ? "Lasts this Region only" : null;
            return removal != null && lifetime != null ? removal + ". " + lifetime : removal ?? lifetime;
        }
    }

    /// <summary>An Item that grows the Active inventory while it is active (Magicraft-style bag space).</summary>
    [Serializable]
    public sealed class BagSpace : ItemEffect
    {
        public int Columns;
        public int Rows;

        public override string Describe()
        {
            if (Columns > 0 && Rows > 0) return $"Active inventory +{Columns} wide, +{Rows} tall";
            if (Columns > 0) return $"Active inventory +{Columns} wide";
            return Rows > 0 ? $"Active inventory +{Rows} tall" : null;
        }
    }
}
