using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class DebugTester : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (!Application.isEditor) { Destroy(gameObject); }   
    }

    [Header("Probability Settings")]
    [SerializeField] private float testLuckFactor = 0f;
    [SerializeField] private int testSampleSize = 100000;

    [ButtonGroup("Luck Probability")]
    public void TestProbabilityAccuracy()
    {

        // Calculate theoretical probabilities
        float[] theoreticalProbs = Item.CalculateAllRarityProbabilities(testLuckFactor);
        var rarities = (Rarity[])System.Enum.GetValues(typeof(Rarity));

        // Test with actual rolls
        var actualCounts = new Dictionary<Rarity, int>();
        foreach (var rarity in rarities)
        {
            actualCounts[rarity] = 0;
        }

        for (int i = 0; i < testSampleSize; i++)
        {
            var rarity = Item.GetRarityFromLuck(testLuckFactor);
            actualCounts[rarity]++;
        }

        StringBuilder sb = new StringBuilder();
        sb.Append($"=== Probability Test (Luck: {testLuckFactor}, Samples: {testSampleSize}) ===").AppendLine();

        sb.Append("   Rarity   | Theoretical |   Actual    | Difference").AppendLine();
        sb.Append("------------|-------------|-------------|-----------").AppendLine();

        for (int i = 0; i < rarities.Length; i++)
        {
            Rarity rarity = rarities[i];
            float theoretical = theoreticalProbs[i] * 100f;
            float actual = (float)actualCounts[rarity] / testSampleSize * 100f;
            float difference = actual - theoretical;

            sb.Append($"{rarity,-11} | {theoretical,6:F2}%     | {actual,6:F2}%     | {difference,6:F2}%").AppendLine();
        }
        Debug.Log( sb.ToString() );
    }

    [ButtonGroup("Probability")]
    public void ShowAllProbabilities()
    {
        float[] probabilities = Item.CalculateAllRarityProbabilities(testLuckFactor);
        var rarities = (Rarity[])System.Enum.GetValues(typeof(Rarity));

        StringBuilder sb = new StringBuilder();
        sb.Append($"=== Rarity Probabilities (Luck: {testLuckFactor}, Samples: {testSampleSize}) ===").AppendLine();

        for (int i = 0; i < rarities.Length; i++)
        {
            sb.Append($"{rarities[i]}: {probabilities[i]:P2} (1 in {1 / probabilities[i]:F1})").AppendLine();
        }
        Debug.Log(sb.ToString() );
    }

    [ButtonGroup("Probability")]
    public void TestLuckProgression()
    {
        float[] luckValues = { 0f, 100f, 250f, 500f, 750f, 1000f, 1500f, 2000f };

        StringBuilder sb = new StringBuilder();
        sb.Append("=== Luck Progression ===").AppendLine();
        sb.Append("  Luck  | Common  |   Rare  |   Epic  | Legend  | Mythic").AppendLine();
        sb.Append("--------|---------|---------|---------|---------|---------").AppendLine();

        foreach (float luck in luckValues)
        {
            float[] probs = Item.CalculateAllRarityProbabilities(luck);
            sb.Append($"{luck,6} | {probs[0]:P2} | {probs[1]:P2} | {probs[2]:P2} | {probs[3]:P2} | {probs[4]:P2}").AppendLine();
        }
        Debug.Log(sb.ToString() );
    }
}
