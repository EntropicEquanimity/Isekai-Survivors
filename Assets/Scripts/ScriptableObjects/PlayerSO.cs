using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

[CreateAssetMenu(menuName = "Data/Character")]
public class PlayerSO : EntitySO
{
    [ResizableTextArea] public string characterDescription;
    public EquipmentSO startingWeapon;
    public PlayerStats playerStats;
}