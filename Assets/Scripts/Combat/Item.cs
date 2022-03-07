using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Item : MonoBehaviour
{
    public const string EffectPrefabPath = "Items/Effects/";
    public abstract string Name { get; }
    public abstract ItemType ItemType { get; }
    public GameObject EffectPrefab
    {
        get
        {
            if (_effectPrefab != null) { return _effectPrefab; }
            else { _effectPrefab = LoadEffectPrefab; return _effectPrefab; }
        }
    }
    public int ItemLevel { get; protected set; }
    protected GameObject _effectPrefab;
    protected abstract string EffectPrefabName { get; }
    public GameObject LoadEffectPrefab => Resources.Load(EffectPrefabPath + EffectPrefabName) as GameObject;
    public abstract void OnEquip();
}
public enum ItemType
{
    Weapon,
    Equipment
}