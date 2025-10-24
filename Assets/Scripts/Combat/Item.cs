using Sirenix.OdinInspector;
using System;
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

    #region Luck And Rarity
    public static Rarity GetRarityFromLuck(float luckFactor = 0f) => GetRarity(GetWeightedRandomNumber(luckFactor));
    public static int GetWeightedRandomNumber(float luckFactor = 0f)
    {
        return Mathf.FloorToInt(100f * (Mathf.Pow(UnityEngine.Random.value, 1f - Mathf.Clamp(luckFactor * 0.9f, 0f, 100f) / 100f)));
    }
    static Rarity GetRarity(int value)
    {
        if(value < COMMON_THRESHOLD) { return Rarity.Common; }
        if(value < RARE_THRESHOLD) { return Rarity.Rare; }
        if(value < EPIC_THRESHOLD) { return Rarity.Epic; }
        if(value < LEGENDARY_THRESHOLD) { return Rarity.Legendary; }

        return Rarity.Mythic;
    }
    public static Color GetColorFromRarity(Rarity rarity)
    {
        return rarity switch
        {
            Rarity.Common => new Color(0.5f, 0.5f, 0.5f, 1f), //Grey
            Rarity.Rare => new Color(0.3f, 0.65f, 0.1f, 1f), // Emerald Green
            Rarity.Epic => new Color(0.25f, 0.41f, 0.88f, 1f), // Royal Blue
            Rarity.Legendary => new Color(0.94f, 0.75f, 0.01f, 1f), // Gold
            Rarity.Mythic => new Color(0.68f, 0f, 0f, 1f), // Red
            _ => Color.white,
        };
    }
    public static float CalculateRarityProbability(Rarity targetRarity, float luckFactor = 0f, int sampleSize = 10000)
    {
        int count = 0;
        for (int i = 0; i < sampleSize; i++)
        {
            var rarity = GetRarityFromLuck(luckFactor);
            if (rarity == targetRarity)
                count++;
        }
        return (float)count / sampleSize;
    }
    public static float[] CalculateAllRarityProbabilities(float luckFactor = 0f, int sampleSize = 50000)
    {
        var rarities = (Rarity[])Enum.GetValues(typeof(Rarity));
        int[] counts = new int[rarities.Length];

        for (int i = 0; i < sampleSize; i++)
        {
            var rarity = GetRarityFromLuck(luckFactor);
            counts[rarity.ToIndex()]++;
        }

        float[] probabilities = new float[rarities.Length];
        for (int i = 0; i < rarities.Length; i++)
        {
            probabilities[i] = (float)counts[i] / sampleSize;
        }

        return probabilities;
    }
    public static int COMMON_THRESHOLD = 60;
    public static int RARE_THRESHOLD = 85;
    public static int EPIC_THRESHOLD = 95;
    public static int LEGENDARY_THRESHOLD = 99;
    public static int MYTHIC_THRESHOLD = 100;
    #endregion
}
public enum ItemType
{
    All,
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