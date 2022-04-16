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

    public override List<ItemStats> UpgradeValues => new List<ItemStats>
    {
        new ItemStats(){ duration = 0.2f },
        new ItemStats(){ cooldown = -0.2f },
        new ItemStats(){ damage = 5, speed = 0.25f },
        new ItemStats(){ damage = 5, duration = 0.1f},
        new ItemStats(){ cooldown = -0.1f, speed = 0.25f },
        new ItemStats(){ damage = 10},
        new ItemStats(){ cooldown = -0.1f },
        new ItemStats(){ duration = 0.1f },
        new ItemStats(){ damage = 10 },
        new ItemStats(){ damage = 20, size = 0.25f}
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
        if (GetRandomInRadius(Size + 0.5f) == null) { return; }
        for (int i = 0; i < ProjectileCount; i++)
        {
            StartCoroutine(SpawnSwingEffects(i * 0.1f));
        }
        CurrentCooldown = Cooldown;
    }
    private IEnumerator SpawnSwingEffects(float delay)
    {
        yield return new WaitForSeconds(delay);

        Projectile projectile = GetPrefab().GetComponent<Projectile>();
        projectile.transform.SetParent(GameManager.Instance.player.transform);
        projectile.transform.localPosition = Vector3.zero;
        projectile.transform.localScale = Vector3.one * Size;
        projectile.transform.rotation = Quaternion.Euler(Vector3.zero);
        projectile.Initialize(new ProjectileStats(GetEquipmentStats(), Vector2.zero, Mathf.RoundToInt(Mathf.Infinity), false, false), this);
    }
}
