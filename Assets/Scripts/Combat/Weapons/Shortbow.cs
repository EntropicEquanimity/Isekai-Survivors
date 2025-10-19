using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shortbow : Equipment
{
    public override string Name => "Shortbow";

    public override ItemType ItemType => ItemType.Weapon;

    protected override string EffectPrefabName => "Shortbow_Arrow";

    public override List<ItemUpgradeStats> ItemUpgrades => new List<ItemUpgradeStats>()
    {
        new(ItemStatType.Cooldown, Rarity.Common, 75, 0.09f),
        new(ItemStatType.Damage, Rarity.Common, 100, 2f),
        new(ItemStatType.CritDamage, Rarity.Common, 50, 0.15f),
        new(ItemStatType.CritChance, Rarity.Common, 50, 0.07f),
        new(ItemStatType.Projectiles, Rarity.Common, 10, 1f),
        new(ItemStatType.Size, Rarity.Rare, 50, 0.12f),
    };

    public override float BaseCooldown => 2.5f;

    public override void OnEquip()
    {
        UseItem();
    }
    public override void StopItem()
    {
        throw new System.NotImplementedException();
    }

    public override void UseItem()
    {
        Collider2D nearest = GetClosestInRadius(15f);
        Vector2 direction = ((nearest != null ? nearest.transform.position : transform.position) - transform.position).normalized;
        //if (GetRandomInRadius(Range + 2f) == null) { return; }
        for (int i = 0; i < ProjectileCount; i++)
        {
            StartCoroutine(SpawnArrows(i * 0.05f, direction + Random.insideUnitCircle * 0.25f));
        }
        CurrentCooldown = CooldownRemaining;
    }
    private IEnumerator SpawnArrows(float delay, Vector2 direction)
    {
        yield return new WaitForSeconds(delay);

        Projectile projectile = GetPrefab().GetComponent<Projectile>();
        projectile.transform.position = transform.position + (Vector3)direction * 0.5f;
        projectile.transform.localScale = Vector3.one * Size;

        projectile.Initialize(new ProjectileStats(GetEquipmentStats(), direction.normalized, PierceCount), this);
    }
}
