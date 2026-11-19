using System.Collections.Generic;
using Blondie.Core;

namespace Isekai
{
    /// <summary>
    /// The game's wording for text Blondie packages show (Core's <see cref="TextLocalization"/> keys), e.g. the skill
    /// tree's "Points" are Shards here. Other keys go to the localizer that was installed before, else their fallback.
    /// </summary>
    public sealed class GameText : ITextLocalizer
    {
        private static readonly Dictionary<string, string> Overrides = new()
        {
            ["skilltree.ui.points"] = "Shards: {points}",
            ["skilltree.ui.cost"] = "{cost} Shards per rank",
            ["skilltree.fail.points"] = "Not enough Shards",
        };

        private readonly ITextLocalizer _inner;

        private GameText(ITextLocalizer inner) => _inner = inner;

        /// <summary>Installs the game's wording (again after each Play Mode start, which clears the localizer).</summary>
        public static void Install()
        {
            if (TextLocalization.Localizer is GameText) return;
            TextLocalization.Localizer = new GameText(TextLocalization.Localizer);
        }

        public string Resolve(string key, string fallback) =>
            Overrides.TryGetValue(key, out string text) ? text : _inner != null ? _inner.Resolve(key, fallback) : fallback;
    }
}
