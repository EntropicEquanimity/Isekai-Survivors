namespace Isekai
{
    /// <summary>Every number of the soul economy, in one place to tune (the spec's "about" figures).</summary>
    public static class SoulTuning
    {
        public const int StartingLevel = 1;

        // Drain per second while bodiless: base + perLevel x soul level + deaths this Run.
        public const float DrainBase = 1f;
        public const float DrainPerLevel = 0.2f;
        public const float HolyDrainMultiplier = 2f;

        // Revive at the Anchor, possess one of his demons.
        public const float ReviveBase = 20f;
        public const float RevivePerLevel = 2f;
        public const float PossessBase = 100f;
        public const float PossessPerLevel = 10f;
        public const float PossessWhileAliveDiscount = 0.25f;

        // Jack in an ordinary demon (not a Demon King) deals and survives this much less (0.3 = 30%).
        public const float OrdinaryDemonPenalty = 0.3f;

        // Summoning a Demon King at the Anchor: about what a revive costs, so getting back into a strong body is never out of reach
        // (playtest 2026-11-21: 220 at soul level 1 was far too much).
        public const float SummonKingBase = 20f;
        public const float SummonKingPerLevel = 2f;

        // Raising soul level costs this x the current level.
        public const int LevelUpCostPerLevel = 50;

        // Souls Jack's side starts a Region with when it is entered from the Planet map. Enough for a few Portals (5 each) and
        // one revive (22 at soul level 1) so that the first mistakes are not the end of the Run; found by autoplay batches (docs/balance.md).
        public const int RegionStartingSouls = 40;

        // Souls a kill gives.
        public const int SoulsPerMortal = 2;
        public const int SoulsPerWildlife = 1;

        // What each soul level above the first gives Jack's body (fractions: 0.05 = +5%).
        public const float HealthPerLevel = 0.05f;
        public const float DamagePerLevel = 0.03f;
        public const float SpeedPerLevel = 0.01f;
    }
}
