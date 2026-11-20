using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Isekai
{
    /// <summary>How an autoplay Run ended.</summary>
    public enum RunResult
    {
        /// <summary>The Capital fell.</summary>
        Won,

        /// <summary>Jack's Souls ran out.</summary>
        Lost,

        /// <summary>The bot gave up: a Region took longer than its game-time cap, or the Run its real-time cap.</summary>
        TimedOut,

        /// <summary>The runner could not carry on (a scene never loaded, no Region to enter).</summary>
        Stalled,
    }

    /// <summary>One Region of an autoplay Run.</summary>
    public sealed class RegionReport
    {
        public string Id = string.Empty;
        public string Name = string.Empty;
        public int Depth;
        public bool IsCapital;

        /// <summary>"Captured", "Defeated" or "Abandoned" (the bot gave up).</summary>
        public string Outcome = "Abandoned";

        public float RealSeconds;
        public float GameSeconds;

        /// <summary>Souls added to and taken from the stockpile in this Region (what is taken includes the drain while bodiless).</summary>
        public int SoulsEarned;
        public int SoulsSpent;

        public int Deaths;
        public VigilanceStage HighestVigilance;
        public int WavesFaced;
        public int RulersSlain;
        public int RulersEscaped;
        public int PortalsBuilt;
        public int WorkersSummoned;
        public int ItemsAtStart;
        public int ItemsAtEnd;

        /// <summary>Times Jack's body was found on a cell that cannot be stood on (stepping through a wall at speed).</summary>
        public int TunnelSamples;
        public int Samples;
        public int Errors;
    }

    /// <summary>One autoplay Run: the settings it ran with, how it ended and the figures per Region and in all.</summary>
    public sealed class RunReport
    {
        public int Index;
        public int Seed;
        public string Anchor = string.Empty;
        public string Strategy = string.Empty;
        public float Speed;
        public RunResult Result = RunResult.Stalled;
        public int RegionsTotal;
        public string Version = string.Empty;
        public string StartedAt = string.Empty;
        public float RealSeconds;
        public int Deaths;
        public int SoulLevel;

        public FrameTimes Frames = FrameTimes.Empty;
        public readonly List<RegionReport> Regions = new();
        public readonly List<string> ErrorMessages = new();
        public int ErrorsLogged;

        /// <summary>The Character's inventory at the start of the Run's first Region, and whether it was what a new Run starts with and what the Regions carried.</summary>
        public int StartItems;
        public bool InventoryStable = true;
        public readonly List<string> InventoryNotes = new();

        public int RegionsCaptured => Regions.Count(r => r.Outcome == "Captured");
        public float GameSeconds => Regions.Sum(r => r.GameSeconds);
        public int SoulsEarned => Regions.Sum(r => r.SoulsEarned);
        public int SoulsSpent => Regions.Sum(r => r.SoulsSpent);
        public int WavesFaced => Regions.Sum(r => r.WavesFaced);
        public int RulersSlain => Regions.Sum(r => r.RulersSlain);
        public int RulersEscaped => Regions.Sum(r => r.RulersEscaped);
        public int PortalsBuilt => Regions.Sum(r => r.PortalsBuilt);
        public int WorkersSummoned => Regions.Sum(r => r.WorkersSummoned);
        public int TunnelSamples => Regions.Sum(r => r.TunnelSamples);
        public int Samples => Regions.Sum(r => r.Samples);
        public VigilanceStage HighestVigilance => Regions.Count == 0 ? VigilanceStage.Unaware : Regions.Max(r => r.HighestVigilance);

        /// <summary>The Run as JSON (indented).</summary>
        public string ToJson()
        {
            var json = new JsonWriter();
            json.BeginObject();
            json.Field("run", Index);
            json.Field("seed", Seed);
            json.Field("anchor", Anchor);
            json.Field("strategy", Strategy);
            json.Field("speed", Speed);
            json.Field("result", Result.ToString());
            json.Field("version", Version);
            json.Field("startedAt", StartedAt);
            json.Field("realSeconds", RealSeconds);
            json.Field("gameSeconds", GameSeconds);
            json.Field("regionsCaptured", RegionsCaptured);
            json.Field("regionsTotal", RegionsTotal);
            json.Field("deaths", Deaths);
            json.Field("soulLevel", SoulLevel);
            json.Field("soulsEarned", SoulsEarned);
            json.Field("soulsSpent", SoulsSpent);
            json.Field("highestVigilance", VigilanceStageNames.Name(HighestVigilance));
            json.Field("wavesFaced", WavesFaced);
            json.Field("rulersSlain", RulersSlain);
            json.Field("rulersEscaped", RulersEscaped);
            json.Field("portalsBuilt", PortalsBuilt);
            json.Field("workersSummoned", WorkersSummoned);
            json.Field("tunnelSamples", TunnelSamples);
            json.Field("samples", Samples);
            json.BeginObjectField("frameMs");
            json.Field("frames", Frames.Count);
            json.Field("p50", Frames.P50);
            json.Field("p95", Frames.P95);
            json.Field("p99", Frames.P99);
            json.Field("max", Frames.Max);
            json.EndObject();
            json.BeginObjectField("inventory");
            json.Field("startItems", StartItems);
            json.Field("stable", InventoryStable);
            json.StringArrayField("notes", InventoryNotes);
            json.EndObject();
            json.Field("errorsLogged", ErrorsLogged);
            json.StringArrayField("errors", ErrorMessages);
            json.BeginArrayField("regions");
            foreach (RegionReport r in Regions)
            {
                json.BeginObjectItem();
                json.Field("id", r.Id);
                json.Field("name", r.Name);
                json.Field("depth", r.Depth);
                json.Field("capital", r.IsCapital);
                json.Field("outcome", r.Outcome);
                json.Field("realSeconds", r.RealSeconds);
                json.Field("gameSeconds", r.GameSeconds);
                json.Field("deaths", r.Deaths);
                json.Field("soulsEarned", r.SoulsEarned);
                json.Field("soulsSpent", r.SoulsSpent);
                json.Field("highestVigilance", VigilanceStageNames.Name(r.HighestVigilance));
                json.Field("wavesFaced", r.WavesFaced);
                json.Field("rulersSlain", r.RulersSlain);
                json.Field("rulersEscaped", r.RulersEscaped);
                json.Field("portalsBuilt", r.PortalsBuilt);
                json.Field("workersSummoned", r.WorkersSummoned);
                json.Field("itemsAtStart", r.ItemsAtStart);
                json.Field("itemsAtEnd", r.ItemsAtEnd);
                json.Field("tunnelSamples", r.TunnelSamples);
                json.Field("samples", r.Samples);
                json.Field("errors", r.Errors);
                json.EndObject();
            }
            json.EndArray();
            json.EndObject();
            return json.ToString();
        }

        /// <summary>The columns of <see cref="ToCsvRow"/>.</summary>
        public static string CsvHeader =>
            "run,seed,anchor,strategy,speed,result,regionsCaptured,regionsTotal,realSeconds,gameSeconds,deaths,soulLevel,soulsEarned,soulsSpent," +
            "highestVigilance,wavesFaced,rulersSlain,rulersEscaped,portalsBuilt,workersSummoned,frameP50Ms,frameP95Ms,frameP99Ms,errorsLogged,inventoryStable,tunnelSamples,regionTimes";

        /// <summary>One CSV line for this Run: the same figures as the JSON, with the time per Region as <c>name:real/game</c> joined by semicolons.</summary>
        public string ToCsvRow()
        {
            string times = string.Join(";", Regions.Select(r => $"{r.Name}:{F(r.RealSeconds)}/{F(r.GameSeconds)}"));
            return string.Join(",", new[]
            {
                Index.ToString(CultureInfo.InvariantCulture), Seed.ToString(CultureInfo.InvariantCulture), Csv(Anchor), Csv(Strategy), F(Speed), Result.ToString(),
                RegionsCaptured.ToString(CultureInfo.InvariantCulture), RegionsTotal.ToString(CultureInfo.InvariantCulture), F(RealSeconds), F(GameSeconds),
                Deaths.ToString(CultureInfo.InvariantCulture), SoulLevel.ToString(CultureInfo.InvariantCulture), SoulsEarned.ToString(CultureInfo.InvariantCulture),
                SoulsSpent.ToString(CultureInfo.InvariantCulture), VigilanceStageNames.Name(HighestVigilance), WavesFaced.ToString(CultureInfo.InvariantCulture),
                RulersSlain.ToString(CultureInfo.InvariantCulture), RulersEscaped.ToString(CultureInfo.InvariantCulture), PortalsBuilt.ToString(CultureInfo.InvariantCulture),
                WorkersSummoned.ToString(CultureInfo.InvariantCulture), F(Frames.P50), F(Frames.P95), F(Frames.P99), ErrorsLogged.ToString(CultureInfo.InvariantCulture),
                InventoryStable ? "true" : "false", TunnelSamples.ToString(CultureInfo.InvariantCulture), Csv(times),
            });
        }

        internal static string F(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);

        internal static string Csv(string text)
        {
            if (text.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0) return text;
            return "\"" + text.Replace("\"", "\"\"") + "\"";
        }
    }

    /// <summary>Frame times in milliseconds: how many frames and the median, 95th and 99th percentile.</summary>
    public readonly struct FrameTimes
    {
        public static readonly FrameTimes Empty = new(0, 0f, 0f, 0f, 0f);

        public FrameTimes(int count, float p50, float p95, float p99, float max)
        {
            Count = count;
            P50 = p50;
            P95 = p95;
            P99 = p99;
            Max = max;
        }

        public int Count { get; }
        public float P50 { get; }
        public float P95 { get; }
        public float P99 { get; }
        public float Max { get; }

        /// <summary>The percentiles of <paramref name="millis"/> (nearest rank; the list is not changed).</summary>
        public static FrameTimes Of(IReadOnlyList<float> millis)
        {
            if (millis == null || millis.Count == 0) return Empty;
            float[] sorted = millis.ToArray();
            Array.Sort(sorted);
            return new FrameTimes(sorted.Length, Percentile(sorted, 0.50f), Percentile(sorted, 0.95f), Percentile(sorted, 0.99f), sorted[sorted.Length - 1]);
        }

        /// <summary>The value at <paramref name="fraction"/> of the sorted samples, by nearest rank.</summary>
        public static float Percentile(IReadOnlyList<float> sorted, float fraction)
        {
            if (sorted.Count == 0) return 0f;
            int rank = (int)Math.Ceiling(fraction * sorted.Count - 0.0001f);
            return sorted[Math.Max(0, Math.Min(sorted.Count - 1, rank - 1))];
        }
    }

    /// <summary>The summary a batch writes when it is done: every Run's result and how many of each.</summary>
    public sealed class BatchSummary
    {
        public readonly List<RunReport> Runs = new();
        public string Strategy = string.Empty;
        public string Anchor = string.Empty;
        public float Speed;

        public int Count(RunResult result) => Runs.Count(r => r.Result == result);
        public int ErrorsLogged => Runs.Sum(r => r.ErrorsLogged);
        public bool InventoryStable => Runs.All(r => r.InventoryStable);

        /// <summary>The process exit code: 0 for a clean batch, 2 when errors were logged or an inventory leaked, 3 when a Run stalled.</summary>
        public int ExitCode => Count(RunResult.Stalled) > 0 ? 3 : ErrorsLogged > 0 || !InventoryStable ? 2 : 0;

        public string ToJson()
        {
            var json = new JsonWriter();
            json.BeginObject();
            json.Field("runs", Runs.Count);
            json.Field("strategy", Strategy);
            json.Field("anchor", Anchor);
            json.Field("speed", Speed);
            json.Field("won", Count(RunResult.Won));
            json.Field("lost", Count(RunResult.Lost));
            json.Field("timedOut", Count(RunResult.TimedOut));
            json.Field("stalled", Count(RunResult.Stalled));
            json.Field("errorsLogged", ErrorsLogged);
            json.Field("inventoryStable", InventoryStable);
            json.Field("exitCode", ExitCode);
            json.Field("averageRegionsCaptured", Runs.Count == 0 ? 0f : (float)Runs.Average(r => r.RegionsCaptured));
            json.Field("averageRealSeconds", Runs.Count == 0 ? 0f : (float)Runs.Average(r => r.RealSeconds));
            json.Field("worstFrameP99Ms", Runs.Count == 0 ? 0f : Runs.Max(r => r.Frames.P99));
            json.BeginArrayField("results");
            foreach (RunReport r in Runs)
            {
                json.BeginObjectItem();
                json.Field("run", r.Index);
                json.Field("seed", r.Seed);
                json.Field("result", r.Result.ToString());
                json.Field("regionsCaptured", r.RegionsCaptured);
                json.Field("realSeconds", r.RealSeconds);
                json.EndObject();
            }
            json.EndArray();
            json.EndObject();
            return json.ToString();
        }
    }

    /// <summary>A small JSON writer (Rules has no engine and no serializer): objects, arrays, strings, numbers and booleans.</summary>
    public sealed class JsonWriter
    {
        private readonly StringBuilder _text = new();
        private readonly Stack<bool> _first = new();
        private int _depth;

        public override string ToString() => _text.ToString();

        public void BeginObject()
        {
            _text.Append('{');
            Open();
        }

        public void BeginObjectItem()
        {
            Separate();
            _text.Append('{');
            Open();
        }

        public void BeginObjectField(string name)
        {
            Key(name);
            _text.Append('{');
            Open();
        }

        public void BeginArrayField(string name)
        {
            Key(name);
            _text.Append('[');
            Open();
        }

        public void EndObject() => Close('}');

        public void EndArray() => Close(']');

        public void Field(string name, string value)
        {
            Key(name);
            Quote(value ?? string.Empty);
        }

        public void Field(string name, int value)
        {
            Key(name);
            _text.Append(value.ToString(CultureInfo.InvariantCulture));
        }

        public void Field(string name, float value)
        {
            Key(name);
            _text.Append(float.IsNaN(value) || float.IsInfinity(value) ? "0" : value.ToString("0.###", CultureInfo.InvariantCulture));
        }

        public void Field(string name, bool value)
        {
            Key(name);
            _text.Append(value ? "true" : "false");
        }

        public void StringArrayField(string name, IReadOnlyList<string> items)
        {
            BeginArrayField(name);
            foreach (string item in items)
            {
                Separate();
                Quote(item);
            }
            EndArray();
        }

        private void Open()
        {
            _first.Push(true);
            _depth++;
        }

        private void Close(char bracket)
        {
            bool empty = _first.Pop();
            _depth--;
            if (!empty) NewLine();
            _text.Append(bracket);
        }

        private void Key(string name)
        {
            Separate();
            Quote(name);
            _text.Append(": ");
        }

        private void Separate()
        {
            if (_first.Count == 0) return;
            bool first = _first.Pop();
            if (!first) _text.Append(',');
            _first.Push(false);
            NewLine();
        }

        private void NewLine()
        {
            _text.Append('\n');
            _text.Append(' ', _depth * 2);
        }

        private void Quote(string value)
        {
            _text.Append('"');
            foreach (char c in value)
            {
                switch (c)
                {
                    case '"': _text.Append("\\\""); break;
                    case '\\': _text.Append("\\\\"); break;
                    case '\n': _text.Append("\\n"); break;
                    case '\r': _text.Append("\\r"); break;
                    case '\t': _text.Append("\\t"); break;
                    default:
                        if (c < 0x20) _text.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else _text.Append(c);
                        break;
                }
            }
            _text.Append('"');
        }
    }
}
