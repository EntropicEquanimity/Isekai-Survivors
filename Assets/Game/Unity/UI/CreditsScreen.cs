using System;
using Blondie.Core;
using Blondie.GameSetup;
using UnityEngine.UIElements;

namespace Isekai
{
    /// <summary>
    /// The credits: every third-party asset in the build (<see cref="CreditsList"/>) with its title, author and licence, the
    /// title being a link to the source when there is one. A wide Game Setup window like Settings, so Back and Escape close it
    /// the same way. Assets whose licence is not confirmed are shown in the warning colour.
    /// </summary>
    public static class CreditsScreen
    {
        public const string MenuName = "GameCredits";

        public const string EntryClass = "isekai-credit";
        public const string TitleClass = "isekai-credit-title";
        public const string AuthorClass = "isekai-credit-author";
        public const string LicenceClass = "isekai-credit-licence";

        /// <summary>Opens a link; tests replace it so no browser starts.</summary>
        public static Action<string> OpenUrl { get; set; } = url => AppEnvironment.OpenUrl(url);

        /// <summary>The panel while the screen is open, else null.</summary>
        public static MenuPanel Current { get; private set; }

        public static void Open()
        {
            if (MenuStack.Contains(MenuName)) return;
            var panel = new MenuPanel(MenuName, "Credits", wide: true);
            Current = panel;
            panel.Menu.Closed += () => { if (Current == panel) Current = null; };

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1;
            panel.Body.Add(scroll);
            Fill(scroll);
            panel.Footer.Add(new MenuButton("common.back", panel.Close, primary: true));
            panel.Open();
        }

        private static void Fill(VisualElement parent)
        {
            parent.Add(GameStyle.Label("Isekai Conquerors is made with the help of these assets and tools. Thank you to everyone who made them.",
                TextRole.Muted));
            foreach (string group in CreditsList.Groups)
            {
                var header = GameStyle.Label(group, TextRole.Accent);
                header.style.marginTop = GameStyle.FontSize;
                parent.Add(header);
                foreach (CreditEntry entry in CreditsList.Entries)
                    if (entry.Group == group) parent.Add(Row(entry));
            }
        }

        private static VisualElement Row(CreditEntry entry)
        {
            var row = new VisualElement();
            row.AddToClassList(EntryClass);
            row.style.marginTop = GameStyle.FontSize / 2;

            VisualElement title;
            if (string.IsNullOrWhiteSpace(entry.Url) || !IsWebLink(entry.Url)) title = GameStyle.Label(entry.Title);
            else
            {
                string url = entry.Url;
                var link = new MenuButton(entry.Title, () => OpenUrl?.Invoke(url));
                link.RemoveFromClassList(MenuButton.ButtonClass);
                link.AddToClassList("blondie-gs-link");
                link.tooltip = url;
                title = link;
            }
            title.AddToClassList(TitleClass);
            row.Add(title);

            var author = GameStyle.Label("By " + entry.Author, TextRole.Muted);
            author.AddToClassList(AuthorClass);
            row.Add(author);

            var licence = GameStyle.Label(entry.Licence, entry.Confirmed ? TextRole.Muted : TextRole.Warning);
            licence.AddToClassList(LicenceClass);
            row.Add(licence);
            return row;
        }

        /// <summary>Only http and https links are opened.</summary>
        public static bool IsWebLink(string url) =>
            url.StartsWith("https://", StringComparison.Ordinal) || url.StartsWith("http://", StringComparison.Ordinal);
    }
}
