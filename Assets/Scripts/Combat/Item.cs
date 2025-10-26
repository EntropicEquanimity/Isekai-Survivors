using Sirenix.OdinInspector;
using System;
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

    #region Luck And Rarity
    public static Rarity GetRarityFromLuck(float luckFactor = 0f) => GetRarity(GetWeightedRandomNumber(luckFactor));
    public static int GetWeightedRandomNumber(float luckFactor = 0f)
    {
        return Mathf.FloorToInt(1000f * (Mathf.Pow(UnityEngine.Random.value, 1f - Mathf.Clamp(luckFactor * 1.2f, 0f, 1000f) / 1000f)));
    }
    static Rarity GetRarity(int value)
    {
        if(value < COMMON_THRESHOLD) { return Rarity.Common; }
        if(value < RARE_THRESHOLD) { return Rarity.Rare; }
        if(value < EPIC_THRESHOLD) { return Rarity.Epic; }
        if(value < LEGENDARY_THRESHOLD) { return Rarity.Legendary; }

        return Rarity.Mythic;
    }
    public static Color GetColorFromRarity(Rarity rarity)
    {
        return rarity switch
        {
            Rarity.Common => new Color(0.5f, 0.5f, 0.5f, 1f), //Grey
            Rarity.Rare => new Color(0.3f, 0.65f, 0.1f, 1f), // Emerald Green
            Rarity.Epic => new Color(0.25f, 0.41f, 0.88f, 1f), // Royal Blue
            Rarity.Legendary => new Color(0.94f, 0.75f, 0.01f, 1f), // Gold
            Rarity.Mythic => new Color(0.68f, 0f, 0f, 1f), // Red
            _ => Color.white,
        };
    }
    public static float CalculateRarityProbability(Rarity targetRarity, float luckFactor = 0f, int sampleSize = 10000)
    {
        int count = 0;
        for (int i = 0; i < sampleSize; i++)
        {
            var rarity = GetRarityFromLuck(luckFactor);
            if (rarity == targetRarity)
                count++;
        }
        return (float)count / sampleSize;
    }
    public static float[] CalculateAllRarityProbabilities(float luckFactor = 0f, int sampleSize = 50000)
    {
        var rarities = (Rarity[])Enum.GetValues(typeof(Rarity));
        int[] counts = new int[rarities.Length];

        for (int i = 0; i < sampleSize; i++)
        {
            var rarity = GetRarityFromLuck(luckFactor);
            counts[rarity.ToIndex()]++;
        }

        float[] probabilities = new float[rarities.Length];
        for (int i = 0; i < rarities.Length; i++)
        {
            probabilities[i] = (float)counts[i] / sampleSize;
        }

        return probabilities;
    }
    public static int COMMON_THRESHOLD = 600;
    public static int RARE_THRESHOLD = 920;
    public static int EPIC_THRESHOLD = 970;
    public static int LEGENDARY_THRESHOLD = 998;
    public static int MYTHIC_THRESHOLD = 1000;
    #endregion
}