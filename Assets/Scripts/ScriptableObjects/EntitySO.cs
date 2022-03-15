using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

[CreateAssetMenu(menuName = "Entity")]
public class EntitySO : ScriptableObject
{
    [ShowAssetPreview] public Sprite entitySprite;
    public EntityStats entityStats;
}