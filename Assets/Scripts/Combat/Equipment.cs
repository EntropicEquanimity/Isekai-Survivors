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
    [ReadOnly] public ItemStats upgradeStats;
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
            projectiles = Projectiles,
            pierce = Pierce,
        };
    }

    public float Damage { get => Mathf.Round((itemStats.damage.Value + GameManager.Instance.Damage) * 100f / 100f) * GameManager.Instance.DamageMultiplier; } 
    public float Knockback { get => itemStats.knockBack.Value + GameManager.Instance.KnockBack; }
    public float Duration { get => Mathf.Max(-1f, itemStats.duration.Value + GameManager.Instance.Duration); }
    public float Size { get => Mathf.Max(-0.8f, ((itemStats.size.Value + GameManager.Instance.Size) * 100f) / 100f) + 1f; }
    public float Speed { get => Mathf.Max(0.1f, itemStats.speed.Value + GameManager.Instance.Speed); }
    public float CritChance { get => Mathf.Max(0f, ((itemStats.critChance.Value + GameManager.Instance.CritChance) * 100f) / 100f); }
    public float CritDamage { get => Mathf.Max(-0.8f, ((itemStats.critDamage.Value + GameManager.Instance.CritDamage) * 100f) / 100f) + 1.5f; } //Base crit damage of +50% damage
    public float Cooldown { get => Mathf.Max(0.1f, itemStats.cooldown.Value + GameManager.Instance.Cooldown + 1f); }
    public int Projectiles { get => Mathf.Max(1, Mathf.RoundToInt(itemStats.projectiles.Value + GameManager.Instance.Projectiles)) + 1; }
    public int Pierce { get => Mathf.Max(0, Mathf.RoundToInt(itemStats.pierce.Value + GameManager.Instance.Pierce)) + 1; }

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
    public string BuildLevelUpStatsString(ItemStats nextLevelStats)
    {
        if (IsMaxLevel) { Debug.LogWarning(this.name + " is already at max level!"); return "Max Level"; }
        StringBuilder sb = new StringBuilder();

        if (ItemLevel + 1 == MaxLevel) { sb.Append("(MAX LVL)").AppendLine(); }

        if (nextLevelStats.damage != 0f) { sb.Append("Damage ").Append(nextLevelStats.damage > 0f ? "+" : "-").Append(Mathf.Round(nextLevelStats.damage * 10f) / 10f).Append(" = ").Append((Damage + nextLevelStats.damage).ToString("F1")).AppendLine(); }
        if (nextLevelStats.knockBack != 0f) { sb.Append("Knockback ").Append(nextLevelStats.knockBack > 0f ? "+" : "-").Append(Mathf.Round(nextLevelStats.knockBack * 10f) / 10f).Append(" = ").Append((Knockback + nextLevelStats.knockBack).ToString("F1")).AppendLine(); }
        if (nextLevelStats.duration != 0f) { sb.Append("Duration ").Append(nextLevelStats.duration > 0f ? "+" : "-").Append(Mathf.Round(nextLevelStats.duration * 10f) / 10f).Append("s = ").Append((Duration + nextLevelStats.duration).ToString("F1")).Append("s").AppendLine(); }
        if (nextLevelStats.size != 0f) { sb.Append("Size ").Append(nextLevelStats.size > 0f ? "+" : "-").Append(Mathf.Round(nextLevelStats.size * 1000f) / 10f).Append("% = ").Append(((Size + nextLevelStats.size) * 100f).ToString("F1")).Append("%").AppendLine(); }       
        if (nextLevelStats.speed != 0f) { sb.Append("Speed ").Append(nextLevelStats.speed > 0f ? "+" : "-").Append(Mathf.Round(nextLevelStats.speed * 10f) / 10f).Append(" = ").Append((Speed + nextLevelStats.speed).ToString("F1")).AppendLine(); }
        if (nextLevelStats.critChance != 0f) { sb.Append("Crit Chance ").Append(nextLevelStats.critChance > 0f ? "+" : "-").Append(Mathf.Round(nextLevelStats.critChance * 1000f) / 10f).Append("% = ").Append(((CritChance + nextLevelStats.critChance) * 100f).ToString("F1")).Append("%").AppendLine(); }
        if (nextLevelStats.critDamage != 0f) { sb.Append("Crit Damage ").Append(nextLevelStats.critDamage > 0f ? "+" : "-").Append(Mathf.Round(nextLevelStats.critDamage * 1000f) / 10f).Append("% = ").Append(((CritDamage + nextLevelStats.critDamage) * 100f).ToString("F1")).Append("%").AppendLine(); }
        if (nextLevelStats.cooldown != 0f) { sb.Append("Cooldown ").Append(nextLevelStats.cooldown > 0f ? "+" : "-").Append(Mathf.Round(nextLevelStats.cooldown * 1000f) / 10f).Append("% = ").Append(((Cooldown + nextLevelStats.cooldown) * 100f).ToString("F1")).Append("%").AppendLine(); }
        if (nextLevelStats.projectiles != 0f) { sb.Append("Projectiles ").Append(nextLevelStats.projectiles > 0f ? "+" : "-").Append(Mathf.Round(nextLevelStats.projectiles * 10f) / 10f).Append(" = ").Append((Projectiles + nextLevelStats.projectiles).ToString("F1")).AppendLine(); }
        if (nextLevelStats.pierce != 0f) { sb.Append("Pierce ").Append(nextLevelStats.pierce > 0f ? "+" : "-").Append(Mathf.Round(nextLevelStats.pierce * 10f) / 10f).Append(" = ").Append((Pierce + nextLevelStats.pierce).ToString("F1")).AppendLine(); }
        //if (nextLevelStats.bounce != 0f) { sb.Append("Bounce ").Append(nextLevelStats.bounce > 0f ? "+" : "-").Append(nextLevelStats.bounce.ToString("F1")).Append(" = ").Append(Bounce + nextLevelStats.bounce.ToString("F1")).AppendLine(); }

        return sb.ToString();
    }
    public ItemStats GetUpgradeStats(Rarity luckRoll, int numberOfStats)
    {
        ItemStats stats = new ItemStats();
        List<ItemUpgradeStats> selectedUpgrades = new List<ItemUpgradeStats>();
        List<ItemUpgradeStats> upgrades = ItemUpgrades;
        int weight = 0;
        float mult = (int)luckRoll / 100f;

        for(int i = 0; i < upgrades.Count; i++) 
        {
            if ((int)upgrades[i].requiredRarity <= (int)luckRoll) 
            {
                selectedUpgrades.Add(upgrades[i]); 
                weight += upgrades[i].weight; 
            }
        }
        upgrades = new List<ItemUpgradeStats>();

        if (weight <= 0)
        {
            for (int i = 0; i < numberOfStats; i++)
            {
                int u = Random.Range(0, selectedUpgrades.Count);
                upgrades.Add(selectedUpgrades[u]);
            }
        }
        else
        {
            for (int i = 0; i < numberOfStats; i++)
            {
                int roll = Random.Range(0, weight);
                for (int j = 0; j < selectedUpgrades.Count; j++)
                {
                    roll -= selectedUpgrades[j].weight;
                    if (roll < 0)
                    {
                        upgrades.Add(selectedUpgrades[j]);
                        break;
                    }
                }
            }
        }
        #region Convert to ItemStats
        if (upgrades.Count > 0) 
        {
            foreach (var upgrade in upgrades)
            {
                float num = upgrade.baseUpgrade * mult;
                switch (upgrade.type) 
                {
                    case ItemStatType.Damage:
                        stats.damage += num;
                        break;
                    case ItemStatType.Knockback:
                        stats.knockBack += num;
                        break;
                    case ItemStatType.Duration:
                        stats.duration += num;
                        break;
                    case ItemStatType.Size:
                        stats.size += num;
                        break;
                    case ItemStatType.Speed:
                        stats.speed += num;
                        break;
                    case ItemStatType.CritChance:
                        stats.critChance += num;
                        break;
                    case ItemStatType.CritDamage:
                        stats.critDamage += num;
                        break;
                    case ItemStatType.Cooldown:
                        stats.cooldown += num;
                        break;
                    case ItemStatType.Projectiles:
                        stats.projectiles += num;
                        break;
                    case ItemStatType.Pierce:
                        stats.pierce += num;
                        break;
                }
            }
        }
        #endregion

        return stats;
    }
    public virtual int MaxLevel => 100; //Hard coded max item level
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
    public List<Vector2> GetSprayDirections(Vector2 baseDirection, int count, float maxSpreadDegrees)
    {
        baseDirection.Normalize();
        float baseAngle = Mathf.Atan2(baseDirection.y, baseDirection.x) * Mathf.Rad2Deg;
        List<Vector2> directions = new List<Vector2>();

        for (int i = 0; i < count; i++)
        {
            float angleOffset = Random.Range(-maxSpreadDegrees / 2f, maxSpreadDegrees / 2f);
            float finalAngle = baseAngle + angleOffset;
            float rad = finalAngle * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            directions.Add(dir);
        }
        return directions;
    }
    #endregion
}