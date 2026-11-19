namespace Isekai
{
    /// <summary>What a Portal's demons do while nothing else is going on: its standing order.</summary>
    public enum PortalOrder
    {
        /// <summary>Fight what comes near the Portal's rally point, then come back to it.</summary>
        Guard,

        /// <summary>Go to the target marked on the map and fight there.</summary>
        Raid,

        /// <summary>Stay near Jack and fight what he fights.</summary>
        Follow,
    }

    /// <summary>What a Portal's demons do about bone piles.</summary>
    public enum BonesPolicy
    {
        /// <summary>Keep the piles for the Portal's bone demons, which spend them (once each) to raise skeletons.</summary>
        Harvest,

        /// <summary>Remove the piles near its demons, so mortals cannot find them and raise an alarm.</summary>
        Destroy,

        /// <summary>Leave the piles where they are.</summary>
        Ignore,
    }

    /// <summary>The kinds of demon a Portal summons or raises. The first two are the Portal's roster.</summary>
    public enum DemonType
    {
        /// <summary>The basic Portal demon (Demon_A).</summary>
        Imp,

        /// <summary>Spends bones to raise skeletons.</summary>
        BoneDemon,

        /// <summary>Raised from a bone pile; crumbles after a few minutes.</summary>
        Skeleton,
    }
}
