using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Text;
using JetBrains.Annotations;
using Unity.Burst.Intrinsics;

public abstract class Equipment : Item
{
    [HideInInspector] public List<GameObject> EffectPrefabs = new List<GameObject>();
    public EquipmentSO itemData;
    [ReadOnly] public ItemSlot itemSlot;
    [ReadOnly] public SessionItemStats itemStats;
    public float CurrentCooldown { get; protected set; }
    public DamageRecord DamageRecord { get; protected set; }
    public void Awake()
    {
        DamageRecord = new DamageRecord();
        ItemLevel = 0;
        RecalculateItemStats();
    }

    #region Leveling / Stats
    public virtual void Upgrade(ItemStats upgradeStats)
    {
        if (IsMaxLevel) { return; }
        ItemLevel++;

        AddStats(upgradeStats);

        UseItem();
        
        if (IsMaxLevel) { LootController.Instance?.RemoveItemFromPool(itemData); }
    }
    void AddStats(ItemStats upgradeStats)
    {
        if (upgradeStats.damage != 0f) { itemStats.damage.BaseValue += upgradeStats.damage; }
        if (upgradeStats.knockBack != 0f) { itemStats.knockBack.BaseValue += upgradeStats.knockBack; }
        if (upgradeStats.duration != 0f) { itemStats.duration.BaseValue += upgradeStats.duration; }
        if (upgradeStats.size != 0f) { itemStats.size.BaseValue += upgradeStats.size; }
        if (upgradeStats.speed != 0f) { itemStats.speed.BaseValue += upgradeStats.speed; }
        if (upgradeStats.critChance != 0f) { itemStats.critChance.BaseValue += upgradeStats.critChance; }
        if (upgradeStats.critDamage!= 0f) { itemStats.critDamage.BaseValue += upgradeStats.critDamage; }
        if (upgradeStats.cooldown != 0f) { itemStats.cooldown.BaseValue += upgradeStats.cooldown; }
        if (upgradeStats.projectiles != 0f) { itemStats.projectiles.BaseValue += upgradeStats.projectiles; }
        if (upgradeStats.pierce != 0f) { itemStats.pierce.BaseValue += upgradeStats.pierce; }
    }
    [Button]
    public void RecalculateItemStats() //Will remove all upgrades, only call once at start of scene or combat
    {
        itemStats = new SessionItemStats(itemData.itemStats);

        //itemStats.damage.BaseValue += (ItemLevel - 1) * itemData.upgradeStats.damage;
        //itemStats.knockBack.BaseValue += (ItemLevel - 1) * itemData.upgradeStats.knockBack;
        //itemStats.duration.BaseValue += (ItemLevel - 1) * itemData.upgradeStats.duration;
        //itemStats.size.BaseValue += (ItemLevel - 1) * itemData.upgradeStats.size;
        //itemStats.speed.BaseValue += (ItemLevel - 1) * itemData.upgradeStats.speed;
        //itemStats.critChance.BaseValue += (ItemLevel - 1) * itemData.upgradeStats.critChance;
        //itemStats.cooldown.BaseValue += (ItemLevel - 1) * itemData.upgradeStats.cooldown;
        //itemStats.projectiles.BaseValue += (ItemLevel - 1) * itemData.upgradeStats.projectiles;
        //itemStats.pierceCount.BaseValue += (ItemLevel - 1) * itemData.upgradeStats.pierceCount;

        //for (int i = 0; i < UpgradeValues.Count; i++)
        //{
        //    if (ItemLevel - 1 >= i)
        //    {
        //        itemStats.damage.BaseValue += UpgradeValues[i].damage;
        //        itemStats.knockBack.BaseValue += UpgradeValues[i].knockBack;
        //        itemStats.duration.BaseValue += UpgradeValues[i].duration;
        //        itemStats.size.BaseValue += UpgradeValues[i].size;
        //        itemStats.speed.BaseValue += UpgradeValues[i].speed;
        //        itemStats.critChance.BaseValue += UpgradeValues[i].critChance;
        //        itemStats.critDamage.BaseValue += UpgradeValues[i].critDamage;
        //        itemStats.cooldown.BaseValue += UpgradeValues[i].cooldown;
        //        itemStats.projectiles.BaseValue += UpgradeValues[i].projectiles;
        //        itemStats.pierce.BaseValue += UpgradeValues[i].pierce;
        //    }
        //}

        Debug.Log(BuildLevelUpStatsString());
    }
    public abstract List<ItemUpgradeStats> ItemUpgrades { get; }
    public abstract float BaseCooldown { get; }
    #endregion

