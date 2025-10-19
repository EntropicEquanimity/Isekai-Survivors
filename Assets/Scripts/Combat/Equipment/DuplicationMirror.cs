using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DuplicationMirror : Equipment
{
    public override string Name => "Duplication Mirror";
    public override ItemType ItemType => ItemType.Tool;

    protected override string EffectPrefabName => throw new System.NotImplementedException();

    public override List<ItemUpgradeStats> ItemUpgrades => new List<ItemUpgradeStats>() 
    {
        new(ItemStatType.Projectiles, 0, 20, 1f, 1.2f, 1.6f, 2f, 3f),
    };

    public override float BaseCooldown => throw new System.NotImplementedException();

    public override void OnEquip()
    {
        GameManager.Instance.projectiles.AddModifier(new StatModifier(1 * ItemLevel, StatModType.Flat, this));
    }

    public override void StopItem()
    {
        GameManager.Instance.projectiles.RemoveAllModifiersFromSource(this);
    }

    public override void UseItem()
    {

    }
}
