using System;
using System.Collections.Generic;

namespace Isekai
{
    /// <summary>The figures the end screens show about a Run.</summary>
    public enum RunStat { Regions, Time, Deaths, Souls, SoulLevel, RulersSlain, RulersEscaped }

    /// <summary>One line of the Run summary: which figure, its label and its value as shown.</summary>
    public readonly struct RunSummaryRow
    {
        public RunSummaryRow(RunStat stat, string label, string value)
        {
            Stat = stat;
            Label = label;
            Value = value;
        }

        public RunStat Stat { get; }
        public string Label { get; }
        public string Value { get; }
    }

    /// <summary>
    /// How a Run went, for the victory and defeat screens: Regions captured, the Run clock, Jack's deaths, the Souls
    /// and soul level he ended with, and the rulers slain or escaped.
    /// </summary>
    public sealed class RunSummary
    {
        public RunSummary(bool won, int regionsCaptured, int regionsTotal, float seconds, int deaths, int souls, int soulLevel, int rulersSlain, int rulersEscaped)
        {
            Won = won;
            RegionsCaptured = regionsCaptured;
            RegionsTotal = regionsTotal;
            Seconds = Math.Max(0f, seconds);
            Deaths = deaths;
            Souls = souls;
            SoulLevel = soulLevel;
            RulersSlain = rulersSlain;
            RulersEscaped = rulersEscaped;
        }

        public bool Won { get; }
        public int RegionsCaptured { get; }
        public int RegionsTotal { get; }
        public float Seconds { get; }
        public int Deaths { get; }
        public int Souls { get; }
        public int SoulLevel { get; }
        public int RulersSlain { get; }
        public int RulersEscaped { get; }

        /// <summary>The Run clock as minutes and seconds, or hours too once it passes one.</summary>
        public string TimeText => FormatTime(Seconds);

        public string Title => Won ? "The Planet is yours!" : "Jack's soul has faded";
        public string Subtitle => Won ? "The Capital has fallen. The Run is won." : "The Run is lost.";

        /// <summary>The summary of a Run that has ended, with the figures that live outside it (soul deaths, Souls).</summary>
        public static RunSummary Of(PlanetRun run, int deaths, int souls, int soulLevel)
        {
            if (run == null) throw new ArgumentNullException(nameof(run));
            return new RunSummary(run.State == PlanetRunState.Won, run.CapturedInOrder.Count, run.Planet.Regions.Count, run.RunSeconds,
                deaths, souls, soulLevel, run.RulersSlain, run.EscapedRulers.Count);
        }

        /// <summary>The lines the screens show, in order.</summary>
        public IReadOnlyList<RunSummaryRow> Rows => new[]
        {
            new RunSummaryRow(RunStat.Regions, "Regions captured", $"{RegionsCaptured} of {RegionsTotal}"),
            new RunSummaryRow(RunStat.Time, "Time", TimeText),
            new RunSummaryRow(RunStat.Deaths, "Deaths", Deaths.ToString()),
            new RunSummaryRow(RunStat.Souls, "Souls", Souls.ToString()),
            new RunSummaryRow(RunStat.SoulLevel, "Soul level", SoulLevel.ToString()),
            new RunSummaryRow(RunStat.RulersSlain, "Rulers slain", RulersSlain.ToString()),
            new RunSummaryRow(RunStat.RulersEscaped, "Rulers escaped", RulersEscaped.ToString()),
        };

        public static string FormatTime(float seconds)
        {
            int total = (int)Math.Floor(Math.Max(0f, seconds));
            int hours = total / 3600, minutes = total % 3600 / 60, secs = total % 60;
            return hours > 0 ? $"{hours}:{minutes:00}:{secs:00}" : $"{minutes}:{secs:00}";
        }
    }
}
