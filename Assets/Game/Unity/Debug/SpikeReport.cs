using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Profiling;

namespace Isekai
{
    /// <summary>
    /// Says where a slow frame went: profiler recorders on the game's own markers (<see cref="PerfMarkers"/>) and the
    /// engine's main phases (scripts, physics, rendering, garbage collection), read for the frame that just ended.
    /// Recorders exist in the Editor and development builds; elsewhere the report is just the frame time.
    /// </summary>
    public sealed class SpikeReport : IDisposable
    {
        private static readonly (ProfilerCategory category, string name)[] Engine =
        {
            (ProfilerCategory.Scripts, "BehaviourUpdate"),
            (ProfilerCategory.Scripts, "LateBehaviourUpdate"),
            (ProfilerCategory.Scripts, "FixedBehaviourUpdate"),
            (ProfilerCategory.Scripts, "CoroutinesDelayedCalls"),
            (ProfilerCategory.Scripts, "FlowFieldCrowd.Update"),
            (ProfilerCategory.Scripts, "FlowFieldCrowd.WaitForJobs"),
            (ProfilerCategory.Physics, "Physics2D.Simulate"),
            (ProfilerCategory.Physics, "Physics2D.FindNewContacts"),
            (ProfilerCategory.Render, "PlayerEndOfFrame"),
            (ProfilerCategory.Render, "UIElementsUpdatePanels"),
            (ProfilerCategory.Render, "Gfx.WaitForPresentOnGfxThread"),
            (ProfilerCategory.Internal, "GC.Collect"),
            (ProfilerCategory.Internal, "Loading.ReadObject"),
        };

        private readonly List<(string name, ProfilerRecorder recorder)> _timers = new();
        private ProfilerRecorder _gcAlloc;

        public SpikeReport()
        {
            foreach (string name in PerfMarkers.Names) Add(ProfilerCategory.Scripts, name);
            foreach ((ProfilerCategory category, string name) in Engine) Add(category, name);
            _gcAlloc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
        }

        private void Add(ProfilerCategory category, string name)
        {
            ProfilerRecorder r = ProfilerRecorder.StartNew(category, name);
            if (r.Valid) _timers.Add((name, r));
        }

        /// <summary>The previous frame's biggest costs (milliseconds) and its garbage, as one line.</summary>
        public string Describe(int top = 7)
        {
            var parts = _timers
                .Select(t => (t.name, ms: t.recorder.LastValue / 1e6))
                .Where(t => t.ms >= 0.3)
                .OrderByDescending(t => t.ms)
                .Take(top)
                .Select(t => $"{Short(t.name)} {t.ms:F1}");
            var text = new StringBuilder(string.Join(", ", parts));
            if (_gcAlloc.Valid) text.Append($" | alloc {_gcAlloc.LastValue / 1024f:F0} KB");
            return text.Length > 0 ? text.ToString() : "(no profiler data)";
        }

        private static string Short(string name) => name.StartsWith("Isekai.", StringComparison.Ordinal) ? name.Substring(7) : name;

        public void Dispose()
        {
            foreach ((_, ProfilerRecorder r) in _timers) r.Dispose();
            _gcAlloc.Dispose();
        }
    }
}
