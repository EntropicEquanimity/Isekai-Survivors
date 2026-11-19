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

        private static IEnumerator AverageFrame(float seconds, float[] result)
        {
            float total = 0f;
            int frames = 0;
            float end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end)
            {
                yield return null;
                total += Time.unscaledDeltaTime;
                frames++;
            }
            result[0] = total / Mathf.Max(1, frames);
        }

        /// <summary>
        /// The budget: the average frame over 2 s with fog on may cost at most 1.5x the same Region with fog off, plus
        /// 2 ms of slack for a frame time that headless can make very small (a 3 ms frame doubling to 6 would
        /// otherwise fail on noise). Fog on is measured first, while the new mortals still walk and change cells,
        /// which is the heavier case for vision; fog off comes after, so the comparison favours the fog being slow.
        /// </summary>
        [UnityTest]
        public IEnumerator Vision_with_hundreds_of_mortals_stays_within_the_frame_budget()
        {
            TestDebug.Press("Spawn 200");
            TestDebug.Press("Spawn 200");
            yield return new WaitForSeconds(2f);
            Assert.Greater(RegionVision.Current.ViewerCount, 300, "the mortals are looking out");

            var on = new float[1];
            yield return AverageFrame(2f, on);
            TestDebug.Toggle("Fog of war", false);
            yield return new WaitForSeconds(0.5f);
            var off = new float[1];
            yield return AverageFrame(2f, off);

            Debug.Log($"[Vision perf] fog on {on[0] * 1000f:F2} ms, fog off {off[0] * 1000f:F2} ms, viewers {RegionVision.Current.ViewerCount}");
            Assert.LessOrEqual(on[0], off[0] * 1.5f + 0.002f, $"fog on {on[0] * 1000f:F2} ms vs fog off {off[0] * 1000f:F2} ms");
        }
    }
}
