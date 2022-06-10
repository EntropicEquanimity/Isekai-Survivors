using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DuplicationMirror : Equipment
{
    public override string Name => "Duplication Mirror";
    public override ItemType ItemType => ItemType.Tool;

    protected override string EffectPrefabName => throw new System.NotImplementedException();

    public override List<ItemStats> UpgradeValues => new List<ItemStats>
    {
        new ItemStats(){ projectiles = 0.5f},
        new ItemStats(){ projectiles = 0.5f},
        new ItemStats(){ projectiles = 1}
    };

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
