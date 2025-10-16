using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public abstract class EntitySO : ScriptableObject
{
    [AssetsOnly] public Sprite entitySprite;
    public EntityStats entityStats;
}