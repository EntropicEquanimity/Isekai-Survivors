using System;
using System.Collections.Generic;

namespace Isekai
{
    /// <summary>
    /// Who a settlement's spawn marker produces: for a kind of settlement and a kind of marker, a weighted choice of
    /// mortal types. Peasants come from the markers of farms and villages, guards from villages, camps and the Seat,
    /// priests from villages and the Seat. The better troops (Knight, Lancer) only stand in the Seat.
    /// </summary>
    public static class MortalSpawns
    {
        public readonly struct Choice
        {
            public Choice(MortalType type, int weight)
            {
                Type = type;
                Weight = weight;
            }

            public MortalType Type { get; }
            public int Weight { get; }
        }

        private static readonly Dictionary<(SettlementKind, MarkerKind), Choice[]> s_table = new()
        {
            [(SettlementKind.Farm, MarkerKind.Peasant)] = new[] { new Choice(MortalType.Peasant, 1) },
            [(SettlementKind.Village, MarkerKind.Peasant)] = new[] { new Choice(MortalType.Peasant, 1) },

            [(SettlementKind.Village, MarkerKind.Guard)] = new[]
            {
                new Choice(MortalType.Soldier, 4), new Choice(MortalType.Swordsman, 3), new Choice(MortalType.Archer, 3),
            },
            [(SettlementKind.Camp, MarkerKind.Guard)] = new[]
            {
                new Choice(MortalType.Soldier, 4), new Choice(MortalType.Swordsman, 2), new Choice(MortalType.Archer, 2), new Choice(MortalType.ArmoredAxeman, 2),
            },
            [(SettlementKind.Seat, MarkerKind.Guard)] = new[]
            {
                new Choice(MortalType.Soldier, 3), new Choice(MortalType.ArmoredAxeman, 2), new Choice(MortalType.Knight, 2),
                new Choice(MortalType.Lancer, 1), new Choice(MortalType.Archer, 1),
            },

            [(SettlementKind.Village, MarkerKind.Priest)] = new[] { new Choice(MortalType.Priest, 1) },
            [(SettlementKind.Seat, MarkerKind.Priest)] = new[] { new Choice(MortalType.Priest, 1) },
        };

        /// <summary>What a marker of <paramref name="marker"/> in a settlement of <paramref name="settlement"/> can produce (empty when it produces nothing).</summary>
        public static IReadOnlyList<Choice> Choices(SettlementKind settlement, MarkerKind marker) =>
            s_table.TryGetValue((settlement, marker), out Choice[] choices) ? choices : Array.Empty<Choice>();

        /// <summary>
        /// The type for a marker, picked by <paramref name="roll"/> in [0, 1) over the weights; false when the marker
        /// produces nothing there.
        /// </summary>
        public static bool TryPick(SettlementKind settlement, MarkerKind marker, double roll, out MortalType type)
        {
            IReadOnlyList<Choice> choices = Choices(settlement, marker);
            type = default;
            if (choices.Count == 0) return false;
            int total = 0;
            foreach (Choice c in choices) total += c.Weight;
            double at = Math.Min(Math.Max(roll, 0d), 0.999999d) * total;
            foreach (Choice c in choices)
            {
                at -= c.Weight;
                if (at < 0d) { type = c.Type; return true; }
            }
            type = choices[choices.Count - 1].Type;
            return true;
        }

        /// <summary>The kind of marker a type comes from (the markers peasants, guards and priests stand at).</summary>
        public static MarkerKind MarkerOf(MortalType type) => MortalProfiles.For(type).Role switch
        {
            MortalRole.Civilian => MarkerKind.Peasant,
            MortalRole.Priest => MarkerKind.Priest,
            _ => MarkerKind.Guard,
        };
    }
}
