namespace Isekai
{
    /// <summary>Every number of the Portal demons, in one place to tune.</summary>
    public static class DemonTuning
    {
        /// <summary>How far from its post a demon chases under each order (cells).</summary>
        public const float GuardLeash = 6f;
        public const float RaidLeash = 8f;
        public const float FollowLeash = 5f;

        /// <summary>The radius of the slots the Portal's demons stand in around the point they gather at.</summary>
        public const float SlotRadius = 2f;

        /// <summary>Seconds between a demon's thoughts (each is staggered by a random start).</summary>
        public const float ThinkSeconds = 0.3f;

        /// <summary>A fleeing demon looks for hostile units this far round it and runs this many cells away from the nearest each time it thinks.</summary>
        public const float FleeSight = 8f;
        public const float FleeStep = 6f;

        /// <summary>A Destroy policy removes the piles within this many cells of a demon.</summary>
        public const float DestroyRadius = 3f;

        /// <summary>A bone demon walks to piles within this many cells of its post.</summary>
        public const float BoneSearchRadius = 10f;

        /// <summary>A bone demon takes a pile once it is this close.</summary>
        public const float BoneReach = 1.1f;

        /// <summary>Seconds a raised skeleton lasts ("a few minutes").</summary>
        public const float SkeletonSeconds = 180f;

        /// <summary>The most skeletons one bone demon keeps up at once, and the wait between two raisings.</summary>
        public const int SkeletonsPerDemon = 3;
        public const float RaiseCooldown = 4f;
    }
}
