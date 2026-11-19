using Blondie.Core;

namespace Isekai
{
    /// <summary>
    /// Jack's soul for the Run in progress: soul level and deaths last across Regions and reset when a new Run starts
    /// (the <see cref="PlanetRun"/> changes). The Souls he spends are the Region's stockpile.
    /// </summary>
    public static class SoulRun
    {
        private static SoulLedger s_ledger;
        private static PlanetRun s_owner;

        static SoulRun() => StaticReset.Register(() => { s_ledger = null; s_owner = null; });

        /// <summary>Puts a saved soul level and death count back on the Run in progress (the Run's save, see <see cref="RunSave"/>).</summary>
        public static void Restore(int level, int deaths)
        {
            var ledger = new SoulLedger();
            while (ledger.Level < level && ledger.TryRaiseLevel(_ => true)) { }
            for (int i = 0; i < deaths; i++) ledger.RecordDeath();
            s_ledger = ledger;
            s_owner = RunSession.Run;
        }

        public static SoulLedger Ledger
        {
            get
            {
                if (s_ledger == null || !ReferenceEquals(s_owner, RunSession.Run))
                {
                    s_ledger = new SoulLedger();
                    s_owner = RunSession.Run;
                }
                return s_ledger;
            }
        }
    }
}
