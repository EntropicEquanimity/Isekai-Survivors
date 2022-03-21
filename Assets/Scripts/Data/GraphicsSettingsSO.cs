using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Settings/Graphics")]
public class GraphicsSettingsSO : ScriptableObject
{
    [BoxGroup("Colors")] public Color weaponTint = Color.red, equipmentTint = Color.green;
    [BoxGroup("Colors")] public Color commonGrade = Color.white, uncommonGrade = Color.green, rareGrade = Color.blue, epicGrade = Color.magenta, legendaryGrade = Color.yellow;
}
