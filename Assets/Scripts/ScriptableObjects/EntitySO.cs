using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

public abstract class EntitySO : ScriptableObject
{
    [ShowAssetPreview] public Sprite entitySprite;
    public EntityStats entityStats;
}