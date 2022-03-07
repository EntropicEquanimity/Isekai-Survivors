using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Player Character")]
public class PlayerSO : EntitySO
{
    public EquipmentSO startingWeapon;
    public PlayerStats playerStats;
}