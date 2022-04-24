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

    public override List<ItemStats> UpgradeValues => new List<ItemStats>
    {
        new ItemStats(){ damage = 15 },
        new ItemStats(){ cooldown = -0.5f, damage = 10},
        new ItemStats(){ projectiles = 1},
        new ItemStats(){ cooldown = -0.5f, pierceCount = 2 },
        new ItemStats(){ damage = 15 },
        new ItemStats(){ size = 0.2f, cooldown = -0.5f },
        new ItemStats(){ projectiles = 1 },
        new ItemStats(){ damage = 15, pierceCount = 2},
        new ItemStats(){ damage = 15, cooldown = -0.5f },
        new ItemStats(){ damage = 30, pierceCount = 5, size = 0.4f }
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
        //if (GetRandomInRadius(Range + 1f) == null) { return; }
        for (int i = 0; i < ProjectileCount; i++)
        {
            StartCoroutine(SpawnSwingEffects(i * 0.2f));
        }
        CurrentCooldown = Cooldown;
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
        projectile.Initialize(new ProjectileStats(GetEquipmentStats(), direction, PierceCount), this);
    }
}
