namespace Isekai
{
    /// <summary>How far each kind of unit sees in daylight, in cells. Night halves these (a later ticket sets the multiplier).</summary>
    public static class VisionRanges
    {
        public const int Character = 14;
        public const int Demon = 8;
        public const int Mortal = 7;
        public const int Wildlife = 6;

        /// <summary>The daylight range of a kind of unit (0 for a Building, which doesn't see).</summary>
        public static int For(UnitKind kind) => kind switch
        {
            UnitKind.Character => Character,
            UnitKind.Demon => Demon,
            UnitKind.Mortal => Mortal,
            UnitKind.Wildlife => Wildlife,
            _ => 0,
        };

        /// <summary>A range scaled by <paramref name="multiplier"/> (1 by day, 0.5 at night), rounded to whole cells, never below 1 for a unit that sees.</summary>
        public static int Scaled(int range, float multiplier)
        {
            if (range <= 0) return 0;
            int scaled = (int)(range * multiplier + 0.5f);
            return scaled < 1 ? 1 : scaled;
        }
    }
}
