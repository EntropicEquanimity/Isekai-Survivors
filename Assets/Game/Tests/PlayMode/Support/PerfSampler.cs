using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Isekai.Tests
{
    /// <summary>
    /// Per-frame samples for the *PerformanceTests: the system's own cost (read from its Stopwatch total) and the whole
    /// frame time, with warm-up frames left out. A busy machine preempts the editor now and then, which inflates single
    /// frames and so a plain mean; the median of the per-frame samples ignores those few spikes, while a system that really
    /// costs more every frame still moves it.
    /// <para>
    /// Load also slows the system's code evenly (shared caches and memory, two Unity runs at once), and a Stopwatch cannot
    /// tell the system's own time from that. The whole frame slows in step, so <see cref="LoadAllowance"/> gives each budget
    /// the room that much frame time earns it, and none on a quiet machine.
    /// </para>
    /// </summary>
    public sealed class PerfSampler
    {
        /// <summary>Frames let go before sampling starts (first-use allocations, shader and cache warm-up, a pending load).</summary>
        public const int WarmupFrames = 30;

        /// <summary>Frame time up to which a machine counts as quiet: these Region scenes run 3 to 8 ms a frame alone.</summary>
        public const float QuietFrameMs = 8f;

        /// <summary>
        /// Extra budget per millisecond of frame time past <see cref="QuietFrameMs"/>. Measured on the shared machine: a system's
        /// cost grows by 0.03 ms for each ms the frame grows (frames of 30 to 40 ms under two Unity runs), so 0.05 leaves a margin;
        /// a system that costs twice what it should still fails at any load seen.
        /// </summary>
        public const float AllowancePerFrameMs = 0.05f;

        public readonly List<float> FrameMs = new();
        public readonly List<float> CostMs = new();

        public float MedianFrameMs => Median(FrameMs);
        public float MedianCostMs => Median(CostMs);
        public float MeanCostMs => Mean(CostMs);

        /// <summary>The extra milliseconds of budget a frame time of <paramref name="frameMs"/> earns.</summary>
        public static float LoadAllowance(float frameMs) => Mathf.Max(0f, frameMs - QuietFrameMs) * AllowancePerFrameMs;

        /// <summary>
        /// Waits the warm-up frames, then samples every frame for <paramref name="seconds"/> of real time. <paramref name="totalMs"/>
        /// reads the system's running total of its own time in milliseconds. <paramref name="keep"/> adds to the samples already
        /// taken instead of starting over, so two conditions can be sampled in turns and meet the same load.
        /// </summary>
        public IEnumerator Run(float seconds, Func<double> totalMs, bool keep = false)
        {
            for (int i = 0; i < WarmupFrames; i++) yield return null;
            if (!keep)
            {
                FrameMs.Clear();
                CostMs.Clear();
            }
            double last = totalMs();
            float end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end)
            {
                yield return null;
                double now = totalMs();
                FrameMs.Add(Time.unscaledDeltaTime * 1000f);
                CostMs.Add((float)(now - last));
                last = now;
            }
        }

        public static float Median(List<float> samples)
        {
            if (samples.Count == 0) return 0f;
            var sorted = new List<float>(samples);
            sorted.Sort();
            int mid = sorted.Count / 2;
            return sorted.Count % 2 == 1 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) * 0.5f;
        }

        private static float Mean(List<float> samples)
        {
            if (samples.Count == 0) return 0f;
            float sum = 0f;
            foreach (float s in samples) sum += s;
            return sum / samples.Count;
        }
    }
}
