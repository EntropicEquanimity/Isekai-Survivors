using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sai : Equipment
{
    public override string Name => "Sai";

    public override ItemType ItemType => ItemType.Weapon;

    protected override string EffectPrefabName => "Sai";

    public override List<ItemUpgradeStats> ItemUpgrades => new List<ItemUpgradeStats>()
    {
        new(ItemStatType.Damage, Rarity.Common, 200, 1.8f),
        new(ItemStatType.Cooldown, Rarity.Common, 150, 0.07f),
        new(ItemStatType.Size, Rarity.Common, 50, 0.1f),
        new(ItemStatType.Speed, Rarity.Rare, 50, 0.08f),
        new(ItemStatType.Pierce, Rarity.Rare, 50, 1f),
        new(ItemStatType.Projectiles, Rarity.Epic, 50, 0.6f),
    };

    public override float BaseCooldown => 3f;

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
        for (int i = 0; i < Projectiles; i++)
        {
            StartCoroutine(ThrowSai(i * 0.2f));
        }
        CurrentCooldown = CooldownRemaining;
    }
    private IEnumerator ThrowSai(float delay) //Throws 2 at a time. More projectiles only increases the number of waves thrown at a time. 
    {
        yield return new WaitForSeconds(delay);

        Collider2D nearest = GetClosestInRadius(5f);
        Vector2 direction = ((nearest != null ? nearest.transform.position : transform.position) + (Vector3)Random.insideUnitCircle * 0.25f - transform.position).normalized;
        ItemStats stats = GetEquipmentStats();
        List<Vector2> spray = GetSprayDirections(direction, 2, 30);
        foreach (Vector2 dir in spray) 
        {
            Projectile projectile1 = GetPrefab().GetComponent<Projectile>();
            projectile1.transform.position = transform.position;
            projectile1.transform.localScale = Vector3.one * Size;

            projectile1.Initialize(new ProjectileStats(stats, dir), this);
        }
    }

}