    #region Public
    public abstract void UseItem();
    public abstract void StopItem();
    public virtual void TickCooldown(float time)
    {
        CurrentCooldown -= time;
        if (CurrentCooldown <= 0f) { UseItem(); }
    }
    public virtual void UnEquip()
    {
        itemSlot.ResetToEmpty();
        StopItem();
        Destroy(gameObject);
    }
    #endregion

    #region Getters
    public ItemStats GetEquipmentStats()
    {
        return new ItemStats()
        {
            damage = Damage,
            knockBack = Knockback,
            duration = Duration,
            size = Size,
            speed = Speed,
            critChance = CritChance,
            critDamage = CritDamage,
            cooldown = Cooldown,
            projectiles = ProjectileCount,
            pierce = PierceCount,
            bounce = Bounce,
        };
    }

    public float Damage { get => Mathf.Round((itemStats.damage.Value + GameManager.Instance.Damage * 100f) / 100f) * GameManager.Instance.DamageMultiplier; } 
    public float Knockback { get => itemStats.knockBack.Value + GameManager.Instance.KnockBack; }
    public float Duration { get => Mathf.Max(0.1f, itemStats.duration.Value + GameManager.Instance.Duration + 1f); }
    public float Size { get => Mathf.Max(0.1f, (itemStats.size.Value + GameManager.Instance.Size * 100f) / 100f); }
    public float Speed { get => Mathf.Max(0.1f, itemStats.speed.Value + GameManager.Instance.Speed); }
    public float CritChance { get => Mathf.Max(0f, (itemStats.critChance.Value + GameManager.Instance.CritChance * 100f) / 100f); }
    public float CritDamage { get => Mathf.Max(-0.8f, (itemStats.critDamage.Value + GameManager.Instance.CritDamage * 100f) / 100f) + 1f; }
    public float Cooldown { get => Mathf.Max(0.1f, itemStats.cooldown.Value + GameManager.Instance.Cooldown + 1f); }
    public int ProjectileCount { get => Mathf.Max(1, Mathf.RoundToInt(itemStats.projectiles.Value + GameManager.Instance.Projectiles)); }
    public int PierceCount { get => Mathf.Max(1, Mathf.RoundToInt(itemStats.pierce.Value + GameManager.Instance.Pierce)); }
    public int Bounce { get => Mathf.Max(0, Mathf.RoundToInt(itemStats.bounce.Value + GameManager.Instance.Bounce)); }

