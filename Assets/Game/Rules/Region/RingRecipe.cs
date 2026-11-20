using System;

namespace Isekai
{
    /// <summary>The four kinds of Region by where they lie on the Planet (see CONTEXT.md: Planet, Capital, Seat).</summary>
    public enum RegionRing { Outer, Middle, Inner, Capital }

    /// <summary>
    /// What a Region's ring changes about it, in one place: the middle and inner rings and the Capital reuse the
    /// outer-ring generator with bigger numbers. Map size, how many settlements, the Seat's size and strength (a small
    /// fort, a fort, a large fort, the palace), the Wave cap, the mortals' troops and Supply, and the ruler's stats.
    /// The outer ring's numbers are what every Region had before rings existed. Balance is tuned in #35.
    /// </summary>
    public sealed class RingRecipe
    {
        private RingRecipe() { }

        public RegionRing Ring { get; private set; }

        /// <summary>The Seat's size in the glossary's words.</summary>
        public string SeatName { get; private set; }

        // ---- the map ----

        /// <summary>The map's size in cells.</summary>
        public int Width { get; private set; }
        public int Height { get; private set; }

        /// <summary>How many villages, farms and camps a full-size map of this ring holds, at least and at most.</summary>
        public int MinSettlements { get; private set; }
        public int MaxSettlements { get; private set; }

        // ---- the Seat ----

        /// <summary>The Seat's courtyard, in cells each way (odd: the Seat's spot is the middle).</summary>
        public int SeatSize { get; private set; }

        /// <summary>Guards in the Seat's courtyard, and its watchtowers.</summary>
        public int SeatGuards { get; private set; }
        public int SeatTowers { get; private set; }

        /// <summary>What the keep's health is multiplied by.</summary>
        public float KeepHealth { get; private set; }

        /// <summary>What the Seat guards' health is multiplied by on top of <see cref="TroopStrength"/> (the palace's elite guard).</summary>
        public float EliteGuardStrength { get; private set; }

        // ---- Vigilance and the mortals' troops ----

        /// <summary>How many Waves may be alive at once.</summary>
        public int WaveCap { get; private set; }

        /// <summary>The stage the Region starts at (the Capital starts Alarmed).</summary>
        public VigilanceStage StartStage { get; private set; }

        /// <summary>What the health of every guard, troop and Wave mortal is multiplied by.</summary>
        public float TroopStrength { get; private set; }

        /// <summary>What the most troops a stronghold raises is multiplied by.</summary>
        public float TroopLimitScale { get; private set; }

        /// <summary>How much faster than the outer ring troops are raised (the raising timers run this many times as fast).</summary>
        public float TroopSpeed { get; private set; }

        /// <summary>Supply the mortals start with, and what their farms' and villages' output is multiplied by.</summary>
        public float StartingSupply { get; private set; }
        public float SupplyRate { get; private set; }

        // ---- the ruler ----

        /// <summary>What the ruler's health and damage are multiplied by on top of a hero's usual strength.</summary>
        public float RulerHealth { get; private set; }
        public float RulerDamage { get; private set; }

        public int MapCells => Width * Height;

        public override string ToString() => $"{Ring} ring ({Width}x{Height}, {SeatName}, Wave cap {WaveCap})";

        private static readonly RingRecipe s_outer = new()
        {
            Ring = RegionRing.Outer, SeatName = "small fort",
            Width = RegionRecipe.DefaultWidth, Height = RegionRecipe.DefaultHeight, MinSettlements = 6, MaxSettlements = SettlementPlan.MaxCount,
            SeatSize = 11, SeatGuards = 6, SeatTowers = 2, KeepHealth = 1f, EliteGuardStrength = 1f,
            WaveCap = VigilanceTuning.OuterRingWaveCap, StartStage = VigilanceStage.Unaware,
            TroopStrength = 1f, TroopLimitScale = 1f, TroopSpeed = 1f, StartingSupply = SupplyTuning.StartingSupply, SupplyRate = 1f,
            RulerHealth = 1f, RulerDamage = 1f,
        };

