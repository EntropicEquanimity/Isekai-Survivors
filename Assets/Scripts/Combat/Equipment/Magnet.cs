using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Magnet : Equipment
{
    public override string Name => "Magnet";

    public override ItemType ItemType => ItemType.Tool;

    protected override string EffectPrefabName => throw new System.NotImplementedException();

    public override List<ItemStats> UpgradeValues => new List<ItemStats> { };
    public override List<string> CustomUpgradeValues => new List<string>
    {
        "Pickup Radius + 0.5",
        "Pickup Radius + 0.5",
        "Pickup Radius + 0.5",
        "Pickup Radius + 0.5",
        "Pickup Radius + 0.5",
        "Pickup Radius + 0.5",
        "Pickup Radius + 0.5",
        "Pickup Radius + 0.5",
    };

    public override void TickCooldown(float time)
    {
        if (CurrentCooldown > 0f) { CurrentCooldown -= time; }
    }
    public override void OnEquip()
    {
        GameManager.Instance.pickupRadius.AddModifier(new StatModifier(0.5f, StatModType.Flat, this));
    }
    public override void Upgrade()
    {
        GameManager.Instance.pickupRadius.AddModifier(new StatModifier(0.5f, StatModType.Flat, this));
    }
    public override void UseItem()
    {

    }

    public override void StopItem()
    {
        GameManager.Instance.pickupRadius.RemoveAllModifiersFromSource(this);
    }
}
