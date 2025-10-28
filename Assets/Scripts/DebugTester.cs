using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class DebugTester : MonoBehaviour
{
    [Header("Test Settings")]
    [SerializeField] private float testLuck = 0f;
    [SerializeField] private int sampleSize = 10000;

    [ButtonGroup("Luck Testing")]
    void TestAllKeyLevels()
    {
        StringBuilder sb = new StringBuilder();
        float[] testLevels = { 0f, 50f, 100f, 150f, 200f, 201f, 500f, 1000f };

        sb.AppendLine("=== Luck Level Probabilities ===");
        sb.AppendLine("Luck  | Common | Rare  | Epic  | Legend | Mythic");
        sb.AppendLine("------|--------|-------|-------|--------|-------");

        foreach (float luck in testLevels)
        {
            float[] probs = Item.CalculateAllRarityProbabilities(luck);
            sb.AppendLine($"{luck,5} | {probs[0]:P1} | {probs[1]:P1} | {probs[2]:P1} | {probs[3]:P1}  | {probs[4]:P4}");
        }

        Debug.Log(sb.ToString());
    }

    [ButtonGroup("Luck Testing")]
    void TestMythicProgression()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("=== Mythic Probability Progression ===");
        sb.AppendLine("Luck  | Mythic Chance | 1 in X");
        sb.AppendLine("------|---------------|--------");

        float[] mythicLevels = { 200f, 201f, 250f, 300f, 400f, 500f, 750f, 1000f };

        foreach (float luck in mythicLevels)
        {
            float mythicProb = Item.CalculateRarityProbability(Rarity.Mythic, luck);
            sb.AppendLine($"{luck,5} | {mythicProb:P4}      | {1 / mythicProb:F0}");
        }

        Debug.Log(sb.ToString());
    }

    [ButtonGroup("Luck Testing")]
    void EmpiricalTestCurrentLuck()
    {
        StringBuilder sb = new StringBuilder();
        float luck = 201f; // Test luck 201 specifically
        int sampleSize = 100000;

        int commonCount = 0, rareCount = 0, epicCount = 0, legendaryCount = 0, mythicCount = 0;

        for (int i = 0; i < sampleSize; i++)
        {
            var rarity = Item.GetWeightedRarity(luck);
            switch (rarity)
            {
                case Rarity.Common: commonCount++; break;
                case Rarity.Rare: rareCount++; break;
                case Rarity.Epic: epicCount++; break;
                case Rarity.Legendary: legendaryCount++; break;
                case Rarity.Mythic: mythicCount++; break;
            }
        }

        float total = sampleSize;
        sb.AppendLine($"=== Empirical Test - Luck {luck} ({sampleSize} samples) ===");
        sb.AppendLine($"Common:    {commonCount / total:P4} ({commonCount} times)");
        sb.AppendLine($"Rare:      {rareCount / total:P4} ({rareCount} times)");
        sb.AppendLine($"Epic:      {epicCount / total:P4} ({epicCount} times)");
        sb.AppendLine($"Legendary: {legendaryCount / total:P4} ({legendaryCount} times)");
        sb.AppendLine($"Mythic:    {mythicCount / total:P4} ({mythicCount} times)");

        Debug.Log(sb.ToString());
    }
}
