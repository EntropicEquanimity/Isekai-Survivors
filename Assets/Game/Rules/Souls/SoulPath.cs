using System;
using System.Collections.Generic;

namespace Isekai
{
    /// <summary>A branch of Jack's per-Run soul tree (see CONTEXT.md: Soul path). Chosen on the loadout screen before a Run.</summary>
    public enum SoulPath
    {
        Brawler,
        Overlord,
        Warlord,
        Corruptor,
        Soulweaver,
        Assassin,
        Reaper,
        Bonelord,
    }

    /// <summary>The Soul paths a Run can start with, their names and what each is about.</summary>
    public static class SoulPaths
    {
        /// <summary>How many paths a Run starts with (more come through meta progression).</summary>
        public const int StartingSlots = 2;

        /// <summary>The paths open from the start (Plaguebringer and Architect come later).</summary>
        public static readonly SoulPath[] Starting =
        {
            SoulPath.Brawler, SoulPath.Overlord, SoulPath.Warlord, SoulPath.Corruptor,
            SoulPath.Soulweaver, SoulPath.Assassin, SoulPath.Reaper, SoulPath.Bonelord,
        };

        public static string Name(SoulPath path) => path.ToString();

        public static string About(SoulPath path) => path switch
        {
            SoulPath.Brawler => "Jack and the body he is in: an ordinary demon becomes a super-powered one.",
            SoulPath.Overlord => "All your demons grow stronger.",
            SoulPath.Warlord => "Demons near Jack rally and fight harder.",
            SoulPath.Corruptor => "Corruption spreads faster and further.",
            SoulPath.Soulweaver => "Spells and abilities.",
            SoulPath.Assassin => "Hiding, and speed and damage while hidden.",
            SoulPath.Reaper => "More Souls harvested, cheaper possession, slower drain.",
            SoulPath.Bonelord => "Bones and skeletons.",
            _ => string.Empty,
        };

        /// <summary>The paths a save wrote (unknown names are dropped), at most <paramref name="slots"/>.</summary>
        public static List<SoulPath> Parse(IEnumerable<string> names, int slots = StartingSlots)
        {
            var paths = new List<SoulPath>();
            if (names == null) return paths;
            foreach (string name in names)
                if (paths.Count < slots && Enum.TryParse(name, out SoulPath path) && Enum.IsDefined(typeof(SoulPath), path) && !paths.Contains(path))
                    paths.Add(path);
            return paths;
        }
    }

    /// <summary>The loadout screen's choice of Soul paths: up to a number of slots, picked and unpicked by toggling.</summary>
    public sealed class SoulPathPicks
    {
        private readonly List<SoulPath> _picked = new();

        public SoulPathPicks(int slots = SoulPaths.StartingSlots)
        {
            if (slots < 1) throw new ArgumentOutOfRangeException(nameof(slots));
            Slots = slots;
        }

        public int Slots { get; }

        public IReadOnlyList<SoulPath> Picked => _picked;

        /// <summary>Every slot is filled: the Run can start.</summary>
        public bool IsFull => _picked.Count >= Slots;

        public bool Has(SoulPath path) => _picked.Contains(path);

        /// <summary>Picks a path, or unpicks it if picked. With every slot full, picking a new one drops the oldest pick.</summary>
        public void Toggle(SoulPath path)
        {
            if (_picked.Remove(path)) return;
            if (IsFull) _picked.RemoveAt(0);
            _picked.Add(path);
        }
    }
}
