using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shortbow : Equipment
{
    public override string Name => "Shortbow";

    public override ItemType ItemType => ItemType.Weapon;

    protected override string EffectPrefabName => "Shortbow_Arrow";

    public override List<ItemStats> UpgradeValues => new List<ItemStats>
    {
        new ItemStats(){ damage = 10},
        new ItemStats(){ speed = 0.25f, duration = 0.25f },
        new ItemStats(){ projectiles = 1, damage = 5},
        new ItemStats(){ cooldown = -0.25f, pierceCount = 1},
        new ItemStats(){ size = 0.25f, damage = 5},
        new ItemStats(){ projectiles = 1, pierceCount = 1},
        new ItemStats(){ speed = 0.25f, cooldown = -0.25f },
        new ItemStats(){ damage = 10 },
        new ItemStats(){ damage = 5, speed = 0.25f },
        new ItemStats(){ projectiles = 2, damage = 15, cooldown = -0.5f }
    };

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
        Collider2D nearest = GetClosestInRadius(15f);
        Vector2 direction = ((nearest != null ? nearest.transform.position : transform.position) - transform.position).normalized;
        //if (GetRandomInRadius(Range + 2f) == null) { return; }
        for (int i = 0; i < ProjectileCount; i++)
        {
            StartCoroutine(SpawnArrows(i * 0.05f, direction + Random.insideUnitCircle * 0.25f));
        }
        CurrentCooldown = Cooldown;
    }
    private IEnumerator SpawnArrows(float delay, Vector2 direction)
    {
        yield return new WaitForSeconds(delay);

        Projectile projectile = GetPrefab().GetComponent<Projectile>();
        projectile.transform.position = transform.position + (Vector3)direction * 0.5f;
        projectile.transform.localScale = Vector3.one * Size;

        projectile.Initialize(new ProjectileStats(GetEquipmentStats(), direction.normalized, PierceCount), this);
    }
}
