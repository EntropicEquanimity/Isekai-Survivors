using System;

namespace Isekai
{
    /// <summary>Where a demon stands and how far it chases from there: the result of its Portal's order.</summary>
    public readonly struct DemonPost
    {
        public DemonPost(PortalOrder order, float x, float y, float leash)
        {
            Order = order;
            X = x;
            Y = y;
            Leash = leash;
        }

        /// <summary>The order actually followed (a Raid with no mark, or a Follow with no Jack, falls back to Guard).</summary>
        public PortalOrder Order { get; }

        public float X { get; }
        public float Y { get; }
        public float Leash { get; }
    }

    /// <summary>Turns a Portal's standing order into the point its demons gather at (engine-free: the points are plain numbers).</summary>
    public static class DemonPosts
    {
        public static float Leash(PortalOrder order) => order switch
        {
            PortalOrder.Raid => DemonTuning.RaidLeash,
            PortalOrder.Follow => DemonTuning.FollowLeash,
            _ => DemonTuning.GuardLeash,
        };

        /// <summary>
        /// The post for an order. Guard gathers at the rally point, Raid at the mark, Follow at Jack. An order whose
        /// point is missing (no mark yet, Jack has no body) does what Guard does.
        /// </summary>
        public static DemonPost Resolve(PortalOrder order, float rallyX, float rallyY, bool hasMark, float markX, float markY, bool hasJack, float jackX, float jackY)
        {
            if (order == PortalOrder.Raid && hasMark) return new DemonPost(order, markX, markY, Leash(order));
            if (order == PortalOrder.Follow && hasJack) return new DemonPost(order, jackX, jackY, Leash(order));
            return new DemonPost(PortalOrder.Guard, rallyX, rallyY, Leash(PortalOrder.Guard));
        }

        /// <summary>The offset of the <paramref name="index"/>th demon's slot around a gathering point, so demons do not stand on each other.</summary>
        public static void SlotOffset(int index, out float dx, out float dy)
        {
            double angle = Math.Max(0, index) * 2.399963;   // the golden angle
            dx = (float)Math.Cos(angle) * DemonTuning.SlotRadius;
            dy = (float)Math.Sin(angle) * DemonTuning.SlotRadius;
        }
    }
}
