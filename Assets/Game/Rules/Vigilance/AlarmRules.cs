using System.Collections.Generic;
using Blondie.Core;

namespace Isekai
{
    /// <summary>The alarm's numbers in one place: bones seen, the witness's delay, the rider, spotting.</summary>
    public static class AlarmTuning
    {
        /// <summary>How many bone piles a mortal must see to raise the alarm.</summary>
        public const int PilesToAlarm = 5;

        /// <summary>Seconds between a witness seeing enough bones and its rider setting off: the time to silence it first.</summary>
        public const float WitnessDelaySeconds = 4f;

        /// <summary>How far round the witness the area is marked alerted, in cells, and for how long.</summary>
        public const int AlertRadiusCells = 24;
        public const float AlertSeconds = 90f;

        /// <summary>How long mortals in an alerted area have the Alarm factor on.</summary>
        public const float AlarmFactorSeconds = 20f;

        /// <summary>How fast a rider rides, in cells per second (peasants flee at 2.4).</summary>
        public const float RiderCellsPerSecond = 3.4f;

        /// <summary>A rider is at the Seat once it is this many cells from the keep's walls or nearer (the same reach as a fleeing peasant's).</summary>
        public const int SeatArriveMargin = 2;

        /// <summary>How long a mortal must have Jack in sight before that counts as spotting him, and the wait before the next raise.</summary>
        public const float SpotConfirmSeconds = 0.75f;
        public const float SpottedCooldownSeconds = 20f;

        /// <summary>A fleeing peasant reaching a stronghold alerts the area round it this far.</summary>
        public const int StrongholdAlertRadiusCells = 12;
    }

    /// <summary>The alarm's decisions as pure functions.</summary>
    public static class AlarmRules
    {
        /// <summary>A mortal that sees this many bone piles raises the alarm.</summary>
        public static bool Raises(int pilesSeen) => pilesSeen >= AlarmTuning.PilesToAlarm;

        /// <summary>A rider at <paramref name="cell"/> has reached the Seat whose interior is <paramref name="seatInterior"/>: inside it, or within the arrival margin of its walls.</summary>
        public static bool HasArrived(Int2 cell, CellRect seatInterior) => seatInterior.Inflate(AlarmTuning.SeatArriveMargin).Contains(cell);
    }

    /// <summary>The areas mortals have been alerted in, each for a while, so one area sends one rider and not one per witness.</summary>
    public sealed class AlertedAreas
    {
        private readonly struct Area
        {
            public Area(Int2 centre, int radius, float until)
            {
                Centre = centre;
                Radius = radius;
                Until = until;
            }

            public Int2 Centre { get; }
            public int Radius { get; }
            public float Until { get; }
        }

        private readonly List<Area> _areas = new();

        public int Count => _areas.Count;

        /// <summary>Marks the area within <paramref name="radius"/> cells of <paramref name="centre"/> alerted until <paramref name="until"/> (game seconds).</summary>
        public void Mark(Int2 centre, int radius, float until) => _areas.Add(new Area(centre, radius, until));

        /// <summary>Whether <paramref name="cell"/> is in an area still alerted at <paramref name="now"/>; forgets the ones that ran out.</summary>
        public bool Contains(Int2 cell, float now)
        {
            bool found = false;
            for (int i = _areas.Count - 1; i >= 0; i--)
            {
                Area a = _areas[i];
                if (a.Until <= now) { _areas.RemoveAt(i); continue; }
                int dx = cell.x - a.Centre.x, dy = cell.y - a.Centre.y;
                if (dx * dx + dy * dy <= a.Radius * a.Radius) found = true;
            }
            return found;
        }

        public void Clear() => _areas.Clear();
    }

    /// <summary>Lets one kind of being spotted raise Vigilance at most once per cooldown, so standing in sight is not a raise per frame.</summary>
    public sealed class SpotLimiter
    {
        private readonly float _cooldown;
        private float _readyAt = float.NegativeInfinity;

        public SpotLimiter(float cooldownSeconds) => _cooldown = cooldownSeconds;

        /// <summary>True (and starts the cooldown) when a raise is allowed at <paramref name="now"/>.</summary>
        public bool TryRaise(float now)
        {
            if (now < _readyAt) return false;
            _readyAt = now + _cooldown;
            return true;
        }
    }
}
