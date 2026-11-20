using System;
using Blondie.GameSetup;

namespace Isekai
{
    /// <summary>
    /// What the main menu's Tutorial and Credits buttons do, as hooks the tickets that own those screens replace:
    /// <see cref="StartTutorial"/> starts the guided first Region (ticket #30) and <see cref="OpenCredits"/> opens the
    /// credits screen (ticket #34).
    /// </summary>
    public static class MenuHooks
    {
        /// <summary>Runs when the player picks Tutorial: starts the guided first Region run.</summary>
        public static Action StartTutorial { get; set; } = TutorialRun.StartFromMenu;

        /// <summary>Runs when the player picks Credits: opens <see cref="CreditsScreen"/>.</summary>
        public static Action OpenCredits { get; set; } = CreditsScreen.Open;
    }
}
