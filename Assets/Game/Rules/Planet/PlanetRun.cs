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

        /// <summary>The Main Base fell.</summary>
        Lost,
    }

    /// <summary>
    /// One Run across a Planet: which Regions are captured, which may be entered next (the Planet's edge at first, then
    /// any Region touching a captured one), the Run clock (a display only), and the buffs and Stone captures have
    /// earned. Capturing the Capital wins; losing a Main Base loses.
    /// </summary>
    public sealed class PlanetRun
    {
        private readonly HashSet<string> _captured = new();
        private readonly List<string> _order = new();

        public PlanetRun(Planet planet) => Planet = planet ?? throw new ArgumentNullException(nameof(planet));

        public Planet Planet { get; }
        public PlanetRunState State { get; private set; } = PlanetRunState.Choosing;
        public RegionInfo Current { get; private set; }

        /// <summary>Seconds of Region play so far; keeps counting across Regions.</summary>
        public float RunSeconds { get; private set; }

        public IReadOnlyList<string> CapturedInOrder => _order;
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

        public void Lose()
        {
            if (!IsOver) State = PlanetRunState.Lost;
        }
    }
}
