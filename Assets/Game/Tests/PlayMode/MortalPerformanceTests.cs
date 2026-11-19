using System.Collections;
using Blondie.FlowFields;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Isekai.Tests
{
    /// <summary>Hundreds of mortals with brains must not bring the lag back: their own update stays small, the frame time stays close to the same Region without them, and they ask for no new map-wide paths.</summary>
    public class MortalPerformanceTests
    {
        [UnitySetUp]
        public IEnumerator Load()
        {
            yield return RegionTestScene.Load();
            yield return TestWait.Until(() => RegionVision.Current != null && RegionVision.Current.IsReady, "vision starts");
            TestDebug.Toggle("God mode", true);   // a defeat would end the Region and stop the clock under test
        }

        private static IEnumerator Sample(float seconds, float[] frameMs, float[] brainMs)
        {
            double cost = MortalCost.TotalMilliseconds;
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
            brainMs[0] = (float)((MortalCost.TotalMilliseconds - cost) / Mathf.Max(1, frames));
        }

        /// <summary>
        /// The brains' own cost is the real budget: the time <see cref="RegionMortals"/> spends in its update (thinking,
        /// walking, senses; measured with a Stopwatch) averages under 1 ms a frame with 400+ mortals on top of the Region's
        /// own. Whole-frame time is noisy when the machine is shared, so that check is loose: at most 1.5x the same Region
        /// without them plus 3 ms. They think a few times a second, so the work is spread across frames.
        /// </summary>
        [UnityTest]
        public IEnumerator Hundreds_of_mortals_with_brains_stay_within_the_frame_budget_and_ask_for_no_new_paths()
        {
            var baseFrame = new float[1];
            var baseBrain = new float[1];
            yield return Sample(2f, baseFrame, baseBrain);
            int cachedBefore = FlowFields.CachedCount;
            int recycledBefore = FlowFields.RecycledCount;
            int brainsBefore = RegionMortals.Current.Brains.Count;

            TestDebug.Press("Spawn 100 mortals");
            TestDebug.Press("Spawn 100 mortals");
            TestDebug.Press("Spawn 100 mortals");
            TestDebug.Press("Spawn 100 mortals");
            yield return new WaitForSeconds(2f);
            Assert.GreaterOrEqual(RegionMortals.Current.Brains.Count, brainsBefore + 400, "the new mortals are thinking");

            var frame = new float[1];
            var brain = new float[1];
            yield return Sample(3f, frame, brain);
            Debug.Log($"[Mortal perf] {RegionMortals.Current.Brains.Count} brains: frame {frame[0]:F2} ms (without {baseFrame[0]:F2} ms), brains {brain[0]:F3} ms, fields held {RegionMortals.Current.HeldFields}, cached {FlowFields.CachedCount}");

            Assert.Less(brain[0], 1f, "the brains' own update time per frame");
            Assert.LessOrEqual(frame[0], baseFrame[0] * 1.5f + 3f, $"{frame[0]:F2} ms with brains vs {baseFrame[0]:F2} ms without");
            Assert.LessOrEqual(RegionMortals.Current.HeldFields, RegionSettlements.Current.Settlements.Count, "at most one static field a settlement");
            Assert.LessOrEqual(FlowFields.CachedCount, cachedBefore + RegionSettlements.Current.Settlements.Count + 1, "no pile of map-sized fields");
        }
    }
}
