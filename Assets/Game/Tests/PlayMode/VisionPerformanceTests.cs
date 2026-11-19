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

        private static IEnumerator Sample(float seconds, float[] frameMs, float[] visionMs)
        {
            double cost = VisionCost.TotalMilliseconds;
            float total = 0f;
            int frames = 0;
            float end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end)
            {
                yield return null;
                total += Time.unscaledDeltaTime;
                frames++;
            }
            frameMs[0] = total / Mathf.Max(1, frames) * 1000f;
            visionMs[0] = (float)((VisionCost.TotalMilliseconds - cost) / Mathf.Max(1, frames));
        }

        /// <summary>
        /// Fog's own cost is the real budget: the time its systems spend in their updates (measured with a Stopwatch
        /// round each update) averages under 1 ms a frame with 400+ mortals on top of the horde. Whole-frame time
        /// is noisy when the machine is shared, so that check is loose: fog on at most 1.5x fog off plus 3 ms.
        /// Fog on is sampled first, while the new mortals still walk and change cells (the heavier case).
        /// </summary>
        [UnityTest]
        public IEnumerator Vision_with_hundreds_of_mortals_stays_within_the_frame_budget()
        {
            TestDebug.Press("Spawn 200");
            TestDebug.Press("Spawn 200");
            yield return new WaitForSeconds(2f);
            Assert.Greater(RegionVision.Current.ViewerCount, 400, "the mortals are looking out");

            var onFrame = new float[1];
            var onVision = new float[1];
            yield return Sample(2f, onFrame, onVision);
            TestDebug.Toggle("Fog of war", false);
            yield return new WaitForSeconds(0.5f);
            var offFrame = new float[1];
            var offVision = new float[1];
            yield return Sample(2f, offFrame, offVision);

            Debug.Log($"[Vision perf] fog on {onFrame[0]:F2} ms (vision {onVision[0]:F3} ms), fog off {offFrame[0]:F2} ms, viewers {RegionVision.Current.ViewerCount}");
            Assert.Less(onVision[0], 1f, "vision's own update time per frame");
            Assert.AreEqual(0f, offVision[0], 1e-4f, "nothing runs with fog off");
            Assert.LessOrEqual(onFrame[0], offFrame[0] * 1.5f + 3f, $"fog on {onFrame[0]:F2} ms vs fog off {offFrame[0]:F2} ms");
        }
    }
}
