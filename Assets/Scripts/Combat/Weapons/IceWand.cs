using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IceWand : Equipment
{
    private Player _player;
    public override string Name => "Ice Wand";

    public override ItemType ItemType => ItemType.Weapon;

    protected override string EffectPrefabName => "Icicle";

    public override List<ItemStats> UpgradeValues => new List<ItemStats>
    {
        new ItemStats(){ }
    };

    public override void OnEquip()
    {
        Debug.Log("Equip not implemented yet!");
        _player = GameManager.Instance.player;
        UseItem();
    }
    public override void StopItem()
    {
        throw new System.NotImplementedException();
    }

    public override void UseItem()
    {
        for (int i = 0; i < ProjectileCount; i++)
        {
            StartCoroutine(FireIcicles(i * 0.2f));
        }
        CurrentCooldown = Cooldown;
    }
    private IEnumerator FireIcicles(float delay)
    {
        yield return new WaitForSeconds(delay);

        Collider2D nearest = GetClosestInRadius(Size + 2f);
        Vector2 direction = ((nearest != null ? nearest.transform.position : transform.position) + (Vector3)Random.insideUnitCircle * 0.25f - transform.position).normalized;

        Projectile projectile = GetPrefab().GetComponent<Projectile>();
        projectile.transform.position = transform.position + (Vector3)direction;
        projectile.transform.localScale = Vector3.one * Size;

        projectile.Initialize(new ProjectileStats(GetEquipmentStats(), direction, PierceCount, false, false), this);
    }
}
