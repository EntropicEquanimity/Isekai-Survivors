using System;
using System.Linq;
using Blondie.GameSetup;
using Blondie.GridInventory;
using Blondie.Roguelike;
using Blondie.Roguelike.Boons;
using UnityEngine.SceneManagement;

namespace Isekai
{
    /// <summary>
    /// The Run in progress, kept across scene loads: the <see cref="PlanetRun"/>, the Character's inventory between
    /// Regions (Run-lifetime Items carry over), and moving between the Planet map and Regions. Blondie Roguelike's Run
    /// (seed, drafts, boons) starts and ends alongside it.
    /// </summary>
    public static class RunSession
    {
        public const string PlanetScene = "Planet";
        public const string RegionScene = "Region";
        private static readonly int[] Rings = { 6, 4, 3 };

        private static GridInventorySaveData s_inventory;

        // Enter Play Mode Options keep statics between plays: forget the Run each time.
        static RunSession() => Blondie.Core.StaticReset.Register(Clear);

        public static PlanetRun Run { get; private set; }
        public static int Seed { get; private set; }
        public static bool HasRun => Run != null && !Run.IsOver;

        /// <summary>Starts a new Run on a new Planet.</summary>
        public static PlanetRun StartNew(int? seed = null) => StartNew(seed, Rings);

        /// <summary>Starts a new Run on a Planet with these rings (tests use small Planets).</summary>
        internal static PlanetRun StartNew(int? seed, int[] rings)
        {
            Seed = seed ?? Environment.TickCount;
            Run = new PlanetRun(Planet.Generate(Seed, rings, RegionBuffs.Ids));
            s_inventory = null;
            RunBoons.Clear();
            if (RunManager.HasActiveRun) RunManager.EndRun(RunOutcome.Abandoned);
            RunManager.StartRun();
            return Run;
        }

        /// <summary>
        /// A Region scene opened without the Planet map (in the Editor, or by tests): starts a Run and lands on an edge
        /// Region so the Region still plays.
        /// </summary>
        public static RegionInfo EnsureInRegion()
        {
            if (!HasRun) StartNew();
            if (Run.State == PlanetRunState.Choosing) Run.Enter(Run.Available().First());
            return Run.Current;
        }

        /// <summary>Enters <paramref name="region"/> and loads the Region scene.</summary>
        public static bool GoToRegion(RegionInfo region)
        {
            if (!HasRun || !Run.Enter(region)) return false;
            Load(RegionScene);
            return true;
        }

        public static void GoToPlanet() => Load(PlanetScene);

        /// <summary>Keeps the Character's inventory for the next Region.</summary>
        public static void StoreInventory(GridInventorySaveData data) => s_inventory = data;

        /// <summary>The stored inventory, once (null when there is none).</summary>
        public static GridInventorySaveData TakeInventory()
        {
            GridInventorySaveData data = s_inventory;
            s_inventory = null;
            return data;
        }

        public static void End(bool won)
        {
            if (Run == null) return;
            if (won) Run.CaptureCurrent();
            else Run.Lose();
            if (RunManager.HasActiveRun) RunManager.EndRun(won ? RunOutcome.Won : RunOutcome.Lost);
        }

        /// <summary>Forgets the Run (tests, quitting to the menu).</summary>
        public static void Clear()
        {
            Run = null;
            s_inventory = null;
        }

        private static void Load(string scene)
        {
            if (GameSetup.IsRunning) GameScenes.Load(scene);
            else SceneManager.LoadScene(scene);
        }
    }
}
