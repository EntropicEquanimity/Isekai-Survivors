using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(menuName = "Data/Character")]
public class PlayerSO : EntitySO
{
    public string characterDescription;
    public EquipmentSO startingWeapon;
    public PlayerStats playerStats;
    public GameObject characterPrefab;
}