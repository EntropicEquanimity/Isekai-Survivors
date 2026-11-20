using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Isekai.Tests
{
    /// <summary>Keeps the shared flipbook animator within budget when the Region is crowded.</summary>
    public class UnitAnimationPerformanceTests
    {
        [UnitySetUp]
        public IEnumerator Load()
        {
            yield return RegionTestScene.Load();
            yield return TestWait.Until(() => RegionBootstrap.Current.IsPopulated, "the Region is ready");
            TestDebug.Toggle("God mode", true);
        }

        private static IEnumerator Sample(float seconds, float[] frameMs, float[] animationMs)
        {
            double startCost = UnitAnimationCost.TotalMilliseconds;
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
            animationMs[0] = (float)((UnitAnimationCost.TotalMilliseconds - startCost) / Mathf.Max(1, frames));
        }

        [UnityTest]
        public IEnumerator Shared_unit_flipbook_animation_stays_within_budget_with_hundreds_of_units()
        {
            TestDebug.Press("Spawn 200");
            TestDebug.Press("Spawn 200");
            yield return new WaitForSeconds(2f);

            UnitFlipbookAnimator animator = UnitFlipbookAnimator.Current;
            Assert.IsNotNull(animator, "animation system exists");
            Assert.Greater(animator.TrackedCount, 400, "the test runs with 400+ animated units");

            var frame = new float[1];
            var animation = new float[1];
            yield return Sample(3f, frame, animation);

            float budget = 0.6f + Mathf.Max(0f, frame[0] - 5f) * 0.03f;
            Debug.Log($"[Animation perf] tracked {animator.TrackedCount}, frame {frame[0]:F2} ms, animator {animation[0]:F3} ms");
            Assert.Less(animation[0], budget, $"shared animation update time per frame (budget {budget:F3} ms at frame {frame[0]:F2} ms)");
        }
    }
}
