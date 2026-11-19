using System;
using System.Collections.Generic;

namespace Isekai
{
    /// <summary>What a Building's destruction did to the Region.</summary>
    [Flags]
    public enum SettlementEffect
    {
        None = 0,

        /// <summary>The last Building of a settlement (other than its fence) fell: the settlement is razed.</summary>
        Razed = 1,

        /// <summary>The Seat's keep fell: its rulers are taken out and the Region is captured.</summary>
        SeatCaptured = 2,
    }

    /// <summary>
    /// What is left standing of each settlement, and the two rules that depend on it, in one place:
    /// <list type="bullet">
    /// <item>Capture: destroying the Seat's keep captures the Region. Taking out the rulers is enough; the rest of the fort, and every other place, may stand.</item>
    /// <item>Razing: a village, farm or camp is razed when every Building in it that is not fence has been destroyed (walls and towers do not count, so the horde need not chew through a palisade to finish a village). The Seat is captured, never razed.</item>
    /// </list>
    /// </summary>
    public sealed class SettlementLedger
    {
        private readonly List<PlacedSettlement> _settlements;
        private readonly bool[][] _destroyed;
        private readonly int[] _standing;
        private readonly bool[] _razed;

        public SettlementLedger(IReadOnlyList<PlacedSettlement> settlements)
        {
            _settlements = new List<PlacedSettlement>(settlements ?? throw new ArgumentNullException(nameof(settlements)));
            _destroyed = new bool[_settlements.Count][];
            _standing = new int[_settlements.Count];
            _razed = new bool[_settlements.Count];
            for (int i = 0; i < _settlements.Count; i++)
            {
                _destroyed[i] = new bool[_settlements[i].Buildings.Count];
                foreach (PlacedBuilding b in _settlements[i].Buildings)
                    if (!b.IsWall) _standing[i]++;
            }
        }

        /// <summary>The Seat's keep has been destroyed.</summary>
        public bool SeatCaptured { get; private set; }

        /// <summary>How many settlements have been razed.</summary>
        public int RazedCount { get; private set; }

        public bool IsRazed(int settlement) => _razed[settlement];

        public bool IsDestroyed(int settlement, int building) => _destroyed[settlement][building];

        /// <summary>How many of a settlement's Buildings that count towards razing are still standing.</summary>
        public int Standing(int settlement) => _standing[settlement];

        /// <summary>Records a Building's destruction (once; a second report changes nothing) and says what it did.</summary>
        public SettlementEffect Destroyed(int settlement, int building)
        {
            if (_destroyed[settlement][building]) return SettlementEffect.None;
            _destroyed[settlement][building] = true;
            PlacedSettlement s = _settlements[settlement];
            PlacedBuilding b = s.Buildings[building];
            var effect = SettlementEffect.None;
            if (s.IsSeat && b.Kind == MortalBuilding.Keep && !SeatCaptured)
            {
                SeatCaptured = true;
                effect |= SettlementEffect.SeatCaptured;
            }
            if (!b.IsWall && --_standing[settlement] == 0 && !s.IsSeat)
            {
                _razed[settlement] = true;
                RazedCount++;
                effect |= SettlementEffect.Razed;
            }
            return effect;
        }
    }
}
