using System;
using System.Linq;
using Blondie.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Isekai
{
    /// <summary>How a Region ended.</summary>
    public enum RegionEnd { Captured, Defeated }

    /// <summary>
    /// A scene component that reacts to the Region ending (stop spawning, stop taking orders, stop building). Each
    /// system answers for itself, so whoever ends the Region needs no list of what to switch off.
    /// </summary>
    public interface IRegionEndListener
    {
        void OnRegionEnded(RegionEnd end);
    }

    /// <summary>The Region's ending, announced once: every <see cref="IRegionEndListener"/> in the scene, then <see cref="Ended"/>.</summary>
    public static class RegionEvents
    {
        static RegionEvents() => StaticReset.Register(Reset);

        public static bool HasEnded { get; private set; }
        public static RegionEnd Outcome { get; private set; }

        /// <summary>After every listener in the scene. Cleared when a Region starts.</summary>
        public static event Action<RegionEnd> Ended;

        /// <summary>Ends the Region (only the first call counts).</summary>
        public static void End(RegionEnd outcome)
        {
            if (HasEnded) return;
            HasEnded = true;
            Outcome = outcome;
            BLog.Info("Region", $"Region ended: {outcome}.");
            foreach (IRegionEndListener listener in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude).OfType<IRegionEndListener>().ToArray())
                listener.OnRegionEnded(outcome);
            Ended?.Invoke(outcome);
        }

        /// <summary>A new Region is starting.</summary>
        public static void Reset()
        {
            HasEnded = false;
            Ended = null;
        }
    }
}
