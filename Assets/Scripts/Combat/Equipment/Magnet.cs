using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Magnet : Equipment
{
    public override string Name => "Magnet";

    public override ItemType ItemType => ItemType.Tool;

    protected override string EffectPrefabName => throw new System.NotImplementedException();

    public override List<ItemUpgradeStats> ItemUpgrades => new List<ItemUpgradeStats>()
    {
        new(ItemStatType.Size, Rarity.Common, 0, 0.125f),
    };

    public override float BaseCooldown => 0;

    public override void TickCooldown(float time)
    {
        if (CurrentCooldown > 0f) { CurrentCooldown -= time; }
    }
    public override void OnEquip()
    {
        GameManager.Instance.pickupRadius.AddModifier(new StatModifier(0.5f, StatModType.Flat, this));
    }
    public override void UseItem()
    {

    }

    public override void Upgrade(ItemStats upgradeStats)
    {
        GameManager.Instance.pickupRadius.AddModifier(new StatModifier(upgradeStats.size, StatModType.Flat, this));
    }

    public override void StopItem()
    {
        GameManager.Instance.pickupRadius.RemoveAllModifiersFromSource(this);
    }
}
