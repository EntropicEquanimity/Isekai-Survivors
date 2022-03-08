using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

public abstract class Equipment : Item
{
    [HideInInspector] public List<GameObject> EffectPrefabs = new List<GameObject>();
    [Expandable] public EquipmentSO itemData;
    [ReadOnly] public ItemSlot itemSlot;
    [ReadOnly] public SessionItemStats itemStats;
    public float CurrentCooldown { get; protected set; }
    public DamageRecord DamageRecord { get; protected set; }
    public void Awake()
    {
        DamageRecord = new DamageRecord();
        ItemLevel = 1;
        RecalculateItemStats();
    }
    [Button]
    public void RecalculateItemStats()
    {
        itemStats = new SessionItemStats(itemData.itemStats);

        itemStats.damage.BaseValue += (ItemLevel - 1) * itemData.upgradeStats.damage;
        itemStats.knockBack.BaseValue += (ItemLevel - 1) * itemData.upgradeStats.knockBack;
        itemStats.duration.BaseValue += (ItemLevel - 1) * itemData.upgradeStats.duration;
        itemStats.size.BaseValue += (ItemLevel - 1) * itemData.upgradeStats.size;
        itemStats.speed.BaseValue += (ItemLevel - 1) * itemData.upgradeStats.speed;
        itemStats.critChance.BaseValue += (ItemLevel - 1) * itemData.upgradeStats.critChance;
        itemStats.cooldown.BaseValue += (ItemLevel - 1) * itemData.upgradeStats.cooldown;
        itemStats.projectiles.BaseValue += (ItemLevel - 1) * itemData.upgradeStats.projectiles;
        itemStats.pierceCount.BaseValue += (ItemLevel - 1) * itemData.upgradeStats.pierceCount;
    }
    public virtual void Upgrade()
    {
        ItemLevel++;
        RecalculateItemStats();
        UseItem();
    }
    public virtual void UnEquip()
    {
        itemSlot.ResetToEmpty();
        StopItem();
        Destroy(gameObject);
    }
    public abstract void UseItem();
    public abstract void StopItem();
    public virtual void TickCooldown(float time)
    {
        CurrentCooldown -= time;
        if (CurrentCooldown <= 0f)
        {
            UseItem();
        }
    }
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
            cooldown = Cooldown,
            projectiles = ProjectileCount,
            pierceCount = PierceCount
        };
    }
    public int Damage { get => Mathf.RoundToInt(itemStats.damage.Value + GameManager.Instance.Damage); }
    public float Knockback { get => Mathf.Max(0f, itemStats.knockBack.Value + GameManager.Instance.KnockBack); }
    public float Duration { get => Mathf.Max(0f, itemStats.duration.Value + GameManager.Instance.Duration); }
    public float Size { get => Mathf.Max(0.1f, itemStats.size.Value + GameManager.Instance.Size); }
    public float Speed { get => Mathf.Max(0.1f, itemStats.speed.Value + GameManager.Instance.Speed); }
    public float CritChance { get => Mathf.Max(0f, itemStats.critChance.Value + GameManager.Instance.CritChance); }
    public float Cooldown { get => Mathf.Max(0.5f, itemStats.cooldown.Value + GameManager.Instance.CritDamage); }
    public int ProjectileCount { get => Mathf.Max(1, Mathf.RoundToInt(itemStats.projectiles.Value + GameManager.Instance.Projectiles)); }
    public int PierceCount { get => Mathf.Max(1, Mathf.RoundToInt(itemStats.pierceCount.Value + GameManager.Instance.PierceCount)); }
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
[System.Serializable]
public struct ItemStats
{
    public float damage;
    public float knockBack;
    public float duration;
    public float size;
    public float speed;
    public float critChance;
    public float cooldown;
    public float projectiles;
    public float pierceCount;
}
[System.Serializable]
public struct SessionItemStats
{
    public CharacterStat damage;
    public CharacterStat knockBack;
    public CharacterStat duration;
    public CharacterStat size;
    public CharacterStat speed;
    public CharacterStat critChance;
    public CharacterStat cooldown;
    public CharacterStat projectiles;
    public CharacterStat pierceCount;

    public SessionItemStats(ItemStats itemStats)
    {
        damage = new CharacterStat() { BaseValue = itemStats.damage };
        knockBack = new CharacterStat() { BaseValue = itemStats.knockBack };
        duration = new CharacterStat() { BaseValue = itemStats.duration };
        size = new CharacterStat() { BaseValue = itemStats.size };
        speed = new CharacterStat() { BaseValue = itemStats.speed };
        critChance = new CharacterStat() { BaseValue = itemStats.critChance };
        cooldown = new CharacterStat() { BaseValue = itemStats.cooldown };
        projectiles = new CharacterStat() { BaseValue = itemStats.projectiles };
        pierceCount = new CharacterStat() { BaseValue = itemStats.pierceCount };
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