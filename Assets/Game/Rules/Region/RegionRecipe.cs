using System;

namespace Isekai
{
    /// <summary>
    /// What a Region is made from: its seed and map size (more settings join as the generator grows). The same recipe
    /// lays out the same Region. Tests set one before the Region loads; a Run derives one from its seed.
    /// </summary>
    public sealed class RegionRecipe
    {
        /// <summary>An outer-ring Region's map size, in cells, when no recipe asks for another.</summary>
        public const int DefaultWidth = 160, DefaultHeight = 110;

        public RegionRecipe(int seed, int width = DefaultWidth, int height = DefaultHeight, RingRecipe ring = null)
        {
            if (width < 1) throw new ArgumentOutOfRangeException(nameof(width));
            if (height < 1) throw new ArgumentOutOfRangeException(nameof(height));
            Seed = seed;
            Width = width;
            Height = height;
            Ring = ring ?? RingRecipe.Of(RegionRing.Outer);
        }

        /// <summary>A recipe at the size its ring asks for.</summary>
        public static RegionRecipe For(int seed, RingRecipe ring) => new(seed, ring.Width, ring.Height, ring);

        public int Seed { get; }
        public int Width { get; }
        public int Height { get; }

        /// <summary>What the Region's ring changes about it (the outer ring unless the recipe says otherwise).</summary>
        public RingRecipe Ring { get; }

        public override string ToString() => $"seed {Seed}, {Width}x{Height}, {Ring.Ring} ring";
    }
}
