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
        new ItemStats(){ speed = 0.5f, damage = 5 },
        new ItemStats(){ projectiles = 1, damage = 5 },
        new ItemStats(){ pierceCount = 2, damage = 5},
        new ItemStats(){ damage = 10, cooldown = -0.2f },
        new ItemStats(){ speed = 0.5f, duration = 0.5f },
        new ItemStats(){ pierceCount = 2, cooldown = -0.3f},
        new ItemStats(){ projectiles = 1, damage = 5 },
        new ItemStats(){ damage = 15 },
        new ItemStats(){ pierceCount = 2, duration = 0.5f },
        new ItemStats(){ projectiles = 3, cooldown = -0.5f }
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
        for (int i = 0; i < ProjectileCount; i++)
        {
            StartCoroutine(FireIcicles(i * 0.05f));
        }
        CurrentCooldown = Cooldown;
    }
    private IEnumerator FireIcicles(float delay)
    {
        yield return new WaitForSeconds(delay);

        Vector2 direction = Random.insideUnitCircle.normalized;

        Projectile projectile = GetPrefab().GetComponent<Projectile>();
        projectile.transform.position = transform.position;
        projectile.transform.localScale = Vector3.one * Size;

        projectile.Initialize(new ProjectileStats(GetEquipmentStats(), direction, PierceCount), this);
    }
}
