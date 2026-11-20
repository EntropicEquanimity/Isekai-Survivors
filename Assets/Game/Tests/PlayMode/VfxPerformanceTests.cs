using System.Collections;
using System.Linq;
using Blondie.Combat;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Isekai.Tests
{
    /// <summary>A horde fight with hundreds of deaths must not cost frame time: the effects' own handlers and updates stay under 0.5 ms a frame and the pool never grows past its cap.</summary>
    public class VfxPerformanceTests
    {
        private const float BudgetMs = 0.5f;

        [UnitySetUp]
        public IEnumerator Load()
        {
            yield return RegionTestScene.Load();
            yield return TestWait.Until(() => RegionVision.Current != null && RegionVision.Current.IsReady, "vision starts");
            TestDebug.Toggle("God mode", true);   // a defeat would end the Region and stop the clock under test
            yield return TestWait.Frames(3);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = 1f;
            yield return null;
        }

        /// <summary>
        /// Hundreds of mortals around Jack, then for a few seconds every frame: dozens of deaths and cues and real hits from
        /// Jack (flashes and shake) on top of the fog edge. The VFX system's own time, measured round every handler and
        /// update, has a median under 0.5 ms a frame (a loaded machine earns a little more, see PerfSampler.LoadAllowance), and the pool never exceeds its cap.
        /// </summary>
        [UnityTest]
        public IEnumerator A_horde_fight_with_hundreds_of_deaths_stays_within_the_VFX_budget()
        {
            VfxPlayer vfx = VfxPlayer.Current;
            Assert.IsNotNull(vfx);
            Vector3 jack = Player.Character.transform.position;
            CombatUnit jackUnit = Player.Character.GetComponent<Damageable>().Unit;

            TestDebug.Press("Spawn 200");
            TestDebug.Press("Spawn 200");
            yield return new WaitForSeconds(1f);

            var sampler = new PerfSampler();   // per-frame samples, judged by the median, the first frames (warm-up) left out
            double cost = VfxCost.TotalMilliseconds;
            int frames = 0, deaths = 0, peak = 0;
            float end = Time.realtimeSinceStartup + 3f;
            while (Time.realtimeSinceStartup < end)
            {
                var alive = Object.FindObjectsByType<HordeMember>(FindObjectsInactive.Exclude).Where(m => m.IsAlive).ToArray();
                // Hits from Jack, and a wave of deaths in the same frame.
                for (int i = 0; i < alive.Length && i < 40; i++)
                    alive[i].Damageable.Unit.TakeDamage(1f, DamageRules.TrueType, jackUnit);
                for (int i = 40; i < alive.Length && i < 100; i++) { alive[i].Damageable.Kill(); deaths++; }
                for (int i = 0; i < 80; i++) Feedback.Raise(Cue.Death, jack + new Vector3(i % 9 - 4, i / 9 - 4, 0f));
                deaths += 80;
                Feedback.Raise(Cue.CorruptionSpread, jack);
                if (frames % 10 == 0) HordeDebug.SpawnNearCharacter(60);
                yield return null;
                frames++;
                double total = VfxCost.TotalMilliseconds;
                if (frames > PerfSampler.WarmupFrames)
                {
                    sampler.FrameMs.Add(Time.unscaledDeltaTime * 1000f);
                    sampler.CostMs.Add((float)(total - cost));
                }
                cost = total;
                peak = Mathf.Max(peak, vfx.ActiveCount);
                Assert.LessOrEqual(vfx.ActiveCount, VfxPlayer.PoolSize, "the pool cap holds every frame");
            }
            float perFrame = sampler.MedianCostMs;
            float budget = BudgetMs + PerfSampler.LoadAllowance(sampler.MedianFrameMs);

            Debug.Log($"[Vfx perf] {perFrame:F3} ms a frame (median, mean {sampler.MeanCostMs:F3}, budget {budget:F3}) over {frames} frames, {deaths} deaths, peak {peak} effects, spawned {vfx.Spawned}, dropped by cap {vfx.DroppedByCap}, flashes skipped {vfx.Flashes.Skipped}");
            Assert.Greater(sampler.CostMs.Count, 5, "frames were sampled after the warm-up");
            Assert.Greater(deaths, 400, "a fight with hundreds of deaths");
            Assert.Greater(vfx.Spawned, 0, "and the effects played");
            Assert.Less(perFrame, budget, $"the VFX system's own cost per frame (median, ms; budget {budget:F3} at frame {sampler.MedianFrameMs:F1} ms)");
        }
    }
}
