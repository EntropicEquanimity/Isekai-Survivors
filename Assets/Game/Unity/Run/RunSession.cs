using System;
using System.Collections.Generic;
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
        private static int[] s_rings = Rings;

        // Enter Play Mode Options keep statics between plays: forget the Run each time.
        static RunSession() => Blondie.Core.StaticReset.Register(Clear);

        public static PlanetRun Run { get; private set; }
        public static int Seed { get; private set; }
        public static bool HasRun => Run != null && !Run.IsOver;

        /// <summary>The Anchor this Run picked on the Planet screen (null: none picked, the Region keeps its scene's Main Base).</summary>
        public static AnchorKind? Anchor { get; set; }

        /// <summary>Starts a new Run on a new Planet.</summary>
        public static PlanetRun StartNew(int? seed = null) => StartNew(seed, Rings);

        /// <summary>Starts a new Run on a Planet with these rings (tests use small Planets).</summary>
        internal static PlanetRun StartNew(int? seed, int[] rings)
        {
            Seed = seed ?? Environment.TickCount;
            s_rings = rings;
            Run = new PlanetRun(Planet.Generate(Seed, rings, RegionBuffs.Ids));
            s_inventory = null;
            Anchor = null;
            RunBoons.Clear();
            if (RunManager.HasActiveRun) RunManager.EndRun(RunOutcome.Abandoned);
            RunManager.StartRun(seed: unchecked((ulong)(uint)Seed));   // Blondie's Run streams follow the Planet's seed
            return Run;
        }

        /// <summary>The ring sizes of this Run's Planet, kept so a saved Run regenerates the same Planet.</summary>
        public static IReadOnlyList<int> RingSizes => s_rings;

        /// <summary>
        /// Rebuilds a saved Run on the Planet map: the same Planet (seed and rings), the same Regions captured in the same
        /// order, the Run clock, the buffs those captures gave, the Anchor and the Items carried between Regions.
        /// </summary>
        public static PlanetRun Restore(int seed, int[] rings, IReadOnlyList<string> captured, float runSeconds, AnchorKind? anchor, GridInventorySaveData inventory)
        {
            PlanetRun run = StartNew(seed, rings);
            for (int i = 0; i < captured.Count; i++)
            {
                RegionInfo region = run.Planet[captured[i]];
                if (region == null || !run.Enter(region)) break;   // a save that no longer fits the Planet: keep what fits
                if (i == captured.Count - 1) run.Tick(runSeconds);   // the clock only runs in a Region
                run.CaptureCurrent();
            }
            foreach (string id in run.Buffs)
            {
                Boon buff = RegionBuffs.Get(id);
                if (buff != null) RunBoons.Add(buff);
            }
            Anchor = anchor;
            s_inventory = inventory;
            return run;
        }

        /// <summary>The Items carried between Regions, without taking them (what a save writes).</summary>
        public static GridInventorySaveData PeekInventory() => s_inventory;

        /// <summary>The player gave the Run up: it ends as lost (and its save is deleted).</summary>
        public static void Abandon() => End(won: false);

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

        /// <summary>Ends the Run as won or lost.</summary>
        public static void End(bool won)
        {
            if (Run == null || Run.IsOver) return;
            if (won) Run.CaptureCurrent();
            else Run.Lose();
            if (RunManager.HasActiveRun) RunManager.EndRun(won ? RunOutcome.Won : RunOutcome.Lost);
            RunSave.Delete();   // a won or lost Run can't be continued
        }

        /// <summary>Forgets the Run (tests, quitting to the menu).</summary>
        public static void Clear()
        {
            Run = null;
            Anchor = null;
            s_inventory = null;
        }

        private static void Load(string scene)
        {
            if (GameSetup.IsRunning) GameScenes.Load(scene);
            else SceneManager.LoadScene(scene);
        }
    }
}
