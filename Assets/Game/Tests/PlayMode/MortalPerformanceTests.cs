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

        /// <summary>
        /// The brains' own cost is the real budget: the time <see cref="RegionMortals"/> spends in its update (thinking,
        /// walking, senses; measured with a Stopwatch) has a median under 1 ms a frame (per-frame samples, warm-up frames left out; a loaded machine earns a little more, see PerfSampler.LoadAllowance) with 400+ mortals on top of the Region's
        /// own. Whole-frame time is noisy when the machine is shared, so that check compares medians and is loose: at most 1.5x the same Region
        /// without them plus 3 ms. They think a few times a second, so the work is spread across frames.
        /// </summary>
        [UnityTest]
        public IEnumerator Hundreds_of_mortals_with_brains_stay_within_the_frame_budget_and_ask_for_no_new_paths()
        {
            var empty = new PerfSampler();
            yield return empty.Run(2f, () => MortalCost.TotalMilliseconds);
            int cachedBefore = FlowFields.CachedCount;
            int recycledBefore = FlowFields.RecycledCount;
            int brainsBefore = RegionMortals.Current.Brains.Count;

            TestDebug.Press("Spawn 100 mortals");
            TestDebug.Press("Spawn 100 mortals");
            TestDebug.Press("Spawn 100 mortals");
            TestDebug.Press("Spawn 100 mortals");
            yield return new WaitForSeconds(2f);
            Assert.GreaterOrEqual(RegionMortals.Current.Brains.Count, brainsBefore + 400, "the new mortals are thinking");

            var crowd = new PerfSampler();
            yield return crowd.Run(3f, () => MortalCost.TotalMilliseconds);
            float budget = 1f + PerfSampler.LoadAllowance(crowd.MedianFrameMs);
            Debug.Log($"[Mortal perf] {RegionMortals.Current.Brains.Count} brains: frame {crowd.MedianFrameMs:F2} ms (without {empty.MedianFrameMs:F2} ms), brains median {crowd.MedianCostMs:F3} ms (mean {crowd.MeanCostMs:F3} ms, budget {budget:F3} ms), fields held {RegionMortals.Current.HeldFields}, cached {FlowFields.CachedCount}");

            Assert.Less(crowd.MedianCostMs, budget, $"the brains' own update time per frame (median; budget {budget:F3} ms at frame {crowd.MedianFrameMs:F2} ms)");
            Assert.LessOrEqual(crowd.MedianFrameMs, empty.MedianFrameMs * 1.5f + 3f, $"{crowd.MedianFrameMs:F2} ms with brains vs {empty.MedianFrameMs:F2} ms without");
            Assert.LessOrEqual(RegionMortals.Current.HeldFields, RegionSettlements.Current.Settlements.Count, "at most one static field a settlement");
            Assert.LessOrEqual(FlowFields.CachedCount, cachedBefore + RegionSettlements.Current.Settlements.Count + 1, "no pile of map-sized fields");
        }
    }
}
