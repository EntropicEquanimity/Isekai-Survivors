using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tesseract : Equipment
{
    public override string Name => "Tesseract";
    public override ItemType ItemType => ItemType.Equipment;

    protected override string EffectPrefabName => throw new System.NotImplementedException();

    public override void OnEquip()
    {
        GameManager.Instance.projectiles.AddModifier(new StatModifier(1 * ItemLevel, StatModType.Flat));
    }

    public override void StopItem()
    {
        throw new System.NotImplementedException();
    }

    public override void UseItem()
    {
        throw new System.NotImplementedException();
    }
}