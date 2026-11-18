using Blondie.Stats;

namespace Isekai
{
    /// <summary>
    /// Every stat id in the game, for Characters, Units, enemies and Buildings alike. Stored by name in Stat Blocks and
    /// saves, so reordering is safe and renaming breaks them.
    /// Names that Blondie packages read as strings must match theirs: Combat's damage formula reads Armor, DamageBonus,
    /// CritChance and CritMultiplier; Controllers' motors read Speed; a vital's max is its id + "Max".
    /// </summary>
    [StatIdEnum]
    public enum StatId
    {
        // Survival
        Health,
        HealthMax,
        Armor,
        Speed,

        // Offence (read by Combat's damage formula and the ability deliveries)
        DamageBonus,
        CritChance,
        CritMultiplier,
        AttackSpeed,
        Range,
        Area,
        Amount,
        Duration,
        ProjectileSpeed,

        // Economy
        MiningSpeed,
        CarryCapacity,
        BuildSpeed,
        PickupRadius,

        // Run
        Luck,
    }
}
