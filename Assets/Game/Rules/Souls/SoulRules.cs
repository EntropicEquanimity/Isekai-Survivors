using System;

namespace Isekai
{
    /// <summary>The soul economy's formulas. Souls are whole numbers; the drain is a rate, paid out by <see cref="SoulDrain"/>.</summary>
    public static class SoulRules
    {
        /// <summary>Souls lost per second while bodiless; doubled near priests or on holy ground.</summary>
        public static float DrainPerSecond(int soulLevel, int deathsThisRun, bool doubled)
        {
            float rate = SoulTuning.DrainBase + SoulTuning.DrainPerLevel * soulLevel + deathsThisRun;
            return doubled ? rate * SoulTuning.HolyDrainMultiplier : rate;
        }

        /// <summary>Souls to revive at the Anchor in a new body.</summary>
        public static int ReviveCost(int soulLevel) => Whole(SoulTuning.ReviveBase + SoulTuning.RevivePerLevel * soulLevel);

        /// <summary>Souls to possess one of his demons: 25% less while Jack's body still lives.</summary>
        public static int PossessCost(int soulLevel, bool bodyAlive)
        {
            float cost = SoulTuning.PossessBase + SoulTuning.PossessPerLevel * soulLevel;
            return Whole(bodyAlive ? cost * (1f - SoulTuning.PossessWhileAliveDiscount) : cost);
        }

        /// <summary>Whether the Souls cover a possession.</summary>
        public static bool CanPossess(int souls, int soulLevel, bool bodyAlive) => souls >= PossessCost(soulLevel, bodyAlive);

        /// <summary>Souls to summon a Demon King at the Anchor.</summary>
        public static int SummonKingCost(int soulLevel) => Whole(SoulTuning.SummonKingBase + SoulTuning.SummonKingPerLevel * soulLevel);

        /// <summary>What Jack's damage and health are multiplied by in a body: a Demon King is whole, an ordinary demon is weaker.</summary>
        public static float BodyMultiplier(bool demonKing) => demonKing ? 1f : 1f - SoulTuning.OrdinaryDemonPenalty;

        /// <summary>Only a living Demon King body spreads corruption as it walks.</summary>
        public static bool BodySpreads(bool demonKing, bool alive) => demonKing && alive;

        /// <summary>Souls to raise soul level from <paramref name="soulLevel"/> to the next.</summary>
        public static int LevelUpCost(int soulLevel) => SoulTuning.LevelUpCostPerLevel * soulLevel;

        /// <summary>The Run is lost when Souls are gone while Jack is bodiless (also the instant his body dies with none).</summary>
        public static bool IsLost(int souls, bool bodiless) => bodiless && souls <= 0;

        /// <summary>Whether a revive can happen: an Anchor stands and the Souls cover the cost.</summary>
        public static bool CanRevive(int souls, int soulLevel, bool anchorStands) => anchorStands && souls >= ReviveCost(soulLevel);

        /// <summary>Souls a kill gives (only mortals and wildlife do).</summary>
        public static int SoulsForKill(UnitKind kind) => kind switch
        {
            UnitKind.Mortal => SoulTuning.SoulsPerMortal,
            UnitKind.Wildlife => SoulTuning.SoulsPerWildlife,
            _ => 0,
        };

        private static int Whole(float value) => (int)MathF.Round(value, MidpointRounding.AwayFromZero);
    }
}
