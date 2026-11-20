namespace Isekai
{
    /// <summary>
    /// One third-party asset in the build, as the credits screen and docs/LICENSES.md list it. <see cref="Licence"/> is the
    /// terms as far as they are known; when <see cref="Confirmed"/> is false the owner still has to check them before release.
    /// </summary>
    public sealed class CreditEntry
    {
        public CreditEntry(string group, string title, string author, string licence, string url, bool confirmed = true, string note = "")
        {
            Group = group;
            Title = title;
            Author = author;
            Licence = licence;
            Url = url;
            Confirmed = confirmed;
            Note = note;
        }

        /// <summary>The section it is listed under (Music, Fonts, ...).</summary>
        public string Group { get; }

        public string Title { get; }

        public string Author { get; }

        public string Licence { get; }

        /// <summary>Where the asset comes from; empty when the source is not known.</summary>
        public string Url { get; }

        /// <summary>False when the licence is unknown or unclear ("licence unconfirmed").</summary>
        public bool Confirmed { get; }

        /// <summary>What it is used for, or what is missing.</summary>
        public string Note { get; }
    }
}
