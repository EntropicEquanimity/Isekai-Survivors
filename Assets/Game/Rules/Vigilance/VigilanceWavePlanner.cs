namespace Isekai
{
    /// <summary>
    /// When Waves are due: one for each stage Vigilance newly reaches, then one every few minutes at Crusade, with at
    /// most a cap of them alive at once. Waves owed while the cap is full wait their turn.
    /// </summary>
    public sealed class VigilanceWavePlanner
    {
        private readonly int _cap;
        private readonly float _repeatSeconds;
        private VigilanceStage _seen;
        private float _repeatClock;

        public VigilanceWavePlanner(int cap, float repeatSeconds)
        {
            _cap = cap < 1 ? 1 : cap;
            _repeatSeconds = repeatSeconds <= 0f ? 1f : repeatSeconds;
        }

        /// <summary>Waves owed and not yet sent.</summary>
        public int Pending { get; private set; }

        /// <summary>Waves sent and not yet over.</summary>
        public int Alive { get; private set; }

        public int Cap => _cap;

        /// <summary>A Wave can be sent now: one is owed and there is room.</summary>
        public bool CanSend => Pending > 0 && Alive < _cap;

        /// <summary>
        /// Tells the planner the highest stage reached. Each stage above the last one it was told of is owed a Wave
        /// (so a stage lost to decay and regained is not a new stage). Lowering it (debug) lets the stages be reached again.
        /// </summary>
        public void Reached(VigilanceStage highest)
        {
            if (highest > _seen) Pending += (int)highest - (int)_seen;
            _seen = highest;
        }

        /// <summary>Time passes at <paramref name="stage"/>: at Crusade a Wave is owed every repeat period, unless the cap is already reached.</summary>
        public void Tick(float seconds, VigilanceStage stage)
        {
            if (stage != VigilanceStage.Crusade) { _repeatClock = 0f; return; }
            if (seconds <= 0f) return;
            _repeatClock += seconds;
            if (_repeatClock < _repeatSeconds) return;
            _repeatClock = 0f;
            if (Alive + Pending < _cap) Pending++;
        }

        /// <summary>Owes one more Wave at once (the debug menu); false when the cap is already reached.</summary>
        public bool OweNow()
        {
            if (Alive + Pending >= _cap) return false;
            Pending++;
            return true;
        }

        /// <summary>A Wave was sent.</summary>
        public bool Sent()
        {
            if (!CanSend) return false;
            Pending--;
            Alive++;
            return true;
        }

        /// <summary>A Wave is over (all its mortals dead or gone).</summary>
        public void Ended()
        {
            if (Alive > 0) Alive--;
        }
    }
}
