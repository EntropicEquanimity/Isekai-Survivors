using System.Collections;
using System.Linq;
using Blondie.Combat;
using Blondie.Roguelike;
using Blondie.Roguelike.Boons;
using Blondie.Roguelike.Drafts;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Isekai.Tests
{
    /// <summary>
    /// A full Run across the rings to the Capital, on small test Planets: each ring's Region is made from its ring's
    /// numbers, capturing the Capital wins the Run, a lost Run shows the defeat screen, and either screen leads to a
    /// new Run. The Planets are small (RunSession.StartNew(seed, rings)); the Regions are the real scenes.
    /// </summary>
    public class RunAcrossRingsPlayModeTests
    {
        private const float Timeout = 25f;

        [UnitySetUp]
        public IEnumerator Clean()
        {
            RunSession.Clear();
            yield return null;
        }

        // Leaves nothing behind for the next fixture: the Region is unloaded while the game runs normally (an end screen
        // pauses it, and objects destroyed under a pause were still registered when the next Region woke), then every
        // piece of Run state a test may have set is forgotten.
        [UnityTearDown]
        public IEnumerator Restore()
        {
            RunDrafts.ClearQueue();
            Time.timeScale = 1f;
            SceneManager.LoadScene(RunSession.PlanetScene);
            yield return null;
            yield return null;
            RunSession.Clear();
            RunSave.Delete();
            RunBoons.Clear();   // the Region buffs a capture hands out
            if (RunManager.HasActiveRun) RunManager.EndRun(RunOutcome.Abandoned);
            Time.timeScale = 1f;
            yield return null;
        }

        [UnityTest]
        public IEnumerator Capturing_through_to_the_Capital_wins_the_Run_and_shows_the_victory_screen()
        {
            RunSession.StartNew(11, new[] { 1 });
            PlanetRun run = RunSession.Run;
            RegionInfo edge = run.Available().Single();
            RunSession.GoToRegion(edge);
            yield return InRegion(edge);
            Assert.IsFalse(RunEndScreen.Current.IsVisible, "no end screen mid-Run");
            yield return CaptureRegion();
            yield return TestWait.Until(() => SceneManager.GetActiveScene().name == RunSession.PlanetScene && Object.FindAnyObjectByType<PlanetScreen>()?.Map != null, "back on the Planet map", Timeout);
            Assert.IsTrue(RunSave.Exists, "the Run is saved between Regions");

            Assert.IsTrue(RunSession.GoToRegion(run.Planet.Capital));
            yield return InRegion(run.Planet.Capital);
            yield return TestWait.Until(() => RegionSettlements.Current?.Seat?.Keep != null, "the palace's keep stands", Timeout);

            RegionSettlements.Settlement palace = RegionSettlements.Current.Seat;
            Assert.AreEqual(RingRecipe.Of(RegionRing.Capital).SeatSize, palace.Plan.Piece.Width, "the Capital's Seat is the palace");
            Assert.AreEqual(VigilanceStage.Alarmed, VigilanceDirector.Current.Stage, "the Capital starts Alarmed");
            Assert.GreaterOrEqual(VigilanceDirector.Current.WaveCap, RingRecipe.CapitalMinimumWaves);
            Assert.IsFalse(RunEndScreen.Current.IsVisible);

            yield return CaptureRegion();
            yield return TestWait.Until(() => RunEndScreen.Current.IsVisible, "the victory screen shows", Timeout);

            Assert.AreEqual(PlanetRunState.Won, run.State);
            Assert.IsFalse(RunManager.HasActiveRun, "Blondie's Run ended too");
            Assert.IsFalse(RunSave.Exists, "a won Run deletes its save");
            RunEndScreen screen = RunEndScreen.Current;
            Assert.IsTrue(screen.Summary.Won);
            StringAssert.Contains("yours", screen.TitleText);
            Assert.AreEqual("2 of 2", screen.RowText(RunStat.Regions).Substring("Regions captured ".Length));
            StringAssert.Contains("Time", screen.RowText(RunStat.Time));
            StringAssert.Contains("Deaths 0", screen.RowText(RunStat.Deaths));
            Assert.IsNotNull(screen.RowText(RunStat.Souls));
            Assert.IsNotNull(screen.RowText(RunStat.RulersSlain));
            Assert.IsNotNull(screen.RowText(RunStat.RulersEscaped));
            Assert.GreaterOrEqual(screen.Summary.RulersSlain + screen.Summary.RulersEscaped, 1, "the Capital's ruler is counted");
            foreach (RunStat stat in System.Enum.GetValues(typeof(RunStat))) Assert.IsTrue(screen.HasTooltip(stat), $"{stat} explains itself on hover");
            CollectionAssert.AreEqual(new[] { "New Run", "Main menu" }, screen.ButtonTexts);
        }

        [UnityTest]
        public IEnumerator Losing_with_no_Souls_while_bodiless_shows_the_defeat_screen_and_deletes_the_save()
        {
            RunSession.StartNew(12, new[] { 1 });
            RunSave.Save();   // a Run saved on the Planet map
            Assert.IsTrue(RunSave.Exists);
            PlanetRun run = RunSession.Run;
            RunSession.GoToRegion(run.Available().Single());
            yield return InRegion(run.Current);
            RegionTestScene.ClearHorde();
            Assert.AreEqual(0, RegionEconomy.Current.Stockpile.Get(ResourceType.Souls));
            Assert.IsFalse(RunEndScreen.Current.IsVisible);

            Player.Character.GetComponent<Damageable>().Kill();
            yield return TestWait.Until(() => RunEndScreen.Current.IsVisible, "the defeat screen shows", Timeout);

            Assert.AreEqual(PlanetRunState.Lost, run.State);
            Assert.IsFalse(RunSave.Exists, "a lost Run deletes its save");
            RunEndScreen screen = RunEndScreen.Current;
            Assert.IsFalse(screen.Summary.Won);
            StringAssert.Contains("faded", screen.TitleText);
            StringAssert.Contains("Deaths 1", screen.RowText(RunStat.Deaths));
            StringAssert.Contains("0 of 2", screen.RowText(RunStat.Regions));
        }

        [UnityTest]
        public IEnumerator A_new_Run_starts_straight_after_a_Run_ends()
        {
            RunSession.StartNew(13, new[] { 1 });
            PlanetRun lost = RunSession.Run;
            RunSession.GoToRegion(lost.Available().Single());
            yield return InRegion(lost.Current);
            RegionTestScene.ClearHorde();
            Player.Character.GetComponent<Damageable>().Kill();
            yield return TestWait.Until(() => RunEndScreen.Current.IsVisible, "the defeat screen shows", Timeout);

            RunEndScreen.Current.NewRun();
            yield return TestWait.Until(() => SceneManager.GetActiveScene().name == RunSession.PlanetScene && Object.FindAnyObjectByType<PlanetScreen>()?.Map != null, "on the new Run's Planet map", Timeout);

            Assert.IsTrue(RunSession.HasRun, "a Run is in progress");
            Assert.AreNotSame(lost, RunSession.Run);
            Assert.AreEqual(PlanetRunState.Choosing, RunSession.Run.State);
            Assert.AreEqual(0, RunSession.Run.CapturedInOrder.Count);
            Assert.AreEqual(0, SoulRun.Ledger.Deaths, "Jack's deaths start again");
            Assert.AreEqual(Time.timeScale, 1f, "the game runs again");
            Assert.IsTrue(RunSession.Run.CanEnter(RunSession.Run.Available().First()));
        }

        [UnityTest]
        public IEnumerator A_middle_ring_Region_has_a_higher_Wave_cap_and_a_bigger_Seat_than_an_outer_one()
        {
            RunSession.StartNew(14, new[] { 1, 1, 1 });
            PlanetRun run = RunSession.Run;
            RegionInfo outer = run.Available().Single();
            RunSession.GoToRegion(outer);
            yield return InRegion(outer);
            yield return TestWait.Until(() => RegionSettlements.Current?.Seat?.Keep != null && VigilanceDirector.Current != null, "the Region fills", Timeout);
            Snapshot outerNumbers = Read();
            Assert.AreEqual(RegionRing.Outer, RegionBootstrap.Current.Context.Ring.Ring);
            Assert.AreEqual(VigilanceTuning.OuterRingWaveCap, outerNumbers.WaveCap);

            yield return CaptureRegion();
            yield return TestWait.Until(() => SceneManager.GetActiveScene().name == RunSession.PlanetScene && Object.FindAnyObjectByType<PlanetScreen>()?.Map != null, "back on the Planet map", Timeout);
            RegionInfo middle = run.Available().Single();
            Assert.AreEqual(1, middle.Depth);
            RunSession.GoToRegion(middle);
            yield return InRegion(middle);
            yield return TestWait.Until(() => RegionSettlements.Current?.Seat?.Keep != null && VigilanceDirector.Current != null, "the Region fills", Timeout);
            Snapshot middleNumbers = Read();

            Assert.AreEqual(RegionRing.Middle, RegionBootstrap.Current.Context.Ring.Ring);
            Assert.Greater(middleNumbers.WaveCap, outerNumbers.WaveCap, "a higher Wave cap");
            Assert.Greater(middleNumbers.SeatSize, outerNumbers.SeatSize, "a bigger Seat");
            Assert.Greater(middleNumbers.SeatGuards, outerNumbers.SeatGuards, "more guards in the Seat");
            Assert.Greater(middleNumbers.Width, outerNumbers.Width, "a bigger map");
            Assert.Greater(middleNumbers.Height, outerNumbers.Height);
            Assert.Greater(middleNumbers.Settlements, outerNumbers.Settlements - 1, "at least as many places");
            Assert.Greater(middleNumbers.KeepHealth, outerNumbers.KeepHealth, "a sturdier keep");
            Assert.AreEqual(middleNumbers.Width, RegionGrid.Current.Width, "the map grid was made the ring's size");
            Assert.AreEqual(RingRecipe.Of(RegionRing.Middle).SupplyRate * 0f + RingRecipe.Of(RegionRing.Middle).StartingSupply, RegionSupply.Current.Pool.Amount, 1f, "the mortals start with the ring's Supply");
        }

        [UnityTest]
        public IEnumerator A_ruler_who_escaped_returns_with_their_Traits_as_an_extra_hero_at_the_Seat_they_fled_to()
        {
            RunSession.StartNew(15, new[] { 1, 1 });
            PlanetRun run = RunSession.Run;
            RegionInfo from = run.Available().Single();
            RunSession.GoToRegion(from);
            yield return InRegion(from);
            yield return TestWait.Until(() => RegionRuler.Current != null && RegionRuler.Current.Ruler != null, "the ruler spawns", Timeout);
            RegionRuler first = RegionRuler.Current;
            first.EscapeSpeedCells = 28f;
            yield return TestWait.Until(() =>
            {
                UnitTraits traits = UnitTraits.Of(first.Ruler.gameObject);
                return traits != null && traits.Traits.Count >= 2;
            }, "ruler Traits rolled", 8f);
            Building keep = RegionSettlements.Current.Seat.Keep;
            keep.Damageable.Unit.TakeDamage(keep.Damageable.MaxHealth * 0.8f, DamageRules.TrueType);
            yield return TestWait.Until(() => first.HasEscaped, "the ruler escapes", 20f);
            EscapedRuler escaped = run.EscapedRulers.Single();
            RegionInfo destination = run.Planet[escaped.ToRegionId];
            Assert.IsFalse(destination.IsCapital, "fled one ring inward");

            yield return CaptureRegion();
            yield return TestWait.Until(() => SceneManager.GetActiveScene().name == RunSession.PlanetScene && Object.FindAnyObjectByType<PlanetScreen>()?.Map != null, "back on the Planet map", Timeout);
            Assert.AreEqual(1, run.EscapedRulers.Count, "the escape is kept on the Run");
            Assert.AreEqual(0, run.RulersSlain, "an escaped ruler is not counted as slain");

            Assert.IsTrue(RunSession.GoToRegion(destination));
            yield return InRegion(destination);
            yield return TestWait.Until(() => RegionRuler.Current != null && RegionRuler.Current.Ruler != null, "the Seat's own ruler spawns", Timeout);
            RegionRuler ruler = RegionRuler.Current;
            Assert.AreEqual(1, ruler.ReturnedHeroes.Count, "the escaped ruler is an extra hero");
            MortalBrain hero = ruler.ReturnedHeroes[0];
            Assert.AreNotSame(ruler.Ruler, hero, "besides the Seat's own ruler");
            Assert.IsTrue(hero.Settlement.IsSeat);
            StringAssert.Contains(escaped.Name, hero.gameObject.name);
            yield return TestWait.Until(() =>
            {
                UnitTraits t = UnitTraits.Of(hero.gameObject);
                return t != null && t.Traits.Count == escaped.Traits.Length;
            }, "the hero has the Traits the player learned", Timeout);
            CollectionAssert.AreEquivalent(escaped.Traits, UnitTraits.Of(hero.gameObject).Traits);
        }

        private readonly struct Snapshot
        {
            public Snapshot(int waveCap, int seatSize, int seatGuards, int width, int height, int settlements, float keepHealth)
            {
                WaveCap = waveCap;
                SeatSize = seatSize;
                SeatGuards = seatGuards;
                Width = width;
                Height = height;
                Settlements = settlements;
                KeepHealth = keepHealth;
            }

            public int WaveCap { get; }
            public int SeatSize { get; }
            public int SeatGuards { get; }
            public int Width { get; }
            public int Height { get; }
            public int Settlements { get; }
            public float KeepHealth { get; }
        }

        private static Snapshot Read()
        {
            RegionSettlements.Settlement seat = RegionSettlements.Current.Seat;
            return new Snapshot(VigilanceDirector.Current.WaveCap, seat.Plan.Piece.Width,
                seat.Markers.Count(m => m.Kind == MarkerKind.Guard), RegionGrid.Current.Width, RegionGrid.Current.Height,
                RegionSettlements.Current.Settlements.Count, seat.Keep.Damageable.MaxHealth);
        }

        private static IEnumerator InRegion(RegionInfo region) =>
            TestWait.Until(() => Object.FindAnyObjectByType<RegionRun>()?.Region == region && CharacterInventory.Current != null && RegionEconomy.Current != null
                                 && RunEndScreen.Current != null, $"in {region.Name}", Timeout);

        // Destroys the Seat's keep (the debug row does it by damage, as a fight would). Drafts are off: they'd pause the game.
        private static IEnumerator CaptureRegion()
        {
            yield return null;
            RegionTestScene.DisableDrafts();
            yield return TestWait.Until(() => RegionSettlements.Current?.Seat?.Keep != null, "the Seat's keep stands", Timeout);
            TestDebug.Press("Destroy the Seat");
            yield return TestWait.Until(() => RegionEvents.HasEnded && RegionEvents.Outcome == RegionEnd.Captured, "captured", Timeout);
            yield return null;
        }
    }
}
