using System;
using Blondie.Core;

namespace Isekai
{
    /// <summary>The corruption rules on a <see cref="CellLayer"/>: a level 0 to 4 per ground cell.</summary>
    public sealed class CorruptionField
    {
        public CorruptionField(CellLayer layer) => Layer = layer ?? throw new ArgumentNullException(nameof(layer));

        public CellLayer Layer { get; }

        /// <summary>The level at a cell (0 outside the map).</summary>
        public int Level(Int2 cell) => Layer.Get(cell);

        /// <summary>Sets a level, kept between 0 and the maximum; true when it changed.</summary>
        public bool Set(Int2 cell, int level) => Layer.Set(cell, (byte)Math.Max(0, Math.Min(CorruptionTuning.MaxLevel, level)));

        /// <summary>Raises a cell one level (a dying demon); false at the maximum.</summary>
        public bool Raise(Int2 cell) => Layer.Contains(cell) && Set(cell, Level(cell) + 1);

        /// <summary>Raises a cell by <paramref name="by"/> levels, never past <paramref name="cap"/> (and never lowering it).</summary>
        public bool RaiseTo(Int2 cell, int cap, int by)
        {
            int level = Level(cell);
            return level < cap && Set(cell, Math.Min(cap, level + by));
        }

        /// <summary>Lowers a cell one level (a priest); false when it is clean already.</summary>
        public bool Purify(Int2 cell) => Layer.Contains(cell) && Level(cell) > 0 && Set(cell, Level(cell) - 1);

        /// <summary>Gives every cell within <paramref name="radius"/> of <paramref name="centre"/> at least <paramref name="level"/> (the Anchor's ground at the start).</summary>
        public void Seed(Int2 centre, int radius, int level)
        {
            for (int dy = -radius; dy <= radius; dy++)
            for (int dx = -radius; dx <= radius; dx++)
            {
                var cell = new Int2(centre.x + dx, centre.y + dy);
                if (Level(cell) < level) Set(cell, level);
            }
        }

        /// <summary>Whether a Building's footprint (its lower-left cell and size) is corrupted enough: every cell at the build level or higher.</summary>
        public bool CanBuild(Int2 origin, Int2 size)
        {
            for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
                if (Level(new Int2(origin.x + x, origin.y + y)) < CorruptionTuning.BuildLevel) return false;
            return true;
        }
    }

    /// <summary>Corruption's odds and multipliers.</summary>
    public static class CorruptionRules
    {
        /// <summary>Whether a dying demon raises its cell, rolled on the seeded <paramref name="random"/> given.</summary>
        public static bool DyingDemonSpreads(Random random, float chance = CorruptionTuning.DyingDemonChance) => random.NextDouble() < chance;

        /// <summary>Whether a demon is weak on ground at <paramref name="level"/>.</summary>
        public static bool IsWeak(int level) => level < CorruptionTuning.BuildLevel;

        /// <summary>What a demon's damage is multiplied by on ground at <paramref name="level"/>.</summary>
        public static float DamageMultiplier(int level) => IsWeak(level) ? CorruptionTuning.WeakMultiplier : 1f;
    }

    /// <summary>A priest's purify clock: one purify each <see cref="CorruptionTuning.PurifySeconds"/> it spends near its settlement.</summary>
    public sealed class PurifyClock
    {
        private float _clock;

        public PurifyClock(float seconds = CorruptionTuning.PurifySeconds) => Seconds = seconds;

        public float Seconds { get; }

        /// <summary>Lets time pass; how many purifies are due now.</summary>
        public int Tick(float seconds)
        {
            _clock += seconds;
            int due = 0;
            while (_clock >= Seconds)
            {
                _clock -= Seconds;
                due++;
            }
            return due;
        }
    }
}
