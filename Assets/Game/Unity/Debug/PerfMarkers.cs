using Unity.Profiling;

namespace Isekai
{
    /// <summary>
    /// Profiler markers on the game's own hot paths, so the Profiler window and <see cref="FrameStats"/>' spike report
    /// can say which system a slow frame spent its time in. Wrap the work: <c>using (PerfMarkers.HordeHits.Auto())</c>.
    /// </summary>
    public static class PerfMarkers
    {
        public static readonly ProfilerMarker HordeRetarget = new("Isekai.Horde.Retarget");
        public static readonly ProfilerMarker HordeCrowdJobs = new("Isekai.Horde.CrowdJobs");
        public static readonly ProfilerMarker HordeHash = new("Isekai.Horde.Hash");
        public static readonly ProfilerMarker HordeHits = new("Isekai.Horde.Hits");
        public static readonly ProfilerMarker HordeAggro = new("Isekai.Horde.Aggro");
        public static readonly ProfilerMarker WildsWander = new("Isekai.Wilds.Wander");
        public static readonly ProfilerMarker MapRedraw = new("Isekai.Map.Redraw");
        public static readonly ProfilerMarker Targeting = new("Isekai.Targeting");
        public static readonly ProfilerMarker Character = new("Isekai.Character");
        public static readonly ProfilerMarker Hud = new("Isekai.Hud");
        public static readonly ProfilerMarker Build = new("Isekai.Build");

        /// <summary>The names above, for recorders.</summary>
        public static readonly string[] Names =
        {
            "Isekai.Horde.Retarget", "Isekai.Horde.CrowdJobs", "Isekai.Horde.Hash", "Isekai.Horde.Hits", "Isekai.Horde.Aggro",
            "Isekai.Wilds.Wander", "Isekai.Map.Redraw", "Isekai.Targeting", "Isekai.Character",
            "Isekai.Hud", "Isekai.Build",
        };
    }
}
