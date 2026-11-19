using System;

namespace Isekai
{
    /// <summary>
    /// What a Region is made from: its seed and map size (more settings join as the generator grows). The same recipe
    /// lays out the same Region. Tests set one before the Region loads; a Run derives one from its seed.
    /// </summary>
    public sealed class RegionRecipe
    {
        /// <summary>The hand-built Region's map size, in cells.</summary>
        public const int DefaultWidth = 288, DefaultHeight = 192;

        public RegionRecipe(int seed, int width = DefaultWidth, int height = DefaultHeight)
        {
            if (width < 1) throw new ArgumentOutOfRangeException(nameof(width));
            if (height < 1) throw new ArgumentOutOfRangeException(nameof(height));
            Seed = seed;
            Width = width;
            Height = height;
        }

        public int Seed { get; }
        public int Width { get; }
        public int Height { get; }

        public override string ToString() => $"seed {Seed}, {Width}x{Height}";
    }
}
