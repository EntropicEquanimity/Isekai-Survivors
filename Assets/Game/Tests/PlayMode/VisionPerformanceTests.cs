using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Isekai.Tests
{
    /// <summary>Fog of war must not bring the lag back: with the horde in the Region and a few hundred more mortals on top, the frame time stays close to the same scene without fog.</summary>
    public class VisionPerformanceTests
    {
        [UnitySetUp]
        public IEnumerator Load()
        {
            yield return RegionTestScene.Load();
            yield return TestWait.Until(() => RegionBootstrap.Current.IsPopulated, "the Region is ready");
            yield return TestWait.Until(() => RegionVision.Current != null && RegionVision.Current.IsReady, "vision starts");
            TestDebug.Toggle("God mode", true);   // a defeat would end the Region and stop the clock under test
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            RegionVision.IsOn = true;
            Time.timeScale = 1f;
            yield return null;
        }

        /// <summary>
        /// Fog's own cost is the real budget: the time its systems spend in their updates (measured with a Stopwatch
        /// round each update) has a median under 1 ms a frame (per-frame samples, warm-up frames left out, so a preempted frame under load does not count; a loaded machine earns a little more, see PerfSampler.LoadAllowance) with 400+ mortals on top of the horde. Whole-frame time
        /// is noisy when the machine is shared, so that check compares medians and is loose: fog on at most 1.5x fog off plus 3 ms.
        /// Fog on and off are sampled in turns, on first, while the new mortals still walk and change cells (the heavier case).
        /// </summary>
        [UnityTest]
        public IEnumerator Vision_with_hundreds_of_mortals_stays_within_the_frame_budget()
        {
            TestDebug.Press("Spawn 200");
            TestDebug.Press("Spawn 200");
            yield return new WaitForSeconds(2f);
            Assert.Greater(RegionVision.Current.ViewerCount, 400, "the mortals are looking out");

            // Fog on and off in turns, three rounds, so both meet the same load on a shared machine (on first, while the new
            // mortals still walk and change cells: the heavier case).
            var on = new PerfSampler();
            var off = new PerfSampler();
            for (int round = 0; round < 3; round++)
            {
                TestDebug.Toggle("Fog of war", true);
                yield return on.Run(1.5f, () => VisionCost.TotalMilliseconds, keep: true);
                TestDebug.Toggle("Fog of war", false);
                yield return new WaitForSeconds(0.3f);
                yield return off.Run(1.5f, () => VisionCost.TotalMilliseconds, keep: true);
            }

            float budget = 1f + PerfSampler.LoadAllowance(on.MedianFrameMs);
            Debug.Log($"[Vision perf] fog on {on.MedianFrameMs:F2} ms (vision median {on.MedianCostMs:F3} ms, mean {on.MeanCostMs:F3} ms, budget {budget:F3} ms), fog off {off.MedianFrameMs:F2} ms, viewers {RegionVision.Current.ViewerCount}");
            Assert.Less(on.MedianCostMs, budget, $"vision's own update time per frame (median; budget {budget:F3} ms at frame {on.MedianFrameMs:F2} ms)");
            Assert.AreEqual(0f, off.MeanCostMs, 1e-4f, "nothing runs with fog off");
            Assert.LessOrEqual(on.MedianFrameMs, off.MedianFrameMs * 1.5f + 3f, $"fog on {on.MedianFrameMs:F2} ms vs fog off {off.MedianFrameMs:F2} ms");
        }
    }
}
