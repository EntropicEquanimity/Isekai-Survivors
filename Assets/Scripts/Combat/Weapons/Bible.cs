using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bible : Equipment
{
    private Projectile _bible;
    public override string Name => "Bible";

    public override ItemType ItemType => ItemType.Weapon;

    protected override string EffectPrefabName => "Bible_Effect";

    public override List<ItemUpgradeStats> ItemUpgrades => new List<ItemUpgradeStats>()
    {
        new(ItemStatType.Size, Rarity.Common, 0, 0.075f),
        new(ItemStatType.Damage, Rarity.Common, 0, 1f),
    };

    public override float BaseCooldown => throw new System.NotImplementedException();

    public override void OnEquip()
    {
        HolyEffect();
    }
    public override void StopItem()
    {
        _bible.gameObject.SetActive(false);
    }
    public override void UseItem()
    {
        HolyEffect();
    }
    public override void TickCooldown(float time)
    {

    }
    private void HolyEffect()
    {
        Projectile projectile = _bible;
        if (projectile == null) 
        {
            projectile = GetPrefab().GetComponent<Projectile>();
            projectile.transform.SetParent(GameManager.Instance.player.transform);
            projectile.transform.localPosition = Vector3.zero;
            _bible = projectile;
        }

        projectile.transform.localScale = Vector2.one * Size;
        ItemStats stats = GetEquipmentStats();
        stats.duration = Mathf.Infinity;
        stats.pierce = Mathf.Infinity;
        projectile.Initialize(new ProjectileStats(stats, Vector3.zero, true), this);
    }
}
