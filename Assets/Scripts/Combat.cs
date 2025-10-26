using UnityEngine;

public enum ItemType
{
    All,
    Weapon,
    Artifact,
    Item,
}
#region Stat Containers
[System.Serializable]
public class ItemStats
{
    public float damage;
    public float knockBack;
    public float duration;
    public float size;
    public float speed;
    public float critChance;
    public float critDamage;
    public float cooldown;
    public float projectiles;
    public float pierce;

    public ItemStats(ItemStats stats)
    {
        damage = stats.damage;
        knockBack = stats.knockBack;
        duration = stats.duration;
        size = stats.size;
        speed = stats.speed;
        critChance = stats.critChance;
        critDamage = stats.critDamage;
        cooldown = stats.cooldown;
        projectiles = stats.projectiles;
        pierce = stats.pierce;
    }
    public ItemStats() { }
}
public struct ItemUpgradeStats
{
    public int weight;
    public Rarity requiredRarity;
    public ItemStatType type;
    public float baseUpgrade;

    public ItemUpgradeStats(ItemStatType type, Rarity requiredRarity, int weight, float baseUpgrade)
    {
        this.type = type;
        this.requiredRarity = requiredRarity;
        this.weight = weight;
        this.baseUpgrade = baseUpgrade;
    }
}
[System.Serializable]
public struct SessionItemStats
{
    public EntityStat damage;
    public EntityStat knockBack;
    public EntityStat duration;
    public EntityStat size;
    public EntityStat speed;
    public EntityStat critChance;
    public EntityStat critDamage;
    public EntityStat cooldown;
    public EntityStat projectiles;
    public EntityStat pierce;

    public SessionItemStats(ItemStats itemStats)
    {
        damage = new EntityStat() { BaseValue = itemStats.damage };
        knockBack = new EntityStat() { BaseValue = itemStats.knockBack };
        duration = new EntityStat() { BaseValue = itemStats.duration };
        size = new EntityStat() { BaseValue = itemStats.size };
        speed = new EntityStat() { BaseValue = itemStats.speed };
        critChance = new EntityStat() { BaseValue = itemStats.critChance };
        critDamage = new EntityStat() { BaseValue = itemStats.critDamage };
        cooldown = new EntityStat() { BaseValue = itemStats.cooldown };
        projectiles = new EntityStat() { BaseValue = itemStats.projectiles };
        pierce = new EntityStat() { BaseValue = itemStats.pierce };
    }
}
[System.Serializable]
public class DamageRecord
{
    public int damageDealt;
    public int kills;

    public void AddStats(DamageReport report)
    {
        damageDealt += report.damageDealt;
        if (report.isDead) { kills++; }
    }
}
#endregion
public enum ItemStatType
{
    Damage = 0,
    Knockback = 1,
    Duration = 2,
    Size = 3,
    Speed = 4,
    CritChance = 5,
    CritDamage = 6,
    Cooldown = 7,
    Projectiles = 8,
    Pierce = 9,
}
public enum Rarity
{
    Common = 100,
    Rare = 125,
    Epic = 175,
    Legendary = 250,
    Mythic = 400
}
public static class RarityExtension
{
    public static int ToIndex(this Rarity rarity)
    {
        return rarity switch
        {
            Rarity.Common => 0,
            Rarity.Rare => 1,
            Rarity.Epic => 2,
            Rarity.Legendary => 3,
            Rarity.Mythic => 4,
            _ => 0
        };
    }
}