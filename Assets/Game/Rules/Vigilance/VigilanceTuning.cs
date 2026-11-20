using System;

namespace Isekai
{
    /// <summary>All Vigilance and Wave tuning values in one place (outer ring for now).</summary>
    public static class VigilanceTuning
    {
        /// <summary>Points at which Unaware, Uneasy, Alarmed, Mobilized and Crusade begin.</summary>
        public static readonly float[] StageThresholds = { 0f, 12f, 30f, 55f, 90f };

        /// <summary>Mortals in a Wave sent at each stage.</summary>
        public static readonly int[] WaveSizes = { 5, 7, 9, 12, 14 };

        /// <summary>How much tougher newly arriving mortals are at each stage.</summary>
        public static readonly float[] HealthMultipliers = { 1f, 1.08f, 1.18f, 1.32f, 1.5f };

        public const float KillPoints = 2.5f;
        public const float WitnessedKillPoints = 4f;
        public const float SpottedPoints = 8f;

        /// <summary>Seconds without anything seen before Vigilance starts to decay.</summary>
        public const float DecayGraceSeconds = 20f;
        public const float DecayPointsPerSecond = 0.5f;

        /// <summary>Seconds between Waves while Vigilance stays at Crusade.</summary>
        public const float CrusadeRepeatSeconds = 150f;

        /// <summary>How many Waves may be alive at once in the outer ring.</summary>
        public const int OuterRingWaveCap = 2;

        /// <summary>How far in from the map edge a Wave may appear, in cells.</summary>
        public const int EdgeBandCells = 6;
        public const int SpawnCircleCount = 1;
        public const float SpawnCircleHealth = 150f;
        public const float SpawnCircleRadius = 3f;
        public const float SpawnCircleMinDistanceFromJack = 30f;
        public const float MinSpawnDistanceFromTargets = 9f;

        /// <summary>Seconds before a Wave that found no room tries again.</summary>
        public const float WaveRetrySeconds = 1f;

        public static VigilanceStage StageAt(float points)
        {
            for (int i = StageThresholds.Length - 1; i > 0; i--)
                if (points >= StageThresholds[i]) return (VigilanceStage)i;
            return VigilanceStage.Unaware;
        }

        public static float Threshold(VigilanceStage stage) => StageThresholds[(int)stage];
        public static int WaveSize(VigilanceStage stage) => WaveSizes[(int)stage];
        public static float HealthMultiplier(VigilanceStage stage) => HealthMultipliers[(int)stage];

        public static VigilanceStage Clamp(int stage) =>
            (VigilanceStage)Math.Clamp(stage, (int)VigilanceStage.Unaware, (int)VigilanceStage.Crusade);
    }
}
