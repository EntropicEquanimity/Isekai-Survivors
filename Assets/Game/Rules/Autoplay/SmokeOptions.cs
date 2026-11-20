using System;
using System.Collections.Generic;
using System.Globalization;

namespace Isekai
{
    /// <summary>
    /// What <c>-smoke</c> asks of a built player: the scripted tour from the main menu through the tutorial Region and the
    /// credits and back to the menu, then quit (see <c>SmokeTour</c>). Words after the flag are <c>key=value</c>:
    /// <c>seconds</c> (real seconds the tutorial Region runs, default 20) and <c>speed</c> (game speed while it does, default 4, at most 16).
    /// </summary>
    public sealed class SmokeOptions
    {
        public const string Flag = "-smoke";
        public const float DefaultSeconds = 20f;
        public const float DefaultSpeed = 4f;
        public const float MaxSpeed = 16f;

        public float Seconds { get; set; } = DefaultSeconds;
        public float Speed { get; set; } = DefaultSpeed;

        /// <summary>Quit the application when the tour is done (the command line does; tests leave it running).</summary>
        public bool QuitWhenDone { get; set; } = true;

        /// <summary>Reads the words after <c>-smoke</c>. True with null options when the flag is absent; false with a message when a word is wrong.</summary>
        public static bool TryParse(IReadOnlyList<string> args, out SmokeOptions options, out string error)
        {
            options = null;
            error = null;
            int flag = -1;
            for (int i = 0; i < args.Count; i++)
                if (string.Equals(args[i], Flag, StringComparison.OrdinalIgnoreCase)) { flag = i; break; }
            if (flag < 0) return true;

            var result = new SmokeOptions();
            for (int i = flag + 1; i < args.Count && !args[i].StartsWith("-", StringComparison.Ordinal); i++)
            {
                int eq = args[i].IndexOf('=');
                if (eq <= 0) { error = $"smoke: '{args[i]}' is not key=value"; return false; }
                string key = args[i].Substring(0, eq).Trim().ToLowerInvariant();
                string value = args[i].Substring(eq + 1).Trim();
                if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float number) || number <= 0f)
                {
                    error = $"smoke: {key} must be a number above 0, not '{value}'";
                    return false;
                }
                switch (key)
                {
                    case "seconds": result.Seconds = number; break;
                    case "speed": result.Speed = Math.Min(number, MaxSpeed); break;
                    default: error = $"smoke: unknown option '{key}'"; return false;
                }
            }
            options = result;
            return true;
        }
    }

    /// <summary>The exit codes of a smoke tour.</summary>
    public static class SmokeExit
    {
        public const int Clean = 0;
        public const int ErrorsLogged = 2;
        public const int Stalled = 3;

        public static int Code(bool stalled, int errorsLogged) => stalled ? Stalled : errorsLogged > 0 ? ErrorsLogged : Clean;
    }
}
