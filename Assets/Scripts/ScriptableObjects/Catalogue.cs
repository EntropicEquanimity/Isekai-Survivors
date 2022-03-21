using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Items/Catalogue")]
public class Catalogue : ScriptableObject
{
    public List<EquipmentSO> weapons = new List<EquipmentSO>();
}
