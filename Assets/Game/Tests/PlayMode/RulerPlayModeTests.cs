using System.Collections;
using System.Linq;
using Blondie.Combat;
using Blondie.Core;
using Blondie.GameSetup;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Isekai.Tests
{
    /// <summary>The Seat's ruler: spawn, escape, capture summary, and keeping escape records across saves.</summary>
    public class RulerPlayModeTests
    {
        private const float Timeout = 25f;

        [UnitySetUp]
        public IEnumerator LoadRegion()
        {
            RunSession.Clear();
            yield return RegionTestScene.Load();
            yield return TestWait.Until(() => RegionRuler.Current != null && RegionRuler.Current.Ruler != null, "the ruler spawns", Timeout);
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            Time.timeScale = 1f;
            RunSession.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator The_ruler_spawns_at_the_Seat_with_2_or_3_hero_traits()
        {
            RegionRuler ruler = RegionRuler.Current;
            MortalBrain brain = ruler.Ruler;
            Assert.IsNotNull(brain);
            Assert.IsTrue(brain.Settlement.IsSeat, "the ruler belongs to the Seat");
            Assert.IsTrue((brain.transform.position - RegionSettlements.Current.Seat.Keep.transform.position).magnitude < 10f, "the ruler stands near the keep");

            yield return TestWait.Until(() =>
            {
                UnitTraits traits = UnitTraits.Of(brain.gameObject);
                return traits != null && traits.Traits.Count >= TraitTuning.HeroMin;
            }, "hero Traits rolled", Timeout);

            UnitTraits own = UnitTraits.Of(brain.gameObject);
            Assert.AreEqual(TraitRank.Hero, own.Rank);
            Assert.GreaterOrEqual(own.Traits.Count, TraitTuning.HeroMin);
            Assert.LessOrEqual(own.Traits.Count, TraitTuning.HeroMax);
        }

        [UnityTest]
        public IEnumerator Destroying_the_Seat_still_captures_even_after_the_ruler_escapes()
        {
            yield return EscapeRuler();
            Assert.IsTrue(RegionRuler.Current.HasEscaped, "the ruler reached the edge and escaped");

            TestDebug.Press("Destroy the Seat");
            yield return TestWait.Until(() => RegionEvents.HasEnded && RegionEvents.Outcome == RegionEnd.Captured, "the Region is captured", Timeout);
            Assert.IsTrue(RegionSettlements.Current.SeatCaptured);

            yield return null;
            RulerCaptureSummaryPanel panel = RulerCaptureSummaryPanel.Current;
            Assert.IsNotNull(panel);
            Assert.IsTrue(panel.IsVisible, "the capture summary shows");
            StringAssert.Contains("escaped", panel.TitleText.ToLowerInvariant());
            StringAssert.Contains("Traits:", panel.DetailText);
        }

        [UnityTest]
        public IEnumerator A_ruler_that_reaches_the_edge_is_recorded_with_destination_and_traits()
        {
            PlanetRun run = RunSession.Run;
            RegionInfo from = run.Current;
            yield return EscapeRuler();
            RegionRuler ruler = RegionRuler.Current;
            Assert.IsTrue(ruler.HasEscaped);
            Assert.IsNotNull(ruler.EscapedTo);

            Assert.AreEqual(1, run.EscapedRulers.Count, "one escaped ruler record");
            EscapedRuler escaped = run.EscapedRulers[0];
            Assert.AreEqual(from.Id, escaped.FromRegionId);
            Assert.AreEqual(ruler.EscapedTo.Id, escaped.ToRegionId);
            CollectionAssert.AreEqual(ruler.RulerTraits, escaped.Traits, "the traits are the ones the player saw");
            RegionInfo destination = run.Planet[escaped.ToRegionId];
            Assert.IsTrue(destination.IsCapital || destination.Depth == from.Depth + 1, "retreats one ring inward, else the Capital");
        }

        [UnityTest]
        public IEnumerator Escaped_ruler_records_survive_save_and_continue()
        {
            yield return EscapeRuler();
            TestDebug.Press("Destroy the Seat");
            yield return TestWait.Until(() => SceneManager.GetActiveScene().name == RunSession.PlanetScene && Object.FindAnyObjectByType<PlanetScreen>()?.Map != null, "back on the Planet screen", Timeout);
            Assert.IsTrue(RunSave.Exists, "the Run is saved on the Planet screen");

            EscapedRuler before = RunSession.Run.EscapedRulers.Single();
            RunSession.Clear();
            EventCenter.Publish(new PlayRequested(RunSave.Slot));
            SceneManager.LoadScene(RunSession.PlanetScene);
            yield return TestWait.Until(() => Object.FindAnyObjectByType<PlanetScreen>()?.Map != null, "continued Run loaded", Timeout);

            EscapedRuler after = RunSession.Run.EscapedRulers.Single();
            Assert.AreEqual(before.FromRegionId, after.FromRegionId);
            Assert.AreEqual(before.ToRegionId, after.ToRegionId);
            Assert.AreEqual(before.Name, after.Name);
            CollectionAssert.AreEqual(before.Traits, after.Traits);
        }

        private static IEnumerator EscapeRuler()
        {
            RegionRuler ruler = RegionRuler.Current;
            ruler.EscapeSpeedCells = 28f;
            yield return TestWait.Until(() =>
            {
                UnitTraits traits = UnitTraits.Of(ruler.Ruler.gameObject);
                return traits != null && traits.Traits.Count >= 2;
            }, "ruler Traits rolled", 8f);
            Building keep = RegionSettlements.Current.Seat.Keep;
            keep.Damageable.Unit.TakeDamage(keep.Damageable.MaxHealth * 0.8f, DamageRules.TrueType);
            yield return TestWait.Until(() => ruler.IsEscaping, "ruler starts escaping", 6f);
            yield return TestWait.Until(() => ruler.HasEscaped, "ruler escapes", 12f);
            yield return null;
        }
    }
}
