using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

public abstract class ItemSO : ScriptableObject
{
    [BoxGroup("Item")] public Sprite icon;
    [BoxGroup("Item")] [ResizableTextArea] public string itemDescription;
    [BoxGroup("Item")] public GameObject pickupablePrefab;
}