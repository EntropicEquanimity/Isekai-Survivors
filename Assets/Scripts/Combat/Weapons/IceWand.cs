using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IceWand : Equipment
{
    private Player _player;
    public override string Name => "Ice Wand";

    public override ItemType ItemType => ItemType.Weapon;

    protected override string EffectPrefabName => "Icicle";

    public override List<ItemUpgradeStats> ItemUpgrades => new List<ItemUpgradeStats>()
    {
        new(ItemStatType.Damage, Rarity.Common, 200, 2.5f),
        new(ItemStatType.Cooldown, Rarity.Common, 150, 0.07f),
        new(ItemStatType.Projectiles, Rarity.Rare, 50, 0.8f),
        new(ItemStatType.Speed, Rarity.Rare, 50, 0.08f),
        new(ItemStatType.Pierce, Rarity.Rare, 50, 0.8f),
        new(ItemStatType.Projectiles, Rarity.Legendary, 150, 0.4f),
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
            StartCoroutine(FireIcicles(i * 0.05f));
        }
        CurrentCooldown = CooldownRemaining;
    }
    private IEnumerator FireIcicles(float delay)
    {
        yield return new WaitForSeconds(delay);

        Vector2 direction = Random.insideUnitCircle.normalized;

        Projectile projectile = GetPrefab().GetComponent<Projectile>();
        projectile.transform.position = transform.position;
        projectile.transform.localScale = Vector3.one * Size;

        projectile.Initialize(new ProjectileStats(GetEquipmentStats(), direction), this);
    }
}
