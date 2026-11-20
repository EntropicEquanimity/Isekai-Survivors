namespace Isekai
{
    /// <summary>Every number of the Traits in one place to tune.</summary>
    public static class TraitTuning
    {
        /// <summary>The chance an ordinary unit has one Trait, and an elite one.</summary>
        public const float OrdinaryOdds = 0.10f;
        public const float EliteOdds = 0.25f;

        /// <summary>A Hero always has between this many and that many Traits.</summary>
        public const int HeroMin = 2;
        public const int HeroMax = 3;

        /// <summary>A Sneaky unit stays visible this long after each attack ("then visible for 3 seconds").</summary>
        public const float VisibleSeconds = 3f;

        /// <summary>An Ambusher backs off after this many kills, for this long (it flees, then fights again).</summary>
        public const int BackOffKills = 3;
        public const float BackOffSeconds = 8f;

        /// <summary>A Vigilant witness sends its alarm after this share of the usual delay.</summary>
        public const float VigilantAlarmDelayShare = 0.5f;

        /// <summary>A Protector is "near" its settlement or post within this many cells.</summary>
        public const float ProtectorNearCells = 8f;

        /// <summary>A Purifier heals each ally within this many cells, this much a second ("slowly").</summary>
        public const float PurifierHealCells = 5f;
        public const float PurifierHealPerSecond = 1.5f;

        /// <summary>An Unruly demon steps this many cells toward trouble every so many seconds, looking this far for it.</summary>
        public const float UnrulyStepCells = 5f;
        public const float UnrulyStepSeconds = 6f;
        public const float UnrulyLookCells = 40f;

        /// <summary>How often the Traits' timed effects (hiding, staying near, healing) are looked at, in seconds.</summary>
        public const float TickSeconds = 0.25f;
    }
}
