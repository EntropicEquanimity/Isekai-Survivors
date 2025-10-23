using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoomerangAxe : Equipment
{
    private Player _player;
    private const int SpriteBaseHeight = 2;
    public override string Name => "Boomerang Axe";

    public override ItemType ItemType => ItemType.Weapon;

    protected override string EffectPrefabName => "BoomerangAxe";

    public override List<ItemUpgradeStats> ItemUpgrades => new List<ItemUpgradeStats>()
    {
        new(ItemStatType.Cooldown, Rarity.Common, 100, 0.08f),
        new(ItemStatType.Damage, Rarity.Common, 100, 2.5f),
        new(ItemStatType.Size, Rarity.Common, 100, 0.1f),
        new(ItemStatType.Duration, Rarity.Common, 50, 0.08f),
        new(ItemStatType.Speed, Rarity.Epic, 25, 0.1f),
        new(ItemStatType.Projectiles, Rarity.Legendary, 100, 0.4f),
    };

    public override float BaseCooldown => 5f;

    public override void OnEquip()
    {
        _player = GameManager.Instance.player;
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
            StartCoroutine(SpawnSwingEffects(i * 0.2f));
        }
        CurrentCooldown = CooldownRemaining;
    }
    private IEnumerator SpawnSwingEffects(float delay)
    {
        yield return new WaitForSeconds(delay);

        Collider2D nearest = GetRandomInRadius(Size + 1f);
        Vector2 direction = ((nearest != null ? nearest.transform.position : transform.position) + (Vector3)Random.insideUnitCircle * 0.25f - transform.position).normalized;

        Projectile projectile = GetPrefab().GetComponent<Projectile>();
        projectile.transform.position = transform.position + (Vector3)direction;
        projectile.transform.localScale = Vector3.one * Size;
        //projectile.GetComponent<SpriteRenderer>().size = new Vector2(SpriteBaseHeight, SpriteBaseHeight * (itemData.itemStats.size + 1f));
        projectile.Initialize(new ProjectileStats(GetEquipmentStats(), direction), this);
    }
}
