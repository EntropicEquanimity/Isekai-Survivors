using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// The debug menu's Autoplay rows: start a batch of one bot Run with a strategy, stop it, and read what it is doing. They
    /// work on any screen, so they register at startup (like the speed rows). The batch plays at game speed 8 and leaves the
    /// game running when it is done; the command line and the Editor menu start longer batches.
    /// </summary>
    public static class AutoplayDebug
    {
        private const string Section = "Autoplay";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Register()
        {
            if (!Application.isEditor && !Debug.isDebugBuild) return;
            DebugActions.Remove(typeof(AutoplayDebug));
            foreach (AutoplayStrategy strategy in AutoplayStrategies.All)
            {
                AutoplayStrategy chosen = strategy;
                DebugActions.Button(typeof(AutoplayDebug), Section, $"Autoplay: one {chosen.Name} Run", () => Start(chosen), "start");
            }
            DebugActions.Button(typeof(AutoplayDebug), Section, "Autoplay: stop", AutoplayRunner.Stop);
            DebugActions.Info(typeof(AutoplayDebug), Section, () => AutoplayRunner.Current != null ? AutoplayRunner.Current.Status : "Autoplay: not running");
        }

        private static void Start(AutoplayStrategy strategy)
        {
            var options = new AutoplayOptions { Runs = 1, Strategy = strategy, Anchor = RunSession.Anchor ?? AnchorKind.Shrine, Speed = 8f };
            AutoplayRunner.Begin(options);
        }
    }
}
