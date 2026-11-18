using System;

namespace Isekai
{
    /// <summary>
    /// The longer a Run takes, the stronger the enemies: every minute of Run time adds <see cref="PerMinute"/> to a
    /// multiplier on enemy counts and health, and each step closer to the Capital adds <see cref="PerDepth"/>.
    /// Higher difficulty levels raise both rates.
    /// </summary>
    public sealed class Escalation
    {
        public float PerMinute { get; set; } = 0.05f;
        public float PerDepth { get; set; } = 0.25f;
        public float Max { get; set; } = 6f;

        /// <param name="runSeconds">Time since the Run started (it keeps counting across Regions).</param>
        /// <param name="depth">Steps from the starting edge toward the Capital (0 at the edge).</param>
        public float Multiplier(float runSeconds, int depth) =>
            Math.Min(Max, 1f + Math.Max(0f, runSeconds) / 60f * PerMinute + Math.Max(0, depth) * PerDepth);

        /// <summary>A Wave's enemy count after escalation, rounded to the nearest enemy and never below 1.</summary>
        public static int Scale(int count, float multiplier) => count <= 0 ? 0 : Math.Max(1, (int)Math.Round(count * multiplier));
    }
}
