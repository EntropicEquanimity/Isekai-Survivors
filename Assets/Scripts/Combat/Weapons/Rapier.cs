using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rapier : Equipment
{
    public bool pullsPlayerForward;
    private Player _player;
    private const int SpriteBaseHeight = 2;
    public override string Name => "Rapier";

    public override ItemType ItemType => ItemType.Weapon;

    protected override string EffectPrefabName => "Rapier_Stab";

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
        if (GetRandomInRadius(Size + SpriteBaseHeight) == null) { return; }
        for (int i = 0; i < ProjectileCount; i++)
        {
            StartCoroutine(SpawnSpearStabs(i * 0.2f));
        }
        CurrentCooldown = Cooldown;
    }
    private IEnumerator SpawnSpearStabs(float delay)
    {
        yield return new WaitForSeconds(delay);
        Rigidbody2D rb = _player.GetComponent<Rigidbody2D>();

        Collider2D nearest = GetClosestInRadius(Size + 2f);
        Vector2 direction = ((nearest != null ? nearest.transform.position : transform.position) + (Vector3)Random.insideUnitCircle * 0.25f - transform.position).normalized;

        if (rb != null && pullsPlayerForward) { rb.AddForce(direction * 10f, ForceMode2D.Impulse); }

        Projectile projectile = GetPrefab().GetComponent<Projectile>();
        projectile.transform.position = transform.position;
        projectile.GetComponent<BoxCollider2D>().size = new Vector2(0.3f, SpriteBaseHeight * (Size + 1f) - 0.2f);
        projectile.GetComponent<BoxCollider2D>().offset = new Vector2(0, (SpriteBaseHeight * (Size + 1f) - 0.2f) / 2f);
        projectile.GetComponent<SpriteRenderer>().size = new Vector2(SpriteBaseHeight, SpriteBaseHeight * (Size + 1f));
        projectile.Initialize(new ProjectileStats(GetEquipmentStats(), direction, Mathf.RoundToInt(Mathf.Infinity), false, false), this);
    }
}
