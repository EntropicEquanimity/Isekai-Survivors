using System;
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

        /// <summary>Reads the saved Run (the slot must be loaded, <see cref="SaveSystem.Load(int)"/>) and makes it the Run in progress.</summary>
        public static bool Restore()
        {
            Data data = SaveSystem.GetValue<Data>(Key);
            if (data == null || data.version != Version || data.captured == null || data.rings == null || data.rings.Length == 0) return false;
            AnchorKind? anchor = Enum.IsDefined(typeof(AnchorKind), data.anchor) ? (AnchorKind)data.anchor : null;
            RunSession.Restore(data.seed, data.rings, data.captured, data.runSeconds, anchor, data.hasInventory ? data.inventory : null, data.escapedRulers, data.rulersSlain);
            SoulRun.Restore(data.soulLevel, data.soulDeaths);
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
