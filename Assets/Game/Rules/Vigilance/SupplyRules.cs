using System;
using System.Collections.Generic;

namespace Isekai
{
    /// <summary>The mortals' Supply and troop-raising numbers in one place (outer ring for now).</summary>
    public static class SupplyTuning
    {
        /// <summary>Supply a standing farm and a standing village each make per minute.</summary>
        public const float FarmPerMinute = 6f;
        public const float VillagePerMinute = 4f;

        /// <summary>Supply the Region's mortals start with.</summary>
        public const float StartingSupply = 10f;

        /// <summary>Supply one raised troop costs.</summary>
        public const float TroopCost = 5f;

        /// <summary>Seconds between troops at each stage, per stronghold (Unaware raises none, so its entry is never used).</summary>
        public static readonly float[] TroopIntervals = { 0f, 40f, 28f, 18f, 10f };

        /// <summary>Most troops a stronghold raises: the limit "faster at each stage" runs up to.</summary>
        public const int SeatTroopLimit = 10;
        public const int TownTroopLimit = 4;

        /// <summary>Extra guards each guarded settlement holds at each stage, on top of its markers' own.</summary>
        public static readonly int[] ExtraPatrols = { 0, 1, 2, 3, 4 };
    }

    /// <summary>What the mortals' Supply and troops do, as pure functions.</summary>
    public static class SupplyRules
    {
        /// <summary>Supply a settlement makes per minute: farms and villages while standing; razed ones, camps and the Seat make none.</summary>
        public static float PerMinute(SettlementKind kind, bool razed)
        {
            if (razed) return 0f;
            return kind switch
            {
                SettlementKind.Farm => SupplyTuning.FarmPerMinute,
                SettlementKind.Village => SupplyTuning.VillagePerMinute,
                _ => 0f,
            };
        }

        /// <summary>Supply all of these settlements make per minute together.</summary>
        public static float RegionPerMinute(IEnumerable<(SettlementKind kind, bool razed)> settlements)
        {
            float total = 0f;
            foreach ((SettlementKind kind, bool razed) in settlements) total += PerMinute(kind, razed);
            return total;
        }

        /// <summary>The fort and the towns raise troops; villages are the towns of the outer ring (no separate town kind yet).</summary>
        public static bool RaisesTroops(SettlementKind kind) => kind == SettlementKind.Seat || kind == SettlementKind.Village;

        /// <summary>Most troops a settlement raises (0 when it raises none).</summary>
        public static int TroopLimit(SettlementKind kind) => kind switch
        {
            SettlementKind.Seat => SupplyTuning.SeatTroopLimit,
            SettlementKind.Village => SupplyTuning.TownTroopLimit,
            _ => 0,
        };

        /// <summary>
        /// Seconds between troops at <paramref name="stage"/>, or null when none are raised: an undetected Region
        /// (Unaware and no alarmed area) raises none. An area alerted by alarms raises as fast as Uneasy at least.
        /// </summary>
        public static float? TroopInterval(VigilanceStage stage, bool areaAlerted)
        {
            if (stage == VigilanceStage.Unaware && !areaAlerted) return null;
            VigilanceStage effective = stage == VigilanceStage.Unaware ? VigilanceStage.Uneasy : stage;
            return SupplyTuning.TroopIntervals[(int)effective];
        }

        /// <summary>Extra guards a guarded settlement should hold at <paramref name="stage"/>.</summary>
        public static int PatrolCount(VigilanceStage stage) => SupplyTuning.ExtraPatrols[(int)stage];

        /// <summary>Guards to add to a settlement that holds <paramref name="have"/> extra ones now (patrols never shrink).</summary>
        public static int PatrolsToAdd(VigilanceStage stage, int have) => Math.Max(0, PatrolCount(stage) - have);
    }

    /// <summary>The Region's mortals' Supply: what farms and villages made, minus what troops cost.</summary>
    public sealed class SupplyPool
    {
        public SupplyPool(float starting = SupplyTuning.StartingSupply) => Amount = starting;

        public float Amount { get; private set; }

        public void Add(float amount) => Amount += Math.Max(0f, amount);

        /// <summary>Takes <paramref name="cost"/> if there is enough.</summary>
        public bool TryTake(float cost)
        {
            if (Amount < cost) return false;
            Amount -= cost;
            return true;
        }

        /// <summary>Makes <paramref name="perMinute"/> for <paramref name="seconds"/>.</summary>
        public void Produce(float perMinute, float seconds) => Add(perMinute * seconds / 60f);
    }

    /// <summary>One stronghold's troop raising: a timer that runs only while the Region is alerted, a limit, and Supply to pay.</summary>
    public sealed class TroopRaiser
    {
        private readonly SettlementKind _kind;
        private float _timer;

        public TroopRaiser(SettlementKind kind) => _kind = kind;

        public int Raised { get; private set; }

        /// <summary>Runs the timer for <paramref name="dt"/> seconds; true when a troop is raised now (its cost is taken from <paramref name="supply"/>).</summary>
        public bool Tick(float dt, VigilanceStage stage, bool areaAlerted, SupplyPool supply)
        {
            float? interval = SupplyRules.TroopInterval(stage, areaAlerted);
            if (interval == null || !SupplyRules.RaisesTroops(_kind) || Raised >= SupplyRules.TroopLimit(_kind))
            {
                _timer = 0f;
                return false;
            }
            _timer += dt;
            if (_timer < interval.Value) return false;
            if (!supply.TryTake(SupplyTuning.TroopCost)) return false; // waits, ready, until Supply comes
            _timer = 0f;
            Raised++;
            return true;
        }
    }
}
