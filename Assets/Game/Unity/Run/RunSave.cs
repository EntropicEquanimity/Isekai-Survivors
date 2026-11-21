using System;
using System.Linq;
using Blondie.GridInventory;
using Blondie.Save;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// The Run's save, through Blondie Save (one slot). It is written on the Planet screen between Regions and never
    /// mid-Region, holds what <see cref="RunSession"/> and <see cref="SoulRun"/> keep between scenes, and is deleted when
    /// the Run is won or lost (<see cref="RunSession.End"/>). The Planet is not stored: its seed and rings make it again.
    /// </summary>
    public static class RunSave
    {
        public const int Slot = 0;

        private const string Key = "isekai.run";
        private const int Version = 1;

        /// <summary>Everything a saved Run holds.</summary>
        [Serializable]
        public sealed class Data
        {
            public int version = Version;
            public int seed;
            public int[] rings = Array.Empty<int>();
            public string[] captured = Array.Empty<string>();
            public EscapedRuler[] escapedRulers = Array.Empty<EscapedRuler>();
            public float runSeconds;
            public int rulersSlain;
            /// <summary>The Anchor's <see cref="AnchorKind"/>, or -1 when none was picked.</summary>
            public int anchor = -1;
            /// <summary>The Soul paths picked on the loadout screen, by name.</summary>
            public string[] paths = Array.Empty<string>();
            /// <summary>The Run was saved on the loadout screen: Continue opens it there again.</summary>
            public bool loadoutPending;
            public int soulLevel;
            public int soulDeaths;
            public bool hasInventory;
            public GridInventorySaveData inventory;
        }

        /// <summary>True when a Run is saved and can be continued.</summary>
        public static bool Exists => SaveSystem.HasSave(Slot);

        /// <summary>What a save would hold now, or null when the Run is not on the Planet map (none, in a Region, or over).</summary>
        public static Data Capture()
        {
            PlanetRun run = RunSession.Run;
            if (run == null || run.State != PlanetRunState.Choosing) return null;
            SoulLedger souls = SoulRun.Ledger;
            GridInventorySaveData inventory = RunSession.PeekInventory();
            var captured = new string[run.CapturedInOrder.Count];
            for (int i = 0; i < captured.Length; i++) captured[i] = run.CapturedInOrder[i];
            var escaped = new EscapedRuler[run.EscapedRulers.Count];
            for (int i = 0; i < escaped.Length; i++)
            {
                EscapedRuler ruler = run.EscapedRulers[i];
                TraitId[] traits = ruler.Traits != null && ruler.Traits.Length > 0 ? (TraitId[])ruler.Traits.Clone() : Array.Empty<TraitId>();
                escaped[i] = new EscapedRuler(ruler.FromRegionId, ruler.ToRegionId, ruler.Name, traits);
            }
            var rings = new int[RunSession.RingSizes.Count];
            for (int i = 0; i < rings.Length; i++) rings[i] = RunSession.RingSizes[i];
            return new Data
            {
                seed = RunSession.Seed,
                rings = rings,
                captured = captured,
                escapedRulers = escaped,
                runSeconds = run.RunSeconds,
                rulersSlain = run.RulersSlain,
                anchor = RunSession.Anchor is { } kind ? (int)kind : -1,
                paths = RunSession.Paths.Select(p => p.ToString()).ToArray(),
                loadoutPending = RunSession.LoadoutPending,
                soulLevel = souls.Level,
                soulDeaths = souls.Deaths,
                hasInventory = inventory != null,
                inventory = inventory,
            };
        }

        /// <summary>Writes the Run to the save slot. False when there is nothing to save (see <see cref="Capture"/>) or writing failed.</summary>
        public static bool Save()
        {
            Data data = Capture();
            if (data == null) return false;
            SaveSystem.SetValue(Key, data);
            return SaveSystem.Save(Slot, $"Run: {data.captured.Length} Regions captured");
        }

        private static int s_pendingSlot = -1;

        static RunSave() => Blondie.Core.StaticReset.Register(() => s_pendingSlot = -1);

        /// <summary>
        /// Continue was pressed: the save in <paramref name="slot"/> is loaded once the Planet screen opens
        /// (<see cref="RestorePending"/>), behind the fade, instead of on the main menu where reading it froze the screen.
        /// </summary>
        public static void RestoreWhenPlanetOpens(int slot) => s_pendingSlot = slot;

        /// <summary>Loads and restores the save Continue asked for, if any (the Planet screen calls it first). True when a Run came back.</summary>
        public static bool RestorePending()
        {
            int slot = s_pendingSlot;
            s_pendingSlot = -1;
            return slot >= 0 && SaveSystem.Load(slot) && Restore();
        }

        /// <summary>Reads the saved Run (the slot must be loaded, <see cref="SaveSystem.Load(int)"/>) and makes it the Run in progress.</summary>
        public static bool Restore()
        {
            Data data = SaveSystem.GetValue<Data>(Key);
            if (data == null || data.version != Version || data.captured == null || data.rings == null || data.rings.Length == 0) return false;
            AnchorKind? anchor = Enum.IsDefined(typeof(AnchorKind), data.anchor) ? (AnchorKind)data.anchor : null;
            RunSession.Restore(data.seed, data.rings, data.captured, data.runSeconds, anchor, data.hasInventory ? data.inventory : null, data.escapedRulers, data.rulersSlain);
            SoulRun.Restore(data.soulLevel, data.soulDeaths);
            RunSession.SetPaths(SoulPaths.Parse(data.paths));
            RunSession.LoadoutPending = data.loadoutPending;
            return true;
        }

        /// <summary>Forgets the save (a new, won or lost Run).</summary>
        public static void Delete()
        {
            SaveSystem.DeleteKey(Key);
            SaveSystem.Delete(Slot);
        }
    }
}
