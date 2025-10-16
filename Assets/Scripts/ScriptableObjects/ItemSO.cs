using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public abstract class ItemSO : ScriptableObject
{
    [BoxGroup("Item")] public Sprite icon;
    [BoxGroup("Item")] public string itemDescription;
    [BoxGroup("Item")] public GameObject pickupablePrefab;
    [BoxGroup("Item")] public int dropWeight;
}