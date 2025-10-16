using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(menuName = "Items/Catalogue")]
public class Catalogue : ScriptableObject
{
    public List<EquipmentSO> equipment = new List<EquipmentSO>();

#if UNITY_EDITOR
    [Button]
    public void LoadAllItems()
    {
        equipment = AssetDatabase.FindAssets("t:EquipmentSO").Select(x => AssetDatabase.LoadAssetAtPath<EquipmentSO>(AssetDatabase.GUIDToAssetPath(x))).ToList();
    }
#endif
}
