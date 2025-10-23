using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cutlass : Equipment
{
    private Player _player;
    private const int SpriteBaseHeight = 2;
    public override string Name => "Cutlass";

    public override ItemType ItemType => ItemType.Weapon;

    protected override string EffectPrefabName => "Cutlass_Swing";

    public override List<ItemUpgradeStats> ItemUpgrades => new List<ItemUpgradeStats>()
    {
        new(ItemStatType.Cooldown, Rarity.Common, 100, 0.08f),
        new(ItemStatType.Damage, Rarity.Common, 100, 2.2f),
        new(ItemStatType.CritDamage, Rarity.Common, 50, 0.15f),
        new(ItemStatType.CritChance, Rarity.Common, 50, 0.08f),
        new(ItemStatType.Duration, Rarity.Rare, 50, 0.05f),
        new(ItemStatType.Projectiles, Rarity.Legendary, 100, 0.4f),
    };

    public override float BaseCooldown => 2.5f;

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
        if (GetRandomInRadius(Size + 0.5f) == null) { return; } //If no enemies are nearby, don't attack
        for (int i = 0; i < Projectiles; i++)
        {
            StartCoroutine(SpawnSwingEffects(i * 0.1f));
        }
        CurrentCooldown = CooldownRemaining;
    }
    private IEnumerator SpawnSwingEffects(float delay)
    {
        yield return new WaitForSeconds(delay);
        Collider2D closest = GetClosestInRadius(Size + 0.5f);
        Projectile projectile = GetPrefab().GetComponent<Projectile>();
        projectile.transform.SetParent(GameManager.Instance.player.transform);
        projectile.transform.localPosition = Vector3.zero;
        projectile.transform.localScale = Vector3.one * Size;
        projectile.transform.up = closest == null ? Vector3.up : (closest.transform.position  - transform.position).normalized;
        projectile.rotationSpeed = Mathf.Clamp(-500 + -25 * Speed, -1000, -100);
        projectile.Initialize(new ProjectileStats(GetEquipmentStats(), Vector2.zero), this);
    }
}
