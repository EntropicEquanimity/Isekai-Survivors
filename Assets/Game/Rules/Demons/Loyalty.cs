using System;

namespace Isekai
{
    /// <summary>Every number of demon levels and Loyalty, in one place to tune.</summary>
    public static class LoyaltyTuning
    {
        /// <summary>A new demon is level 1 with full Loyalty.</summary>
        public const int StartingLevel = 1;
        public const float MaxLoyalty = 100f;

        /// <summary>Experience one kill of a hostile unit is worth.</summary>
        public const int XpPerKill = 1;

        /// <summary>Experience to go from level n to n+1 is this times n (3, 6, 9, ...).</summary>
        public const int XpPerLevelStep = 3;

        /// <summary>Loyalty lost each second for every level the demon is above Jack's soul level (2 levels above: 3 a second).</summary>
        public const float DrainPerLevelAbove = 1.5f;

        /// <summary>
        /// Loyalty regained each second while the demon's level is at or below Jack's soul level. Slow on purpose: a demon that
        /// was drifting is not forgiven at once, but raising the soul level does bring it back from the edge.
        /// </summary>
        public const float RecoverPerSecond = 1f;

        /// <summary>When a demon turns rogue, demons of Jack's within this many cells with Loyalty below <see cref="FollowBelow"/> go with it.</summary>
        public const float FollowRadius = 8f;
        public const float FollowBelow = 50f;

        /// <summary>A rogue demon Jack brings down to this share of its health surrenders instead of dying.</summary>
        public const float SurrenderHealth = 0.25f;

        /// <summary>The Loyalty a demon has when Jack wins it back (it is above his soul level still, so it can drift again).</summary>
        public const float WonBackLoyalty = 60f;
    }

    /// <summary>The engine-free rules of demon levels and Loyalty.</summary>
    public static class LoyaltyRules
    {
        /// <summary>The total experience a demon needs to reach <paramref name="level"/> (0 for level 1).</summary>
        public static int XpToReach(int level)
        {
            int total = 0;
            for (int l = LoyaltyTuning.StartingLevel; l < level; l++) total += LoyaltyTuning.XpPerLevelStep * l;
            return total;
        }

        /// <summary>The level a demon with this much experience has.</summary>
        public static int LevelFor(int xp)
        {
            int level = LoyaltyTuning.StartingLevel;
            while (xp >= XpToReach(level + 1)) level++;
            return level;
        }

        /// <summary>Loyalty lost (negative) or gained (positive) in <paramref name="seconds"/> at this demon level against Jack's soul level.</summary>
        public static float LoyaltyChange(int demonLevel, int soulLevel, float seconds)
        {
            int above = demonLevel - soulLevel;
            return above > 0 ? -LoyaltyTuning.DrainPerLevelAbove * above * seconds : LoyaltyTuning.RecoverPerSecond * seconds;
        }

        /// <summary>Whether a demon of Jack's with this Loyalty, this far from a revolting demon, follows it.</summary>
        public static bool FollowsRevolt(float distance, float loyalty) =>
            distance <= LoyaltyTuning.FollowRadius && loyalty < LoyaltyTuning.FollowBelow;

        /// <summary>Whether a rogue demon at this health (0 to 1) has been beaten into surrender.</summary>
        public static bool Surrenders(float healthShare) => healthShare <= LoyaltyTuning.SurrenderHealth;
    }

    /// <summary>One demon's experience, level and Loyalty, and whether it is rogue.</summary>
    public sealed class LoyaltyState
    {
        public int Xp { get; private set; }
        public int Level { get; private set; } = LoyaltyTuning.StartingLevel;
        public float Loyalty { get; private set; } = LoyaltyTuning.MaxLoyalty;
        public bool IsRogue { get; private set; }

        /// <summary>Gives experience; true when the demon gained a level.</summary>
        public bool AddXp(int xp)
        {
            if (xp <= 0) return false;
            Xp += xp;
            int level = LoyaltyRules.LevelFor(Xp);
            bool up = level > Level;
            Level = level;
            return up;
        }

        /// <summary>Sets Loyalty directly (clamped), for tests and debug.</summary>
        public void SetLoyalty(float loyalty) => Loyalty = Math.Max(0f, Math.Min(LoyaltyTuning.MaxLoyalty, loyalty));

        /// <summary>Lets <paramref name="seconds"/> pass against Jack's soul level. True when the demon's Loyalty reached zero and it turned rogue just now. A rogue demon's Loyalty does not move.</summary>
        public bool Tick(float seconds, int soulLevel)
        {
            if (IsRogue) return false;
            SetLoyalty(Loyalty + LoyaltyRules.LoyaltyChange(Level, soulLevel, seconds));
            if (Loyalty > 0f) return false;
            IsRogue = true;
            return true;
        }

        /// <summary>Turns it rogue now (it followed another's revolt, or a debug row). False when it already was.</summary>
        public bool GoRogue()
        {
            if (IsRogue) return false;
            IsRogue = true;
            SetLoyalty(0f);
            return true;
        }

        /// <summary>Jack won it back: it is his again with <see cref="LoyaltyTuning.WonBackLoyalty"/>.</summary>
        public void WinBack()
        {
            IsRogue = false;
            SetLoyalty(LoyaltyTuning.WonBackLoyalty);
        }

        /// <summary>A new life: level 1, full Loyalty, loyal.</summary>
        public void Reset()
        {
            Xp = 0;
            Level = LoyaltyTuning.StartingLevel;
            Loyalty = LoyaltyTuning.MaxLoyalty;
            IsRogue = false;
        }
    }
}
