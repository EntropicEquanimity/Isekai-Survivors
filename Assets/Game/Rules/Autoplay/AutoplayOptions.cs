using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Isekai
{
    /// <summary>
    /// A batch of autoplay Runs: how many, on which seeds, with which Anchor and strategy, at what game speed. Read from the
    /// command line (<c>-autoplay runs=N seeds=a,b,c anchor=Shrine|WarThrone strategy=stealth|rush|balanced speed=8</c>) or
    /// built by the debug menu and the Editor menu.
    /// </summary>
    public sealed class AutoplayOptions
    {
        public const string Flag = "-autoplay";

        /// <summary>The seed of Run <c>i</c> when none were given.</summary>
        public const int DefaultFirstSeed = 1001;

        public int Runs { get; set; } = 1;

        /// <summary>The seeds the Runs use in turn (Run i takes seed i, round again when there are fewer seeds than Runs).</summary>
        public List<int> Seeds { get; } = new();

        public AnchorKind Anchor { get; set; } = AnchorKind.Shrine;
        public AutoplayStrategy Strategy { get; set; } = AutoplayStrategies.Balanced;

        /// <summary>
        /// Several Anchors and strategies in one batch (<c>anchor=Shrine,WarThrone strategy=stealth,rush</c>): every pairing is
        /// played, each on every seed. Empty: just <see cref="Anchor"/> and <see cref="Strategy"/>.
        /// </summary>
        public List<AnchorKind> Anchors { get; } = new();
        public List<AutoplayStrategy> Strategies { get; } = new();

        private int AnchorCount => Math.Max(1, Anchors.Count);
        private int StrategyCount => Math.Max(1, Strategies.Count);

        /// <summary>The Anchor and strategy Run <c>i</c> plays: the pairings in turn, strategy changing fastest.</summary>
        public AnchorKind AnchorOf(int run) => Anchors.Count == 0 ? Anchor : Anchors[run / StrategyCount % AnchorCount];
        public AutoplayStrategy StrategyOf(int run) => Strategies.Count == 0 ? Strategy : Strategies[run % StrategyCount];

        /// <summary>The game speed asked for (the runner caps it, see <see cref="AutoplaySpeed"/>).</summary>
        public float Speed { get; set; } = 8f;

        /// <summary>The Planet's ring sizes (null: the game's own; tests use small Planets).</summary>
        public int[] Rings { get; set; }

        /// <summary>Game seconds one Region may take before the Run is given up as timed out.</summary>
        public float RegionSecondsCap { get; set; } = 2400f;

        /// <summary>Real seconds a Run may take before it is given up (a stuck bot or game).</summary>
        public float RunRealSecondsCap { get; set; } = 3600f;

        /// <summary>Where reports go (null: Logs/Autoplay in the project, or the persistent data path in a build).</summary>
        public string OutputDirectory { get; set; }

        /// <summary>Log what the bot decides and what kills Jack (for working out why a Run went the way it did).</summary>
        public bool Verbose { get; set; }

        /// <summary>Quit the application once the batch is done (command line); the debug menu and Editor menu leave it running.</summary>
        public bool QuitWhenDone { get; set; }

        public int SeedOf(int run)
        {
            int round = run / (AnchorCount * StrategyCount);
            if (Seeds.Count == 0) return DefaultFirstSeed + round;
            return Seeds[round % Seeds.Count];
        }

        /// <summary>The options after <c>-autoplay</c> in <paramref name="args"/>; null when the flag is not there.</summary>
        public static AutoplayOptions FromArgs(IReadOnlyList<string> args)
        {
            if (!TryParse(args, out AutoplayOptions options, out string error)) throw new FormatException(error);
            return options;
        }

        /// <summary>Reads the <c>key=value</c> words after <c>-autoplay</c>. True with null options when the flag is absent; false with a message when a word is wrong.</summary>
        public static bool TryParse(IReadOnlyList<string> args, out AutoplayOptions options, out string error)
        {
            options = null;
            error = null;
            int flag = -1;
            for (int i = 0; i < args.Count; i++)
                if (string.Equals(args[i], Flag, StringComparison.OrdinalIgnoreCase)) { flag = i; break; }
            if (flag < 0) return true;

            var result = new AutoplayOptions { QuitWhenDone = true };
            bool runsGiven = false;
            for (int i = flag + 1; i < args.Count && !args[i].StartsWith("-", StringComparison.Ordinal); i++)
            {
                int eq = args[i].IndexOf('=');
                if (eq <= 0) { error = $"autoplay: '{args[i]}' is not key=value"; return false; }
                string key = args[i].Substring(0, eq).Trim().ToLowerInvariant();
                string value = args[i].Substring(eq + 1).Trim();
                switch (key)
                {
                    case "runs":
                        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int runs) || runs < 1) { error = $"autoplay: runs must be 1 or more, not '{value}'"; return false; }
                        result.Runs = runs;
                        runsGiven = true;
                        break;
                    case "seeds":
                        foreach (string part in value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            if (!int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out int seed)) { error = $"autoplay: seed '{part}' is not a number"; return false; }
                            result.Seeds.Add(seed);
                        }
                        break;
                    case "anchor":
                        foreach (string part in value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            if (!TryAnchor(part, out AnchorKind anchor)) { error = $"autoplay: anchor must be Shrine or WarThrone, not '{part}'"; return false; }
                            result.Anchors.Add(anchor);
                        }
                        if (result.Anchors.Count > 0) result.Anchor = result.Anchors[0];
                        break;
                    case "strategy":
                        foreach (string part in value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            AutoplayStrategy strategy = AutoplayStrategies.Find(part);
                            if (strategy == null) { error = $"autoplay: strategy must be {string.Join("|", AutoplayStrategies.All.Select(s => s.Name))}, not '{part}'"; return false; }
                            result.Strategies.Add(strategy);
                        }
                        if (result.Strategies.Count > 0) result.Strategy = result.Strategies[0];
                        break;
                    case "speed":
                        if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float speed) || speed <= 0f) { error = $"autoplay: speed must be above 0, not '{value}'"; return false; }
                        result.Speed = speed;
                        break;
                    case "rings":
                        var rings = new List<int>();
                        foreach (string part in value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            if (!int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out int ring) || ring < 1) { error = $"autoplay: ring '{part}' must be 1 or more"; return false; }
                            rings.Add(ring);
                        }
                        result.Rings = rings.Count > 0 ? rings.ToArray() : null;
                        break;
                    case "regioncap":
                        if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float cap) || cap <= 0f) { error = $"autoplay: regioncap must be above 0, not '{value}'"; return false; }
                        result.RegionSecondsCap = cap;
                        break;
                    case "verbose":
                        result.Verbose = !string.Equals(value, "false", StringComparison.OrdinalIgnoreCase) && value != "0";
                        break;
                    case "out":
                        result.OutputDirectory = value;
                        break;
                    default:
                        error = $"autoplay: unknown option '{key}'";
                        return false;
                }
            }
            if (!runsGiven && result.Seeds.Count > 0) result.Runs = result.Seeds.Count * result.AnchorCount * result.StrategyCount;
            else if (!runsGiven && (result.Anchors.Count > 1 || result.Strategies.Count > 1)) result.Runs = result.AnchorCount * result.StrategyCount;
            options = result;
            return true;
        }

        private static bool TryAnchor(string value, out AnchorKind kind)
        {
            string flat = value.Replace(" ", string.Empty).Replace("_", string.Empty).Replace("-", string.Empty);
            if (string.Equals(flat, "Shrine", StringComparison.OrdinalIgnoreCase)) { kind = AnchorKind.Shrine; return true; }
            if (string.Equals(flat, "WarThrone", StringComparison.OrdinalIgnoreCase)) { kind = AnchorKind.WarThrone; return true; }
            kind = AnchorKind.Shrine;
            return false;
        }
    }

    /// <summary>
    /// The game speed the bot runs at. The scale is capped, and lowered when frames get long, so that no frame advances more
    /// than a fixed slice of game time: physics, the flow-field refresh and the units' own movement keep their usual step
    /// and nothing steps through a wall.
    /// </summary>
    public static class AutoplaySpeed
    {
        /// <summary>The highest time scale the runner uses.</summary>
        public const float MaxScale = 16f;

        /// <summary>The most game seconds one frame may advance.</summary>
        public const float MaxFrameGameSeconds = 0.1f;

        public const float NormalFixedStep = 0.02f;
        public const float FastFixedStep = 0.04f;

        /// <summary>The time scale for a frame that took <paramref name="frameSeconds"/> of real time.</summary>
        public static float Scale(float requested, float frameSeconds)
        {
            float wanted = Math.Max(1f, Math.Min(requested, MaxScale));
            if (frameSeconds <= 0f) return wanted;
            return Math.Max(1f, Math.Min(wanted, MaxFrameGameSeconds / frameSeconds));
        }

        /// <summary>The physics step for a time scale: the usual one up to double speed, a coarser one above, never so coarse a body steps through a wall.</summary>
        public static float FixedStep(float scale) => Math.Max(NormalFixedStep, Math.Min(FastFixedStep, NormalFixedStep * scale / 4f));
    }
}
