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
    private static System.Random _random = new System.Random();

    public static Rarity GetWeightedRarity(float luckFactor = 0f)
    {
        luckFactor = Mathf.Max(0f, luckFactor);

        // Get probabilities for current luck level
        float[] probabilities = GetRarityProbabilities(luckFactor);

        // Generate random number and determine rarity
        float roll = (float)_random.NextDouble();
        float cumulative = 0f;

        for (int i = 0; i < probabilities.Length; i++)
        {
            cumulative += probabilities[i];
            if (roll < cumulative)
            {
                // Explicit mapping instead of casting
                return i switch
                {
                    0 => Rarity.Common,
                    1 => Rarity.Rare,
                    2 => Rarity.Epic,
                    3 => Rarity.Legendary,
                    4 => Rarity.Mythic,
                    _ => Rarity.Common
                };
            }
        }

        // Fallback to Common if something goes wrong
        return Rarity.Common;
    }

    /// <summary>
    /// Get precise probabilities based on luck tier
    /// </summary>
    private static float[] GetRarityProbabilities(float luck)
    {
        float[] probs = new float[] { 0.8f, 0.19f, 0.01f, 0f, 0f }; // Common, Rare, Epic, Legendary, Mythic

        if (luck < 100f)
        {
            // Tier 1: Only up to Epic
            float t = luck / 100f; // 0 to 1

            probs[0] = Mathf.Lerp(0.80f, 0.60f, t);  // Common: 80% → 60%
            probs[1] = Mathf.Lerp(0.19f, 0.30f, t);  // Rare:   19% → 30%
            probs[2] = Mathf.Lerp(0.01f, 0.09f, t);  // Epic:    1% → 9%
            probs[3] = 0.01f * t;                     // Legendary: 0% → 1%
            probs[4] = 0f;                            // Mythic: 0%
        }
        else if (luck < 200f)
        {
            // Tier 2: Up to Legendary
            float t = (luck - 100f) / 100f; // 0 to 1

            probs[0] = Mathf.Lerp(0.60f, 0.50f, t);  // Common: 60% → 50%
            probs[1] = Mathf.Lerp(0.30f, 0.30f, t);  // Rare:   30% → 30%
            probs[2] = Mathf.Lerp(0.09f, 0.15f, t);  // Epic:    9% → 15%
            probs[3] = Mathf.Lerp(0.01f, 0.05f, t);  // Legendary: 1% → 5%
            probs[4] = 0f;                            // Mythic: 0%
        }
        else
        {
            // Tier 3: Includes Mythic - starting at 0.01% at luck 201
            float t = Mathf.Clamp01((luck - 200f) / 800f); // 0 to 1 for 200-1000 luck

            probs[0] = Mathf.Lerp(0.50f, 0.00f, t);       // Common: 50% → 0%
            probs[1] = Mathf.Lerp(0.30f, 0.00f, t);       // Rare:   30% → 0%
            probs[2] = Mathf.Lerp(0.15f, 0.00f, t);       // Epic:   15% → 0%
            probs[3] = Mathf.Lerp(0.05f, 0.50f, t);       // Legendary: 5% → 50%

            // Mythic starts at 0.01% at luck 201 and grows to 50% at luck 1000
            float mythicStart = 0.0001f; // 0.01%
            float mythicEnd = 0.50f;     // 50%
            probs[4] = Mathf.Lerp(mythicStart, mythicEnd, t); // Mythic: 0.01% → 50%
        }

        return probs;
    }
    /// <summary>
    /// Get the exact probabilities for display/analysis
    /// </summary>
    public static float[] CalculateAllRarityProbabilities(float luckFactor = 0f)
    {
        return GetRarityProbabilities(luckFactor);
    }

    /// <summary>
    /// Get individual rarity probability
    /// </summary>
    public static float CalculateRarityProbability(Rarity rarity, float luckFactor = 0f)
    {
        float[] probs = GetRarityProbabilities(luckFactor);

        // Safe array access using switch instead of direct casting
        return rarity switch
        {
            Rarity.Common => probs[0],
            Rarity.Rare => probs[1],
            Rarity.Epic => probs[2],
            Rarity.Legendary => probs[3],
            Rarity.Mythic => probs[4],
            _ => probs[0] // fallback to Common
        };
    }
    public static Rarity GetRarityFromLuck(float luck)
    {
        return GetWeightedRarity(luck);
    }
    // Utility methods
    public static Color GetRarityColor(Rarity rarity)
    {
        return rarity switch
        {
            Rarity.Common => new Color(0.3f, 0.75f, 0.3f),
            Rarity.Rare => new Color(0.33f, 0.6f, 1f),
            Rarity.Epic => new Color(0.6f, 0.5f, 0.8f),
            Rarity.Legendary => new Color(0.93f, 0.75f, 0.2f),
            Rarity.Mythic => new Color(0.88f, 0.07f, 0.37f),
            _ => Color.white
        };
    }
    #endregion
}