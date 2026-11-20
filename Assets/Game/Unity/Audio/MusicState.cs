namespace Isekai
{
    /// <summary>What the music is for. Capture and Victory share a track.</summary>
    public enum MusicState { None, Menu, Planet, RegionCalm, RegionTense, Captured, Victory, Defeat }

    /// <summary>Which <see cref="MusicState"/> fits the screen and the Region's situation.</summary>
    public static class MusicStates
    {
        /// <summary>Calm until Vigilance reaches this stage (or a Wave is alive); tense from then on.</summary>
        public const VigilanceStage TenseFrom = VigilanceStage.Alarmed;

        public static MusicState Pick(string scene, PlanetRun run, VigilanceDirector vigilance, bool regionEnded, RegionEnd outcome)
        {
            if (scene == RunSession.RegionScene)
            {
                if (regionEnded)
                {
                    if (outcome == RegionEnd.Defeated) return MusicState.Defeat;
                    return run != null && run.State == PlanetRunState.Won ? MusicState.Victory : MusicState.Captured;
                }
                bool tense = vigilance != null && (vigilance.Stage >= TenseFrom || vigilance.AliveWaves > 0);
                return tense ? MusicState.RegionTense : MusicState.RegionCalm;
            }
            if (scene == RunSession.PlanetScene)
            {
                if (run != null && run.State == PlanetRunState.Won) return MusicState.Victory;
                if (run != null && run.State == PlanetRunState.Lost) return MusicState.Defeat;
                return MusicState.Planet;
            }
            return MusicState.Menu;   // Boot, Menu and any other screen
        }
    }
}
