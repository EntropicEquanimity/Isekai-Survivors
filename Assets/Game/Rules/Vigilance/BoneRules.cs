using System;

namespace Isekai
{
    /// <summary>Which remains a death leaves; they match the rows of the bones art.</summary>
    public enum BoneKind
    {
        Human = 0,
        Beast = 1,
        Demon = 2,
        Small = 3,
    }

    /// <summary>When a unit leaves bones, which kind, and how a pile decays: intact, cracked, crumbling, mostly crumbled, gone.</summary>
    public static class BoneRules
    {
        /// <summary>Seconds a pile lasts before it is gone.</summary>
        public const float CrumbleSeconds = 150f;

        /// <summary>How many looks a pile has before it is gone (the art's columns).</summary>
        public const int Stages = 4;

        /// <summary>The most piles the Region keeps; the oldest crumble away early past it.</summary>
        public const int MaxPiles = 300;

        /// <summary>A mortal or wildlife death leaves a pile.</summary>
        public static bool LeavesBones(UnitKind kind) => kind == UnitKind.Mortal || kind == UnitKind.Wildlife;

        public static BoneKind KindOf(UnitKind kind) => kind switch
        {
            UnitKind.Wildlife => BoneKind.Beast,
            UnitKind.Demon => BoneKind.Demon,
            _ => BoneKind.Human,
        };

        /// <summary>The look (0 intact .. 3 mostly crumbled) of a pile that is <paramref name="age"/> seconds old.</summary>
        public static int Stage(float age) => Math.Clamp((int)(Math.Max(0f, age) / (CrumbleSeconds / Stages)), 0, Stages - 1);

        public static bool Expired(float age) => age >= CrumbleSeconds;
    }
}
