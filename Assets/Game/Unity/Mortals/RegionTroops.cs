using System.Collections.Generic;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// Vigilance's effect on the mortals: once the Region is alerted the fort and the towns raise troops from
    /// <see cref="RegionSupply"/> (faster at each stage, up to a limit; an undetected Region raises none), and each
    /// guarded settlement holds more patrols at higher stages, tougher by <see cref="VigilanceTuning.HealthMultiplier"/>.
    /// Troops and patrols are guards spawned through <see cref="RegionMortals.AddPatrols"/>.
    /// </summary>
    [RegionSystem]
    public sealed class RegionTroops : MonoBehaviour
    {
        private readonly Dictionary<RegionSettlements.Settlement, TroopRaiser> _raisers = new();
        private readonly Dictionary<RegionSettlements.Settlement, int> _patrols = new();
        private VigilanceStage _patrolStage = VigilanceStage.Unaware;

        public static RegionTroops Current { get; private set; }

        /// <summary>Troops raised so far in this Region.</summary>
        public int TroopsRaised { get; private set; }

        /// <summary>Extra patrols added so far in this Region.</summary>
        public int PatrolsAdded { get; private set; }

        /// <summary>Extra patrols a settlement holds (0 when none).</summary>
        public int PatrolsAt(RegionSettlements.Settlement s) => _patrols.TryGetValue(s, out int n) ? n : 0;

        /// <summary>Troops a settlement has raised.</summary>
        public int TroopsAt(RegionSettlements.Settlement s) => _raisers.TryGetValue(s, out TroopRaiser r) ? r.Raised : 0;

        private void OnEnable() => Current = this;

        private void OnDisable()
        {
            if (Current == this) Current = null;
        }

        private void Update()
        {
            RegionMortals mortals = RegionMortals.Current;
            RegionSettlements settlements = RegionSettlements.Current;
            RegionSupply supply = RegionSupply.Current;
            VigilanceDirector director = VigilanceDirector.Current;
            if (mortals == null || settlements == null || supply == null || director == null || settlements.SeatCaptured) return;

            VigilanceStage stage = director.Stage;
            if (stage != _patrolStage)
            {
                _patrolStage = stage;
                mortals.PatrolStrength = VigilanceTuning.HealthMultiplier(stage);
            }

            float dt = Time.deltaTime * supply.Speed;
            foreach (RegionSettlements.Settlement s in settlements.Settlements)
            {
                if (s.IsRazed) continue;
                if (HoldsPatrols(s)) AddPatrolsTo(mortals, s, stage);
                if (!SupplyRules.RaisesTroops(s.Kind)) continue;
                if (!_raisers.TryGetValue(s, out TroopRaiser raiser)) _raisers[s] = raiser = new TroopRaiser(s.Kind);
                if (raiser.Tick(dt, stage, AreaAlerted(mortals, s), supply.Pool))
                    TroopsRaised += mortals.AddPatrols(s, 1);
            }
        }

        private static bool HoldsPatrols(RegionSettlements.Settlement s) =>
            s.Kind == SettlementKind.Seat || s.Kind == SettlementKind.Village || s.Kind == SettlementKind.Camp;

        private void AddPatrolsTo(RegionMortals mortals, RegionSettlements.Settlement s, VigilanceStage stage)
        {
            int have = PatrolsAt(s);
            int add = SupplyRules.PatrolsToAdd(stage, have);
            if (add == 0) return;
            int added = mortals.AddPatrols(s, add);
            _patrols[s] = have + added;
            PatrolsAdded += added;
        }

        private static bool AreaAlerted(RegionMortals mortals, RegionSettlements.Settlement s)
        {
            RegionAlarms alarms = RegionAlarms.Current;
            if (alarms == null) return false;
            Vector3 at = mortals.Homes != null ? mortals.Homes.Centre(s) : s.Markers.Count > 0 ? s.Markers[0].Position : Vector3.zero;
            return alarms.IsAlerted(at);
        }

        public string Readout()
        {
            RegionSupply supply = RegionSupply.Current;
            return supply == null ? "Supply -" : $"Supply {supply.Pool.Amount:0.#} (+{supply.PerMinute:0.#}/min) · troops raised {TroopsRaised} · extra patrols {PatrolsAdded}";
        }
    }
}
