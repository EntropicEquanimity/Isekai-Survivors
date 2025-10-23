using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rapier : Equipment
{
    public bool pullsPlayerForward;
    private Player _player;
    private const float SpriteBaseHeight = 1.5f;
    public override string Name => "Rapier";

    public override ItemType ItemType => ItemType.Weapon;

    protected override string EffectPrefabName => "Rapier_Stab";

    public override List<ItemUpgradeStats> ItemUpgrades => new List<ItemUpgradeStats>()
    {
        new(ItemStatType.Damage, Rarity.Common, 100, 1.75f),
        new(ItemStatType.Cooldown, Rarity.Common, 50, 0.1f),
        new(ItemStatType.Size, Rarity.Common, 50, 0.075f),
        new(ItemStatType.CritDamage, Rarity.Rare, 50, 0.15f),
        new(ItemStatType.Projectiles, Rarity.Legendary, 100, 0.4f),
    };

    public override float BaseCooldown => 1.5f;

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
        if (GetRandomInRadius(Size + 0.75f) == null) { return; }
        for (int i = 0; i < Projectiles; i++)
        {
            StartCoroutine(SpawnSpearStabs(i * 0.2f));
        }
        CurrentCooldown = CooldownRemaining;
    }
    private IEnumerator SpawnSpearStabs(float delay)
    {
        yield return new WaitForSeconds(delay);
        Rigidbody2D rb = _player.GetComponent<Rigidbody2D>();

        Collider2D nearest = GetClosestInRadius(Size + 0.75f);
        Vector2 direction = ((nearest != null ? nearest.transform.position : transform.position) + (Vector3)Random.insideUnitCircle * 0.25f - transform.position).normalized;

        if (rb != null && pullsPlayerForward) { rb.AddForce(direction * 10f, ForceMode2D.Impulse); }

        Projectile projectile = GetPrefab().GetComponent<Projectile>();
        projectile.transform.position = transform.position + (Vector3)(direction * 0.5f);
        projectile.GetComponent<BoxCollider2D>().size = new Vector2(0.5f, SpriteBaseHeight * (Size));
        projectile.GetComponent<BoxCollider2D>().offset = new Vector2(0, (SpriteBaseHeight * (Size)) / 2f);
        projectile.GetComponent<SpriteRenderer>().size = new Vector2(SpriteBaseHeight, SpriteBaseHeight * (Size));
        projectile.Initialize(new ProjectileStats(GetEquipmentStats(), direction), this);
    }
}