    public GameObject GetPrefab()
    {
        for (int i = 0; i < EffectPrefabs.Count; i++)
        {
            if (!EffectPrefabs[i].activeInHierarchy)
            {
                EffectPrefabs[i].SetActive(true);
                EffectPrefabs[i].transform.localScale = Vector3.one;
                return EffectPrefabs[i];
            }
        }
        GameObject obj = Instantiate(EffectPrefab);
        EffectPrefabs.Add(obj);
        return obj;
    }
    public string BuildLevelUpStatsString()
    {
        if (IsMaxLevel) { Debug.LogWarning(this.name + " is already at max level!"); return "Max Level"; }
        StringBuilder sb = new StringBuilder();

        ItemStats nextLevelStats = GetUpgradeStats(0, ItemUpgrades);
        if (ItemLevel + 1 == MaxLevel) { sb.Append("(MAX LVL)").AppendLine(); }

        if (nextLevelStats.damage != 0f) { sb.Append("Damage ").Append(Damage.ToString("F1")).Append(" + ").Append(nextLevelStats.damage).Append(" -> ").Append((Damage + nextLevelStats.damage).ToString("F1")).AppendLine(); }
        if (nextLevelStats.knockBack != 0f) { sb.Append("Knockback ").Append(Knockback.ToString("F1")).Append(" + ").Append(nextLevelStats.knockBack).Append(" -> ").Append((Knockback + nextLevelStats.knockBack).ToString("F1")).AppendLine(); }
        if (nextLevelStats.duration != 0f) { sb.Append("Duration ").Append(Duration.ToString("F1")).Append("% + ").Append(nextLevelStats.duration).Append("% -> ").Append((Duration + nextLevelStats.duration).ToString("F1")).Append("%").AppendLine(); }
        if (nextLevelStats.size != 0f) { sb.Append("Duration ").Append(Duration.ToString("F1")).Append("% + ").Append(nextLevelStats.duration).Append("% -> ").Append((Duration + nextLevelStats.duration).ToString("F1")).Append("%").AppendLine(); }
        if (nextLevelStats.speed != 0f) { sb.Append("Speed ").Append(Speed.ToString("F1")).Append(" + ").Append(nextLevelStats.speed).Append(" -> ").Append((Speed + nextLevelStats.speed).ToString("F1")).AppendLine(); }
        if (nextLevelStats.critChance != 0f) { sb.Append("Crit Chance ").Append(CritChance.ToString("F1")).Append("% + ").Append(nextLevelStats.critChance).Append("% -> ").Append((CritChance + nextLevelStats.critChance).ToString("F1")).Append("%").AppendLine(); }
        if (nextLevelStats.critDamage != 0f) { sb.Append("Crit Damage ").Append(CritDamage.ToString("F1")).Append("% + ").Append(nextLevelStats.critDamage).Append("% -> ").Append((CritDamage + nextLevelStats.critDamage).ToString("F1")).Append("%").AppendLine(); }
        if (nextLevelStats.cooldown != 0f) { sb.Append("Cooldown ").Append(Cooldown.ToString("F1")).Append("% + ").Append(nextLevelStats.cooldown).Append("% -> ").Append((Cooldown + nextLevelStats.cooldown).ToString("F1")).Append("%").AppendLine(); }
        if (nextLevelStats.projectiles != 0f) { sb.Append("Projectiles ").Append(ProjectileCount.ToString("F1")).Append(" + ").Append(nextLevelStats.projectiles).Append(" -> ").Append((ProjectileCount + nextLevelStats.projectiles).ToString("F1")).AppendLine(); }
        if (nextLevelStats.pierce != 0f) { sb.Append("Pierce +").Append(nextLevelStats.pierce).AppendLine(); }
        if (nextLevelStats.bounce != 0f) { sb.Append("Bounce +").Append(nextLevelStats.bounce).AppendLine(); }

        return sb.ToString();
    }
    public ItemStats GetUpgradeStats(Rarity luckRoll, List<ItemUpgradeStats> upgrades, int numberOfStats = 2)
    {
        ItemStats stats = new ItemStats();
        List<ItemUpgradeStats> selectedUpgrades = new List<ItemUpgradeStats>();
        int weight = 0;
        float mult = (int)luckRoll / 100f;
        foreach(var upgrade in upgrades) 
        {
            if ((int)upgrade.requiredRarity > (int)luckRoll) { upgrades.Remove(upgrade); } //If luck roll did not reach required amount, remove 
            else weight += upgrade.weight; 
        }

        if (weight <= 0) { //Calculate weights
            for (int i = 0; i < numberOfStats; i++)
            {
                selectedUpgrades.Add(upgrades[Random.Range(0, upgrades.Count)]);
            }
        }
        else 
        {
            for (int i = 0; i < numberOfStats; i++)
            {
                int roll = Random.Range(0, weight);
                for (int j = 0; j < upgrades.Count; j++) 
                {
                    roll -= upgrades[j].weight;
                    if (roll < 0) 
                    {
                        selectedUpgrades.Add(upgrades[j]);
                    }
                }
            }
        }

        #region Convert to ItemStats
        if (selectedUpgrades.Count > 0) 
        {
            foreach (var upgrade in selectedUpgrades)
            {
                switch (upgrade.type) 
                {
                    case ItemStatType.Damage:
                        stats.damage = upgrade.baseUpgrade * mult;
                        break;
                }
            }
        }
        #endregion

        return stats;
    }
    public int MaxLevel => 100; //Hard coded max item level
    public bool IsMaxLevel => ItemLevel >= MaxLevel;
    public float CooldownRemaining => BaseCooldown / Cooldown;
    #endregion

    #region Physics Casting
    public Collider2D[] CircleCastAll(float range)
    {
        return Physics2D.OverlapCircleAll(transform.position, range, LayerMask.GetMask("Enemy"));
    }
    public Collider2D GetRandomInRadius(float radius)
    {
        return Physics2D.OverlapCircle(transform.position, radius, LayerMask.GetMask("Enemy"));
    }
    public Collider2D GetClosestInRadius(float radius)
    {
        Collider2D selected = null;
        float distance = Mathf.Infinity;
        foreach (var collider in Physics2D.OverlapCircleAll(transform.position, radius, LayerMask.GetMask("Enemy")))
        {
            float dist = Vector2.Distance(collider.transform.position, transform.position);
            if (dist < distance)
            {
                selected = collider;
                distance = dist;
            }
        }
        return selected;
    }
    #endregion
}