        private static readonly RingRecipe s_middle = new()
        {
            Ring = RegionRing.Middle, SeatName = "fort",
            Width = 184, Height = 126, MinSettlements = 8, MaxSettlements = 13,
            SeatSize = 13, SeatGuards = 9, SeatTowers = 4, KeepHealth = 1.5f, EliteGuardStrength = 1.15f,
            WaveCap = 3, StartStage = VigilanceStage.Unaware,
            TroopStrength = 1.2f, TroopLimitScale = 1.5f, TroopSpeed = 1.25f, StartingSupply = 15f, SupplyRate = 1.25f,
            RulerHealth = 1.3f, RulerDamage = 1.15f,
        };

        private static readonly RingRecipe s_inner = new()
        {
            Ring = RegionRing.Inner, SeatName = "large fort",
            Width = 208, Height = 142, MinSettlements = 10, MaxSettlements = 16,
            SeatSize = 15, SeatGuards = 12, SeatTowers = 6, KeepHealth = 2f, EliteGuardStrength = 1.3f,
            WaveCap = 5, StartStage = VigilanceStage.Unaware,
            TroopStrength = 1.45f, TroopLimitScale = 2f, TroopSpeed = 1.5f, StartingSupply = 20f, SupplyRate = 1.5f,
            RulerHealth = 1.6f, RulerDamage = 1.3f,
        };

        /// <summary>The Capital's numbers with the fewest Waves: <see cref="For"/> raises the cap to one Wave per Region left.</summary>
        public const int CapitalMinimumWaves = 3;

        private static RingRecipe Capital(int waveCap) => new()
        {
            Ring = RegionRing.Capital, SeatName = "palace",
            Width = 240, Height = 164, MinSettlements = 14, MaxSettlements = 22,
            SeatSize = 19, SeatGuards = 18, SeatTowers = 8, KeepHealth = 2.5f, EliteGuardStrength = 2f,
            WaveCap = Math.Max(CapitalMinimumWaves, waveCap), StartStage = VigilanceStage.Alarmed,
            TroopStrength = 1.8f, TroopLimitScale = 3f, TroopSpeed = 2f, StartingSupply = 30f, SupplyRate = 2f,
            RulerHealth = 2.2f, RulerDamage = 1.5f,
        };

        /// <summary>The recipe of a ring. The Capital's Wave cap is <paramref name="capitalWaves"/> (at least <see cref="CapitalMinimumWaves"/>).</summary>
        public static RingRecipe Of(RegionRing ring, int capitalWaves = CapitalMinimumWaves) => ring switch
        {
            RegionRing.Middle => s_middle,
            RegionRing.Inner => s_inner,
            RegionRing.Capital => Capital(capitalWaves),
            _ => s_outer,
        };

        /// <summary>
        /// Which ring a Region of this <paramref name="depth"/> (0 at the Planet's edge) lies in on a Planet of
        /// <paramref name="rings"/> rings: the edge is outer, the innermost ring inner, and a Planet with more rings
        /// spreads the middle between them (so a small test Planet has all three).
        /// </summary>
        public static RegionRing RingOf(int depth, int rings)
        {
            if (rings <= 1) return RegionRing.Outer;
            int tier = (int)Math.Round(depth * 2.0 / (rings - 1), MidpointRounding.AwayFromZero);
            return (RegionRing)Math.Clamp(tier, (int)RegionRing.Outer, (int)RegionRing.Inner);
        }

        /// <summary>
        /// The recipe for a Region of a Planet, <paramref name="captured"/> Regions of the Run being taken already: the
        /// Capital has one Wave per Region not yet captured (at least three).
        /// </summary>
        public static RingRecipe For(RegionInfo region, Planet planet, int captured)
        {
            if (region == null || planet == null) return s_outer;
            if (region.IsCapital) return Capital(planet.Regions.Count - 1 - captured);
            return Of(RingOf(region.Depth, planet.Rings));
        }
    }
}
