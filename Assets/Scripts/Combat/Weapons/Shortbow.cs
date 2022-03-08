using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shortbow : Equipment
{
    public override string Name => "Shortbow";

    public override ItemType ItemType => ItemType.Weapon;

    protected override string EffectPrefabName => "Shortbow_Arrow";

    public override void OnEquip()
    {
        Debug.Log("Equip not implemented yet!");
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

        projectile.Initialize(new ProjectileStats(GetEquipmentStats(), direction.normalized, PierceCount, false, false), this);
    }
}
