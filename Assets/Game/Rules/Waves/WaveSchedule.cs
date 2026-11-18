using System;
using System.Collections.Generic;

namespace Isekai
{
    /// <summary>How the game brings a Wave's enemies (see CONTEXT.md).</summary>
    public enum WaveType
    {
        /// <summary>A spike of cheap Chasers from one side.</summary>
        Horde,

        /// <summary>Flyers from off-screen. Treated as Mix until flying enemies exist.</summary>
        Flight,

        /// <summary>A small clump of tougher units that arrive together.</summary>
        Squad,

        /// <summary>Spread out around the map. Default filler.</summary>
        Mix,
    }

    /// <summary>One timed attack on the player's Bases.</summary>
    [Serializable]
    public struct Wave
    {
        /// <summary>Seconds into the Region it arrives.</summary>
        public float Time;
        public WaveType Type;
        /// <summary>Enemies before escalation.</summary>
        public int Count;
        /// <summary>The Region's last Wave: defeating it (with every enemy Base gone) captures the Region.</summary>
        public bool IsFinal;

        public Wave(float time, WaveType type, int count, bool isFinal = false)
        {
            Time = time;
            Type = type;
            Count = count;
            IsFinal = isFinal;
        }
    }

    /// <summary>A Region's Waves in time order; hands out each one once when its time comes.</summary>
    public sealed class WaveSchedule
    {
        private readonly List<Wave> _waves;
        private int _next;

        public WaveSchedule(IEnumerable<Wave> waves)
        {
            _waves = new List<Wave>(waves);
            _waves.Sort((a, b) => a.Time.CompareTo(b.Time));
        }

        public int Count => _waves.Count;
        public int Released => _next;
        public bool IsFinished => _next >= _waves.Count;

        /// <summary>The next Wave not yet released, if any.</summary>
        public bool TryPeekNext(out Wave wave)
        {
            wave = _next < _waves.Count ? _waves[_next] : default;
            return _next < _waves.Count;
        }

        /// <summary>Seconds from <paramref name="now"/> until the next Wave (0 when due, infinity when none are left).</summary>
        public float SecondsUntilNext(float now) => TryPeekNext(out Wave w) ? Math.Max(0f, w.Time - now) : float.PositiveInfinity;

        /// <summary>Adds every Wave due by <paramref name="now"/> to <paramref name="due"/>, each once.</summary>
        public void Release(float now, List<Wave> due)
        {
            while (_next < _waves.Count && _waves[_next].Time <= now) due.Add(_waves[_next++]);
        }
    }
}
