using System;
using Blondie.GameSetup;

namespace Isekai
{
    /// <summary>
    /// What the main menu's Tutorial and Credits buttons do, as hooks the tickets that own those screens replace:
    /// ticket #30 sets <see cref="StartTutorial"/> (the guided first Region) and <see cref="OpenCredits"/> opens
    /// the credits screen. Until ticket #30 sets its hook, Tutorial starts a new Run.
    /// </summary>
    public static class MenuHooks
    {
        /// <summary>Runs when the player picks Tutorial. Placeholder: starts a new Run (asking first when a Run is saved).</summary>
        public static Action StartTutorial { get; set; } = GameSetup.Play;

        /// <summary>Runs when the player picks Credits: opens <see cref="CreditsScreen"/>.</summary>
        public static Action OpenCredits { get; set; } = CreditsScreen.Open;
    }
}
