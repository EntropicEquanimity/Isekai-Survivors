namespace Isekai
{
    /// <summary>What each Anchor does differently, and every tunable number of those differences.</summary>
    public static class AnchorTuning
    {
        /// <summary>The War Throne's aura reaches this far (world units, one cell is one unit).</summary>
        public const float AuraRadius = 10f;

        /// <summary>A demon of yours inside the aura deals this much more damage (a Damage Bonus modifier).</summary>
        public const float AuraDamageBonus = 0.3f;

        /// <summary>Seconds between checks of who is inside the aura and whether a Shrine has been seen.</summary>
        public const float CheckSeconds = 0.25f;

        /// <summary>The War Throne spreads corruption further and twice as fast as the Shrine's ordinary Anchor source.</summary>
        public static readonly CorruptionSource WarThroneSpread = new(radius: 14, secondsPerStep: 2f);

        public static string Name(AnchorKind kind) => kind == AnchorKind.WarThrone ? "War Throne" : "Shrine";

        /// <summary>The Shrine stays hidden from the mortals until a patrol sees it; the War Throne is in plain sight.</summary>
        public static bool StartsHidden(AnchorKind kind) => kind == AnchorKind.Shrine;

        /// <summary>The stage Vigilance starts the Region at.</summary>
        public static VigilanceStage StartStage(AnchorKind kind) => kind == AnchorKind.WarThrone ? VigilanceStage.Uneasy : VigilanceStage.Unaware;

        /// <summary>Whether demons in range get the aura.</summary>
        public static bool HasAura(AnchorKind kind) => kind == AnchorKind.WarThrone;

        /// <summary>The corruption source this Anchor is.</summary>
        public static CorruptionSource Source(AnchorKind kind) => kind == AnchorKind.WarThrone ? WarThroneSpread : CorruptionTuning.Anchor;
    }
}
