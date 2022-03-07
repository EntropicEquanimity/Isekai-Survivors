using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

[CreateAssetMenu(menuName = "Equipment")]
public class EquipmentSO : ItemSO
{
    [BoxGroup("Equipment")] public ItemStats itemStats;
    [BoxGroup("Equipment")] public ItemStats upgradeStats;
}
