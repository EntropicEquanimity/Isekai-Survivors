namespace Isekai
{
    /// <summary>
    /// Named places in the Region's startup, for the setup and populate orders of Region steps (lower runs first). A
    /// step picks the slot it belongs in instead of a number; steps in one slot run in no fixed order. Setup runs
    /// before the walls are baked, populate after.
    /// </summary>
    public static class RegionOrder
    {
        // Setup: the ground and its rules come before settings that build on them.
        public const int Terrain = -300;
        public const int Settings = 0;

        // Populate: ground features first, then what stands on it, the wild last.
        public const int Settlements = -200;
        public const int Roads = -100;
        public const int Wilds = 0;
        public const int Patrols = 100;
    }
}
