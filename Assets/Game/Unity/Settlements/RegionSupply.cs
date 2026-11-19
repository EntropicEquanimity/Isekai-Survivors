using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// The Region's mortals' Supply: standing farms and villages make it over time, razing them cuts it
    /// (<see cref="SupplyRules"/>), and <see cref="RegionTroops"/> spends it on troops.
    /// </summary>
    [RegionSystem]
    public sealed class RegionSupply : MonoBehaviour
    {
        public static RegionSupply Current { get; private set; }

        public SupplyPool Pool { get; private set; } = new SupplyPool();

        /// <summary>How fast Supply and troop timers run (tests turn it up; 1 in play).</summary>
        public float Speed { get; set; } = 1f;

        /// <summary>Supply the standing farms and villages make per minute right now.</summary>
        public float PerMinute
        {
            get
            {
                RegionSettlements settlements = RegionSettlements.Current;
                if (settlements == null) return 0f;
                float total = 0f;
                foreach (RegionSettlements.Settlement s in settlements.Settlements) total += SupplyRules.PerMinute(s.Kind, s.IsRazed);
                return total;
            }
        }

        private void OnEnable()
        {
            Current = this;
            Pool = new SupplyPool();
        }

        private void OnDisable()
        {
            if (Current == this) Current = null;
        }

        private void Update() => Pool.Produce(PerMinute, Time.deltaTime * Speed);
    }
}
