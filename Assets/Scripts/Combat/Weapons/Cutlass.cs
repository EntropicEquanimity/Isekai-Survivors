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
        new(ItemStatType.Cooldown, 100, 0, 0.05f, 0.08f, 0.15f, 0.25f, 0.4f),
        new(ItemStatType.Damage, 150, 0, 1f, 1.2f, 1.6f, 2f, 3f),
        new(ItemStatType.CritChance, 50, 50, 0.07f, 0.1f, 0.15f, 0.2f, 0.35f),
        new(ItemStatType.CritDamage, 50, 75, 0.1f, 0.12f, 0.18f, 0.25f, 0.35f),
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
        for (int i = 0; i < ProjectileCount; i++)
        {
            StartCoroutine(SpawnSwingEffects(i * 0.1f));
        }
        CurrentCooldown = CooldownRemaining;
    }
    private IEnumerator SpawnSwingEffects(float delay)
    {
        yield return new WaitForSeconds(delay);

        Projectile projectile = GetPrefab().GetComponent<Projectile>();
        projectile.transform.SetParent(GameManager.Instance.player.transform);
        projectile.transform.localPosition = Vector3.zero;
        projectile.transform.localScale = Vector3.one * Size;
        projectile.transform.rotation = Quaternion.Euler(Vector3.zero);
        projectile.Initialize(new ProjectileStats(GetEquipmentStats(), Vector2.zero, 999999999), this);
    }
}
