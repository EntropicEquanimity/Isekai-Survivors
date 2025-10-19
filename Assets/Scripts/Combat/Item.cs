using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Item : MonoBehaviour
{
    public const string EffectPrefabPath = "Items/Effects/";
    public abstract string Name { get; }
    public abstract ItemType ItemType { get; }
    public GameObject EffectPrefab
    {
        get
        {
            if (_effectPrefab != null) { return _effectPrefab; }
            else { _effectPrefab = LoadEffectPrefab; return _effectPrefab; }
        }
    }
    public int ItemLevel { get; protected set; }
    protected GameObject _effectPrefab;
    protected abstract string EffectPrefabName { get; }
    public GameObject LoadEffectPrefab => Resources.Load(EffectPrefabPath + EffectPrefabName) as GameObject;
    public abstract void OnEquip();
}
public enum ItemType
{
    Weapon,
    Tool,
    Artifact,
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
    public float bounce;
}
public struct ItemUpgradeStats
{
    public int weight;
    public Rarity requiredRarity;
    public ItemStatType type;
    public float baseUpgrade;

    public ItemUpgradeStats(ItemStatType type, Rarity requireRarity, int weight, float baseUpgrade)
    {
        this.type = type;
        this.requiredRarity = requireRarity;
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
    public EntityStat bounce;

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
        bounce = new EntityStat() { BaseValue = itemStats.bounce };
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
    Bounce = 10,
}
public enum Rarity
{
    None = 0,
    Common = 100,
    Rare = 125,
    Epic = 175,
    Legendary = 250,
    Mythic = 400
}