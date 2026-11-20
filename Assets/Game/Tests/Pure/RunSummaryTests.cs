using System.Linq;
using NUnit.Framework;

namespace Isekai.Tests
{
    /// <summary>The Run summary the victory and defeat screens show.</summary>
    public class RunSummaryTests
    {
        private static PlanetRun Run()
        {
            var run = new PlanetRun(Planet.Generate(3, new[] { 1 }, new[] { "a" }));
            run.Enter(run.Available().Single());
            return run;
        }

        [Test]
        public void A_won_Run_counts_its_Regions_time_and_rulers()
        {
            PlanetRun run = Run();
            RegionInfo edge = run.Current;
            run.Tick(125f);
            run.RecordRulersSlain();
            run.RecordEscapedRuler(edge, run.Planet.Capital, "Aldric the Stern", new[] { TraitId.Brave });
            run.CaptureCurrent();
            run.Enter(run.Planet.Capital);
            run.Tick(60f);
            run.RecordRulersSlain();
            run.CaptureCurrent();

            RunSummary summary = RunSummary.Of(run, deaths: 2, souls: 40, soulLevel: 3);
            Assert.IsTrue(summary.Won);
            Assert.AreEqual(2, summary.RegionsCaptured);
            Assert.AreEqual(2, summary.RegionsTotal);
            Assert.AreEqual("3:05", summary.TimeText);
            Assert.AreEqual(2, summary.Deaths);
            Assert.AreEqual(40, summary.Souls);
            Assert.AreEqual(3, summary.SoulLevel);
            Assert.AreEqual(2, summary.RulersSlain);
            Assert.AreEqual(1, summary.RulersEscaped);
        }

        [Test]
        public void A_lost_Run_is_not_won()
        {
            PlanetRun run = Run();
            run.Lose();
            RunSummary summary = RunSummary.Of(run, 5, 0, 1);
            Assert.IsFalse(summary.Won);
            Assert.AreEqual(0, summary.RegionsCaptured);
            StringAssert.Contains("lost", summary.Subtitle.ToLowerInvariant());
        }

        [Test]
        public void Every_figure_has_a_row_in_order()
        {
            RunSummary summary = new(true, 3, 14, 3700f, 1, 12, 2, 2, 1);
            CollectionAssert.AreEqual(new[]
            {
                RunStat.Regions, RunStat.Time, RunStat.Deaths, RunStat.Souls, RunStat.SoulLevel, RunStat.RulersSlain, RunStat.RulersEscaped,
            }, summary.Rows.Select(r => r.Stat).ToArray());
            Assert.AreEqual("3 of 14", summary.Rows[0].Value);
            Assert.AreEqual("1:01:40", summary.Rows[1].Value);
        }

        [Test]
        public void Time_never_goes_negative()
        {
            Assert.AreEqual("0:00", RunSummary.FormatTime(-4f));
            Assert.AreEqual("0:59", RunSummary.FormatTime(59.9f));
        }

        [Test]
        public void Every_figure_explains_itself_on_hover()
        {
            foreach (RunStat stat in System.Enum.GetValues(typeof(RunStat)))
            {
                Assert.IsNotEmpty(RunStatTips.Header(stat), stat.ToString());
                Assert.IsNotEmpty(RunStatTips.Text(stat), stat.ToString());
            }
        }
    }
}
