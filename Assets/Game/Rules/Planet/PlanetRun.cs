using System;
using System.Collections.Generic;
using System.Linq;

namespace Isekai
{
    public enum PlanetRunState
    {
        /// <summary>On the Planet map, choosing where to go.</summary>
        Choosing,

        /// <summary>Playing a Region.</summary>
        InRegion,

        /// <summary>The Capital was captured.</summary>
        Won,

        /// <summary>Jack ran out of Souls while bodiless.</summary>
        Lost,
    }

    /// <summary>
    /// One Run across a Planet: which Regions are captured, which may be entered next (the Planet's edge at first, then
    /// any Region touching a captured one), the Run clock (a display only), and the buffs and Stone captures have
    /// earned. Capturing the Capital wins; running out of Souls while bodiless loses (losing the Anchor does not).
    /// </summary>
    public sealed class PlanetRun
    {
        private readonly HashSet<string> _captured = new();
        private readonly List<string> _order = new();
        private readonly List<EscapedRuler> _escapedRulers = new();

        public PlanetRun(Planet planet) => Planet = planet ?? throw new ArgumentNullException(nameof(planet));

        public Planet Planet { get; }
        public PlanetRunState State { get; private set; } = PlanetRunState.Choosing;
        public RegionInfo Current { get; private set; }

        /// <summary>Seconds of Region play so far; keeps counting across Regions.</summary>
        public float RunSeconds { get; private set; }

        public IReadOnlyList<string> CapturedInOrder => _order;
        public IReadOnlyList<EscapedRuler> EscapedRulers => _escapedRulers;

        /// <summary>Rulers killed with their Seats over the Run (an escaped ruler is in <see cref="EscapedRulers"/> instead).</summary>
        public int RulersSlain { get; private set; }
        public bool IsCaptured(RegionInfo region) => region != null && _captured.Contains(region.Id);
        public bool IsOver => State is PlanetRunState.Won or PlanetRunState.Lost;

        /// <summary>Buff ids earned so far, in the order the Regions were captured.</summary>
        public IEnumerable<string> Buffs => _order.Select(id => Planet[id].BuffId).Where(b => !string.IsNullOrEmpty(b));

        /// <summary>Stone the next Region starts with on top of its own, from every capture so far.</summary>
        public int BonusStone => _order.Sum(id => Planet[id].BonusStone);

        /// <summary>The Regions that may be entered now.</summary>
        public IEnumerable<RegionInfo> Available()
        {
            if (IsOver) return Enumerable.Empty<RegionInfo>();
            if (_captured.Count == 0) return Planet.Edge;
            return Planet.Regions.Where(r => !_captured.Contains(r.Id) && r.Neighbours.Any(_captured.Contains));
        }

        public bool CanEnter(RegionInfo region) => State == PlanetRunState.Choosing && Available().Contains(region);

        public bool Enter(RegionInfo region)
        {
            if (!CanEnter(region)) return false;
            Current = region;
            State = PlanetRunState.InRegion;
            return true;
        }

        public void Tick(float seconds)
        {
            if (State == PlanetRunState.InRegion && seconds > 0f) RunSeconds += seconds;
        }

        /// <summary>The current Region is captured: back to the map, or the Run is won if it was the Capital.</summary>
        public void CaptureCurrent()
        {
            if (State != PlanetRunState.InRegion || Current == null) return;
            if (_captured.Add(Current.Id)) _order.Add(Current.Id);
            State = Current.IsCapital ? PlanetRunState.Won : PlanetRunState.Choosing;
        }

        /// <summary>A ruler fell with their Seat (also puts a saved count back: <paramref name="count"/>).</summary>
        public void RecordRulersSlain(int count = 1)
        {
            if (count > 0) RulersSlain += count;
        }

        public void Lose()
        {
            if (!IsOver) State = PlanetRunState.Lost;
        }

        /// <summary>
        /// Where an escaped ruler from <paramref name="from"/> retreats: an uncaptured Region one ring deeper when one
        /// is left, else the Capital.
        /// </summary>
        public RegionInfo PickRetreatTarget(RegionInfo from)
        {
            if (Planet == null) return null;
            if (from == null) return Planet.Capital;
            int wantedDepth = from.Depth + 1;
            RegionInfo best = null;
            float bestAngle = float.MaxValue;
            foreach (RegionInfo region in Planet.Regions)
            {
                if (region == null || region.IsCapital || _captured.Contains(region.Id) || region.Depth != wantedDepth) continue;
                float angle = AngleBetween(region.Angle, from.Angle);
                if (best == null || angle < bestAngle || (Math.Abs(angle - bestAngle) < 0.0001f && string.CompareOrdinal(region.Id, best.Id) < 0))
                {
                    best = region;
                    bestAngle = angle;
                }
            }

            if (best != null) return best;
            return Planet.Capital;
        }

        /// <summary>Records a ruler escape on this Run; false when the source or destination Region does not belong to this Planet.</summary>
        public bool RecordEscapedRuler(RegionInfo from, RegionInfo to, string name, IReadOnlyList<TraitId> traits)
        {
            if (from == null || to == null || Planet[from.Id] == null || Planet[to.Id] == null) return false;
            TraitId[] copied = traits != null && traits.Count > 0 ? traits.ToArray() : Array.Empty<TraitId>();
            for (int i = _escapedRulers.Count - 1; i >= 0; i--)
                if (_escapedRulers[i].FromRegionId == from.Id) _escapedRulers.RemoveAt(i);
            _escapedRulers.Add(new EscapedRuler(from.Id, to.Id, name, copied));
            return true;
        }

        /// <summary>The escaped rulers that fled to <paramref name="destination"/> (empty when none).</summary>
        public IEnumerable<EscapedRuler> EscapedRulersAt(RegionInfo destination)
        {
            if (destination == null) return Enumerable.Empty<EscapedRuler>();
            return _escapedRulers.Where(r => r.ToRegionId == destination.Id);
        }

        private static float AngleBetween(float a, float b)
        {
            float d = Math.Abs(a - b) % (float)(Math.PI * 2);
            return d > Math.PI ? (float)(Math.PI * 2) - d : d;
        }
    }
}
