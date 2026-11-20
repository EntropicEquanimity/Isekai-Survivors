using System.Collections;
using System.IO;
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
    /// The autoplay bot plays whole Runs on small test Planets (<c>RunSession.StartNew(seed, rings)</c>) at high game speed
    /// through the game's own flow, and writes a report per Run. The Planets are small and each Region is capped in game time,
    /// so a Run ends when the Capital falls, Jack is lost, or the bot gives up on a Region.
    /// </summary>
    public class AutoplayPlayModeTests
    {
        private const float Timeout = 170f;
        private string _reports;

        [UnitySetUp]
        public IEnumerator Clean()
        {
            RunSession.Clear();
            _reports = Path.Combine(Path.GetTempPath(), "isekai-autoplay-" + System.Guid.NewGuid().ToString("N"));
            yield return null;
        }

        // The Region is unloaded while the game runs normally, and every piece of Run state is forgotten, as in RunAcrossRingsPlayModeTests.
        [UnityTearDown]
        public IEnumerator Restore()
        {
            AutoplayRunner.Stop();
            RunDrafts.ClearQueue();
            Time.timeScale = 1f;
            SceneManager.LoadScene(RunSession.PlanetScene);
            yield return null;
            yield return null;
            RunSession.Clear();
            RunSave.Delete();
            RunBoons.Clear();
            if (RunManager.HasActiveRun) RunManager.EndRun(RunOutcome.Abandoned);
            Time.timeScale = 1f;
            if (Directory.Exists(_reports)) Directory.Delete(_reports, recursive: true);
            yield return null;
        }

        private AutoplayOptions Options(AutoplayStrategy strategy, float regionCap, params int[] seeds)
        {
            var options = new AutoplayOptions
            {
                Strategy = strategy, Speed = 16f, Rings = new[] { 1 }, RegionSecondsCap = regionCap, OutputDirectory = _reports, Runs = seeds.Length, Verbose = true,
            };
            options.Seeds.AddRange(seeds);
            return options;
        }

        [UnityTest]
        public IEnumerator One_bot_Run_on_a_small_Planet_finishes_and_writes_its_report()
        {
            AutoplayRunner runner = AutoplayRunner.Begin(Options(AutoplayStrategies.Rush, 150f, 41));
            yield return TestWait.Until(() => runner.IsFinished, "the bot Run finishes", Timeout);

            RunReport report = runner.Summary.Runs.Single();
            Assert.That(report.Result, Is.EqualTo(RunResult.Won).Or.EqualTo(RunResult.Lost).Or.EqualTo(RunResult.TimedOut), "it ended, it did not stall");
            Assert.AreEqual(41, report.Seed);
            Assert.AreEqual("rush", report.Strategy);
            Assert.AreEqual(2, report.RegionsTotal);
            Assert.GreaterOrEqual(report.Regions.Count, 1, "it played a Region");
            Assert.Greater(report.Regions[0].GameSeconds, 1f, "game time was played");
            Assert.Greater(report.Regions[0].RealSeconds, 0f);
            Assert.Greater(report.Frames.Count, 0, "frame times were sampled");
            Assert.Greater(report.Frames.P99, 0f);
            Assert.GreaterOrEqual(report.Frames.P99, report.Frames.P50);
            Assert.AreEqual(0, report.ErrorsLogged, string.Join("; ", report.ErrorMessages));
            Assert.IsTrue(report.InventoryStable, string.Join("; ", report.InventoryNotes));

            string json = Directory.GetFiles(_reports, "run-*.json").Single();
            string text = File.ReadAllText(json);
            foreach (string field in new[] { "\"result\"", "\"regions\"", "\"deaths\"", "\"soulsEarned\"", "\"soulsSpent\"", "\"highestVigilance\"", "\"wavesFaced\"", "\"rulersEscaped\"", "\"portalsBuilt\"", "\"frameMs\"", "\"p99\"", "\"errorsLogged\"" })
                StringAssert.Contains(field, text);
            string[] csv = File.ReadAllLines(Path.Combine(_reports, AutoplayReportFiles.CsvName));
            Assert.AreEqual(2, csv.Length, "a header and one row");
            Assert.AreEqual(RunReport.CsvHeader, csv[0]);
            Assert.AreEqual(1, Directory.GetFiles(_reports, "summary-*.json").Length, "the batch wrote its summary");
        }

        [UnityTest]
        public IEnumerator The_bot_mines_Stone_and_builds_a_Portal_on_corrupted_ground_with_the_game_s_own_means()
        {
            AutoplayRunner runner = AutoplayRunner.Begin(Options(AutoplayStrategies.Balanced, 300f, 41));
            yield return TestWait.Until(() => runner.Status.Contains("Playing") && RegionEconomy.Current != null && Player.Character != null, "the bot plays the Region", Timeout);

            // A Region starts with Stone and no Souls: take the Stone away and hand over the Souls, so that the bot has to mine for its Portal.
            Stockpile pile = RegionEconomy.Current.Stockpile;
            Assert.IsTrue(pile.TrySpend(ResourceType.Stone, pile.Get(ResourceType.Stone)));
            pile.Add(ResourceType.Souls, 30);
            Assert.IsFalse(IsPortalStanding(), "no Portal yet");

            yield return TestWait.Until(IsPortalStanding, "the bot mined 20 Stone and placed a Portal", 90f);
            Building portal = Building.All.First(b => b.IsPlayers && b.Asset != null && b.Asset.DisplayName == "Portal");
            Assert.GreaterOrEqual(RegionCorruption.Current.Level(RegionGrid.Current.CellAt(portal.transform.position)), 2, "on ground corrupted enough to build on");
            Assert.AreEqual(PortalOrder.Guard, portal.GetComponent<UnitProducer>().Order, "it guards while the bot builds up");
            yield return TestWait.Until(() => runner.IsFinished, "the bot Run finishes", Timeout);
            Assert.GreaterOrEqual(runner.Summary.Runs.Single().PortalsBuilt, 1, "the report counts it");
            Assert.AreEqual(0, runner.Summary.ErrorsLogged, string.Join("; ", runner.Summary.Runs.Single().ErrorMessages));
        }

        private static bool IsPortalStanding() => Building.All.Any(b => b != null && b.IsPlayers && b.Asset != null && b.Asset.DisplayName == "Portal");

        [UnityTest]
        public IEnumerator Two_Runs_in_a_row_make_two_reports_and_the_inventory_does_not_grow()
        {
            AutoplayRunner runner = AutoplayRunner.Begin(Options(AutoplayStrategies.Balanced, 40f, 51, 52));
            yield return TestWait.Until(() => runner.IsFinished, "both bot Runs finish", Timeout);

            Assert.AreEqual(2, runner.Summary.Runs.Count);
            Assert.AreEqual(new[] { 0, 1 }, runner.Summary.Runs.Select(r => r.Index).ToArray());
            Assert.AreEqual(new[] { 51, 52 }, runner.Summary.Runs.Select(r => r.Seed).ToArray());
            Assert.AreEqual(2, Directory.GetFiles(_reports, "run-*.json").Length, "a JSON report per Run");
            Assert.AreEqual(3, File.ReadAllLines(Path.Combine(_reports, AutoplayReportFiles.CsvName)).Length, "a header and a row per Run");
            Assert.AreEqual(runner.Summary.Runs[0].StartItems, runner.Summary.Runs[1].StartItems, "the second Run starts with what the first did");
            foreach (RunReport report in runner.Summary.Runs)
            {
                Assert.IsTrue(report.InventoryStable, string.Join("; ", report.InventoryNotes));
                foreach (RegionReport region in report.Regions) Assert.AreEqual(report.StartItems, region.ItemsAtStart, "no Items appeared between Regions or Runs");
            }
            Assert.AreEqual(0, runner.Summary.ErrorsLogged);
        }

        [UnityTest]
        public IEnumerator A_lost_Run_goes_through_the_end_screen_to_the_menu_and_the_next_Run_starts_clean()
        {
            AutoplayRunner runner = AutoplayRunner.Begin(Options(AutoplayStrategies.Balanced, 40f, 61, 62));
            yield return TestWait.Until(() => runner.Status.Contains("Playing") && RegionEconomy.Current != null && Player.Character != null, "the bot plays the first Region", Timeout);
            Assert.AreEqual(0, RegionEconomy.Current.Stockpile.Get(ResourceType.Souls), "no Souls, so a dead Jack is a lost Run");
            Player.Character.GetComponent<Damageable>().Kill();
            yield return TestWait.Until(() => runner.Summary.Runs.Count == 1, "the first Run is reported", Timeout);

            RunReport lost = runner.Summary.Runs[0];
            Assert.AreEqual(RunResult.Lost, lost.Result);
            Assert.AreEqual("Defeated", lost.Regions.Last().Outcome);
            Assert.GreaterOrEqual(lost.Deaths, 1);

            yield return TestWait.Until(() => runner.IsFinished, "the second Run finishes", Timeout);
            RunReport next = runner.Summary.Runs[1];
            Assert.AreEqual(62, next.Seed);
            Assert.IsTrue(next.InventoryStable, string.Join("; ", next.InventoryNotes));
            Assert.AreEqual(lost.StartItems, next.StartItems, "the Character starts the next Run with the same Items");
            Assert.AreEqual(next.Regions.Sum(r => r.Deaths), next.Deaths, "Jack's deaths start again (the last Run's are not carried over)");
            Assert.AreEqual(1f, Time.timeScale, "the game is not left paused or sped up");
        }

        [UnityTest]
        public IEnumerator The_bot_walks_Jack_through_the_games_Move_input_and_hands_it_back_when_the_batch_ends()
        {
            Blondie.Core.IActionInput before = Blondie.Core.ActionInput.Backend;
            float fixedStep = Time.fixedDeltaTime;
            AutoplayRunner runner = AutoplayRunner.Begin(Options(AutoplayStrategies.Rush, 60f, 41));
            Assert.AreNotSame(before, Blondie.Core.ActionInput.Backend, "the bot's input sits over the game's");
            Assert.IsTrue(Blondie.Core.ActionInput.HasAction("Move"));

            yield return TestWait.Until(() => runner.Status.Contains("Playing") && Player.Character != null, "the bot plays the Region", Timeout);
            Stockpile pile = RegionEconomy.Current.Stockpile;
            pile.TrySpend(ResourceType.Stone, pile.Get(ResourceType.Stone));   // no Stone: the bot walks Jack to a node to mine it
            Vector3 start = Player.Character.transform.position;
            yield return TestWait.Until(() => Player.Character != null && Vector2.Distance(Player.Character.transform.position, start) > 4f, "Jack was walked somewhere through the Move input", Timeout);

            yield return TestWait.Until(() => runner.IsFinished, "the bot Run finishes", Timeout);
            Assert.AreSame(before, Blondie.Core.ActionInput.Backend, "the game's own input is back");
            Assert.AreEqual(1f, Time.timeScale);
            Assert.AreEqual(fixedStep, Time.fixedDeltaTime, 0.0001f, "the physics step is back");
        }
    }
}
