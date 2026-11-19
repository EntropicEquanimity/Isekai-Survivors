using System;
using Blondie.GameSetup;

namespace Isekai
{
    /// <summary>
    /// What the main menu's Tutorial and Credits buttons do, as hooks the tickets that own those screens replace:
    /// ticket #30 sets <see cref="StartTutorial"/> (the guided first Region) and ticket #34 sets <see cref="OpenCredits"/>
    /// (the credits screen). Until then Tutorial starts a new Run and Credits shows a placeholder.
    /// </summary>
    public static class MenuHooks
    {
        /// <summary>Runs when the player picks Tutorial. Placeholder: starts a new Run (asking first when a Run is saved).</summary>
        public static Action StartTutorial { get; set; } = GameSetup.Play;

        /// <summary>Runs when the player picks Credits. Placeholder: a screen saying the credits are coming.</summary>
        public static Action OpenCredits { get; set; } = CreditsPlaceholder.Open;
    }

    /// <summary>The stand-in credits screen, until ticket #34 sets <see cref="MenuHooks.OpenCredits"/>.</summary>
    internal static class CreditsPlaceholder
    {
        public const string MenuName = "GameCredits";

        public static void Open()
        {
            if (MenuStack.Contains(MenuName)) return;
            var panel = new MenuPanel(MenuName, "Credits");
            panel.Body.Add(GameStyle.Label("The credits are coming soon.", TextRole.Muted));
            panel.Footer.Add(new MenuButton("common.back", panel.Close, primary: true));
            panel.Open();
        }
    }
}
