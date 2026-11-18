namespace Isekai
{
    /// <summary>
    /// Whether a Region is captured: every enemy Base destroyed and its final Wave defeated (released, and every enemy
    /// in it dead). Either can come first.
    /// </summary>
    public sealed class RegionCapture
    {
        public int EnemyBases { get; private set; }
        public int EnemyBasesDestroyed { get; private set; }
        public bool FinalWaveReleased { get; private set; }
        public int FinalWaveRemaining { get; private set; }
        public bool IsCaptured { get; private set; }

        public bool EnemyBasesCleared => EnemyBasesDestroyed >= EnemyBases;
        public bool FinalWaveDefeated => FinalWaveReleased && FinalWaveRemaining <= 0;

        public void AddEnemyBase() => EnemyBases++;

        public void EnemyBaseDestroyed()
        {
            if (EnemyBasesDestroyed < EnemyBases) EnemyBasesDestroyed++;
            Update();
        }

        public void FinalWaveSpawned(int enemies)
        {
            FinalWaveReleased = true;
            FinalWaveRemaining = enemies;
            Update();
        }

        public void FinalWaveEnemyDied()
        {
            if (FinalWaveRemaining > 0) FinalWaveRemaining--;
            Update();
        }

        private void Update() => IsCaptured |= EnemyBasesCleared && FinalWaveDefeated;
    }
}
