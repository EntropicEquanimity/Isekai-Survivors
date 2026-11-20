using System;

namespace Isekai
{
    /// <summary>Jack's soul for the whole Run: his soul level and how often his body has died. The Souls themselves are a Resource.</summary>
    public sealed class SoulLedger
    {
        public int Level { get; private set; } = SoulTuning.StartingLevel;
        public int Deaths { get; private set; }

        public int ReviveCost => SoulRules.ReviveCost(Level);
        public int LevelUpCost => SoulRules.LevelUpCost(Level);
        public int PossessCost(bool bodyAlive) => SoulRules.PossessCost(Level, bodyAlive);
        public float DrainPerSecond(bool doubled) => SoulRules.DrainPerSecond(Level, Deaths, doubled);

        public void RecordDeath() => Deaths++;

        /// <summary>Raises soul level for <see cref="LevelUpCost"/> Souls taken through <paramref name="spend"/> (returns false and does nothing if it refuses).</summary>
        public bool TryRaiseLevel(Func<int, bool> spend)
        {
            if (!spend(LevelUpCost)) return false;
            Level++;
            return true;
        }
    }
}
