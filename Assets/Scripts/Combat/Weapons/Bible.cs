using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bible : Equipment
{
    private Projectile _bible;
    public override string Name => "Bible";

    public override ItemType ItemType => ItemType.Weapon;

    protected override string EffectPrefabName => "Bible_Effect";

    public override List<ItemStats> UpgradeValues => new List<ItemStats>
    {
        new ItemStats(){ }
    };

    public override void OnEquip()
    {
        RecalculateItemStats();
        SpawnHolyEffect();
    }
    public override void StopItem()
    {
        _bible.gameObject.SetActive(false);
    }
    public override void UseItem()
    {
        SpawnHolyEffect();
    }
    public override void TickCooldown(float time)
    {

    }
    private void SpawnHolyEffect()
    {
        if (_bible != null) { _bible.gameObject.SetActive(false); _bible = null; }
        Projectile projectile = GetPrefab().GetComponent<Projectile>();
        projectile.transform.SetParent(GameManager.Instance.player.transform);
        projectile.transform.localPosition = Vector3.zero;
        projectile.transform.localScale = Vector3.one * Size;
        projectile.Initialize(new ProjectileStats(GetEquipmentStats(), Vector3.zero, Mathf.RoundToInt(Mathf.Infinity), false, true), this);
        _bible = projectile;
    }
}
